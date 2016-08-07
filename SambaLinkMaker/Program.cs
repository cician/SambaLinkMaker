// Copyright (c) 2016 Maciej Kacper Jagiełło <maciej@jagiello.it>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Mono.Options;

namespace SambaLinkMaker {
	public class MainClass {
		/// <summary>
		/// Exception caused by user input in expected ways.
		/// </summary>
		class InputError : Exception {
			public InputError(string msg) : base(msg) {

			}
		}

		static string FindIPAddress(bool ipv6) {
			string result = null;
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

			IPAddress loopback = null;

			foreach(NetworkInterface adapter in nics) {
				foreach(var uaddr in adapter.GetIPProperties().UnicastAddresses) {
					IPAddress addr = uaddr.Address;

					if ((addr.AddressFamily == AddressFamily.InterNetwork && !ipv6)
						|| (addr.AddressFamily == AddressFamily.InterNetworkV6 && ipv6)) {

						// don't consider loopback address for now, but save it for later if nothing interesting is found...
						if (IPAddress.IsLoopback(addr)) {
							loopback = addr;
							continue;
						}

						result = addr.ToString();

						// A little hack. In presence of multiple addresses prefer the ones with physical
						// mac address. This is just because usually you want to advertise a real IP
						// and skip virtual networks on hosts with VMs.
						bool hasMacAddr = Array.TrueForAll(adapter.GetPhysicalAddress().GetAddressBytes(), (v) => v != 0);
						if (hasMacAddr)
							return result;
					}
				}
			}

			if (result == null) {
				if (loopback != null) {
					result = loopback.ToString();
				} else {
					// this shouldn't happen, but... whatever
					result = ipv6 ? "::1" : "127.0.0.1";
				}
			}

			return result;
		}

		public static int Main(string[] args) {
			// Failed unless otherwise reached the right point.
			int exitCode = 1;

			string host = null;
			string sharesfile = null;
			bool showHelp = false;
			LinkFormat linkFormat = LinkFormat.Unc;
			HostType hostType = HostType.Netbios;

			// Not implemented for now to keep things simple. The problem with the clipboard is that it's normally tied to a widget toolkit.
			// Other than that, easy peasy.
			bool copyToClipboard = false;

			bool readStdIn = false;
			bool noStdErr = false;

			// I don't really like changing local variables from lambdas, but it makes the code short.
			// If I get to know other command line parsing libs I may replace Mono.Options in the future.. maybe.
			var options = new OptionSet () {
				{ "host=", "Custom hostname.",
					(string v) => host = v },
				{ "sharesfile=", "Provide a custom .ini file with share list. The format should correspond to outout of \"net usershare info\" command, which also matches smb.conf share list.",
					(string v) => sharesfile = v },
				{ "format=", "Link format. Either smb or unc. Default is " + linkFormat + ".",
					(LinkFormat v) => linkFormat = v },
				{ "hosttype=", "One of ip, ip6, netbios or hostname. Default is " + hostType + ". Ignored if custom host is specified.",
					(HostType v) => hostType = v },
				{ "help",  "Show this message and exit.", 
					v => showHelp = v != null },
				{ "stdin", "Read path list from strandard input.",
					v => readStdIn = v != null },
				{ "nostderr", "Write errors into stdout instead of stderr.",
					v => noStdErr = v != null },
				//{ "copy",  "Copy the result to clipboard instead of standard output.", 
				//	v => copyToClipboard = v != null },
			};

			try {
				List<string> localPaths = options.Parse(args);

				if (noStdErr)
					Console.SetError(Console.Out);

				if (readStdIn) {
					string s;
					while ((s = Console.In.ReadLine()) != null) {
						localPaths.Add(s);
					}
				}

				if (localPaths.Count < 1)
					throw new InputError("local path not specified");

				SharesList shares = new SharesList();
				if (sharesfile == null) {
					// TODO windows?
					switch (Environment.OSVersion.Platform) {
						case PlatformID.Unix:
							// Load the samba shares.
							SambaShareLoader.LoadUserShares(shares);
							SambaShareLoader.LoadGlobalSambaShares(shares);
							break;
//						case PlatformID.MacOSX:
//						case PlatformID.Win32NT:
//						case PlatformID.Win32Windows:
						default:
							throw new Exception(string.Format("Unknown platform {0}. Could not get LAN shares from the system.", Environment.OSVersion.Platform));
					}
				} else {
					// For the custom share list we use the same format as the "net usershare info" command.
					string shareListIniContent = File.ReadAllText(sharesfile, Encoding.UTF8);
					SambaShareLoader.ParseNetUserShareList(shares, shareListIniContent);
				}

				if (host == null) {
					if (hostType == HostType.Ip || hostType == HostType.Ip6) {
						host = FindIPAddress(hostType == HostType.Ip6);
					} else if (hostType == HostType.Netbios) {
						host = System.Environment.MachineName;
					} else if (hostType == HostType.Hostname) {
						host = Dns.GetHostName();
					} else {
						throw new InputError(String.Format("unexpected host-type option: {0}", hostType));
					}
				}

				TextWriter output;
				StringWriter outputSW;
				if (copyToClipboard) {
					outputSW = new StringWriter();
					output = outputSW;
				} else {
					outputSW = null;
					output = Console.Out;
				}

				/*
				 * Small note on complexity. For now the complexity is exponential.
				 * As long as there aren't many shares AND many paths to convert it
				 * shouldn't matter much. Otherwise a better solution is needed with
				 * some data structure. For example searching in a sorted list of
				 * shares using binary search.
				 * 
				 * Could implement a binary search on SortedList<string,string> or
				 * use a ready made class prefixdictionary/trie.
				 */

				bool first = true;
				bool foundAll = true;
				foreach (string localPath in localPaths) {
					if (first) {
						first = false;
					} else {
						output.WriteLine();
					}

					string link = null;

					TokenizedLocalPath tokenizedLocalPath = null;
					try {
						tokenizedLocalPath = new TokenizedLocalPath(localPath);
					} catch (Exception ex) {
						Console.Error.WriteLine(ex.ToString());
					}

					if (tokenizedLocalPath != null) {
						link = LinkMaker.MakeLink(linkFormat, host, shares, tokenizedLocalPath);	
					} else {
						link = localPath;
					}

					if (link != null) {
						output.Write(link);
					} else {
						Console.Error.WriteLine(string.Format("No share found containing local path \"{0}\"", localPath));
						output.Write(localPath);
						foundAll = false;
					}
				}

				/*
				if (copyToClipboard) {
					//Xwt.Application.Initialize ("Xwt.GtkBackend.GtkEngine, Xwt.Gtk, Version=1.0.0.0");
					//Xwt.Clipboard.SetText(outputSW.ToString());
					//Xwt.Application.Dispose();
					System.Windows.Forms.Clipboard.SetText(outputSW.ToString());
				}
				*/

				if (foundAll) {
					// OK. If we reached this point then everything went O~K.
					exitCode = 0;
				}
			} catch (InputError e) {
				Console.Error.WriteLine(e.Message);
				showHelp = true;
			} catch (OptionException e) {
				Console.Error.WriteLine(e.ToString());
				showHelp = true;
			} catch (Exception ex) {
				Console.Error.WriteLine("Unexpected error: " + ex.ToString());
			}

			if (showHelp) {
				Console.WriteLine ("Usage:");
				Console.WriteLine ("\t" + Path.GetFileNameWithoutExtension(System.Environment.CommandLine) + " <local-path> [<local-path2>, ...]");
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				options.WriteOptionDescriptions (Console.Out);
			}

			return exitCode;
		}
	}
}
