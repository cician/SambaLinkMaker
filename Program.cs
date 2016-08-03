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
using IniParser.Parser;
using IniParser.Model;
using Mono.Options;

namespace SambaLinkMaker {
	class MainClass {
		/// <summary>
		/// Exception caused by user input in expected ways.
		/// </summary>
		class InputError : Exception {
			public InputError(string msg) : base(msg) {

			}
		}

		enum HostType {
			Ip,
			Ip6,
			Hostname,
			Netbios
		}

		enum LinkFormat {
			Unc,
			Smb
		}

		static SortedDictionary<string,string> LoadSambaShares() {
			var result = new SortedDictionary<string, string>();

			// Use "net usershare info" command for easy access to samba shares without root.
			// I don't know if it's generally avaiable and a good idea, but works well for me.
			// Alternatives:
			// - Parse smb.conf
			// - Use some native samba library, but may as well rewrite in C
			// - Let the user provide a list of shares
			// 
			// Actually this part can be abstracted and made cross platform. I even have some
			// old windows code laying around for this purpose, but for now the goal is to run
			// on Linux. Contributions welcome ;)
			string output;
			using (System.Diagnostics.Process p = new System.Diagnostics.Process()) {
				p.StartInfo.FileName = "net";
				p.StartInfo.Arguments = "usershare info";
				p.StartInfo.RedirectStandardError = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.UseShellExecute = false;
				if (!p.Start())
					throw new Exception("could not start command \"net usershare info\" to get list of shares");

				output = p.StandardOutput.ReadToEnd();

				p.WaitForExit();

				if (p.ExitCode != 0)
					throw new Exception(string.Format("command \"net usershare info\" returned error code {0}", p.ExitCode));
			}

			var parser = new IniDataParser();
			var iniData = parser.Parse(output);

			foreach (var shareIniSection in iniData.Sections) {
				string shareLocalPath = shareIniSection.Keys["path"];

				if (!shareLocalPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
					shareLocalPath += '/';

				string shareName = shareIniSection.SectionName;
				if (!result.ContainsKey(shareLocalPath))
					result.Add(shareLocalPath, shareName);
			}

			return result;
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

		public static void Main(string[] args) {
			// Failed unless otherwise reached the right point.
			System.Environment.ExitCode = 1;

			string host = null;
			bool showHelp = false;
			LinkFormat linkFormat = LinkFormat.Unc;
			HostType hostType = HostType.Netbios;

			// Not implemented for now to keep things simple. The problem with the clipboard is that it's normally tied to a widget toolkit.
			// Other than that, easy peasy.
			bool copyToClipboard = false;

			// I don't really like changing local variables from lambdas, but it makes the code short.
			// If I get to know other command line parsing libs I may replace Mono.Options in the future.. maybe.
			var options = new OptionSet () {
				{ "host=", "Custom hostname.",
					(string v) => host = v },
				{ "format=", "Link format. Either smb or unc. Default is " + linkFormat + ".",
					(LinkFormat v) => linkFormat = v },
				{ "hosttype=", "One of ip, ip6, netbios or hostname. Default is " + hostType + ". Ignored if custom host is specified.",
					(HostType v) => hostType = v },
				{ "help",  "Show this message and exit.", 
					v => showHelp = v != null },
				//{ "copy",  "Copy the result to clipboard instead of standard output.", 
				//	v => copyToClipboard = v != null },
			};

			try {
				List<string> localPaths = options.Parse(args);
				if (localPaths.Count < 1)
					throw new InputError("local path not specified");

				SortedDictionary<string,string> shares = LoadSambaShares();

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
					string absolutePath = localPath;
					if (absolutePath.StartsWith("file://"))
						absolutePath = absolutePath.Substring("file://".Length);
					absolutePath = Path.GetFullPath(absolutePath);
					string localPathWithTrailingDelim = absolutePath + Path.DirectorySeparatorChar;

					string link = null;

					foreach (var e in shares) {
						string shareLocalPath = e.Key;
						if (localPathWithTrailingDelim.StartsWith(shareLocalPath)) {
							string shareName = e.Value;

							string relativePath;
							if (shareLocalPath.Length == localPathWithTrailingDelim.Length) {
								// special case: this is a link to the share itself, instead of a contained file or directory
								relativePath = "";
							} else {
								relativePath = absolutePath.Substring(shareLocalPath.Length);
							}

							StringBuilder sb = new StringBuilder();
							if (linkFormat == LinkFormat.Smb) {
								// actually not needed as the program expects to run on linux
								if (Path.DirectorySeparatorChar != '/')
									relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

								sb.Append("smb://");
								sb.Append(host);
								sb.Append("/");
								sb.Append(shareName);
								sb.Append("/");
								sb.Append(relativePath);
							} else if (linkFormat == LinkFormat.Unc) {
								if (Path.DirectorySeparatorChar != '\\')
									relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '\\');

								sb.Append("\\\\");
								sb.Append(host);
								sb.Append("\\");
								sb.Append(shareName);
								sb.Append("\\");
								sb.Append(relativePath);
							} else {
								throw new Exception("unexpected link format: " + linkFormat);
							}

							link = sb.ToString();
							break;
						}
					}

					if (first) {
						first = false;
					} else {
						output.WriteLine();
					}

					if (link != null) {
						output.Write(link);
					} else {
						Console.Error.WriteLine(string.Format("No share found containing local path \"{0}\"", absolutePath));
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
					Environment.ExitCode = 0;
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
		}
	}
}
