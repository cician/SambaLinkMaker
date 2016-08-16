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
using System.IO;
using System.Collections.Generic;
using IniParser.Parser;
using IniParser.Model;

namespace SambaLinkMaker {
	public class SambaShareLoader {
		public static void ParseNetUserShareList(SharesList dstList, string shareListIniContent) {
			var parser = new IniDataParser();
			var iniData = parser.Parse(shareListIniContent);

			foreach (var shareIniSection in iniData.Sections) {
				string shareName = shareIniSection.SectionName;
				if (!shareIniSection.Keys.ContainsKey("path"))
					throw new Exception(String.Format("share {0} doesn't have local path specified", shareName));

				string shareLocalPath = shareIniSection.Keys["path"];
				dstList.AddOrReplace(new Share(shareName, new TokenizedLocalPath(shareLocalPath, '/')));
			}
		}

		/// <summary>
		/// Loads the usershare shares.
		/// </summary>
		public static void LoadUserShares(SharesList dstList) {
			// Use "net usershare info" command for easy access to samba shares without root.
			// These are limited to user shares, so we still need to parse smb.conf for
			// system-wide shares.
			// Not that I could read them directly from /var/lib/samba/usershares, but the hope
			// is that the net command filters some permissions, so I don't have to. Actually
			// I haven't checked if it does, so it may make sense in order to remove one
			// dipendency.

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

			ParseNetUserShareList(dstList, output);
		}

		public static void ParseSmbConfShareList(SharesList dstList, string smbConfContent) {
			// note: some smb.conffeatures are not handled. Like special variables and includes.
			// TODO special case for [homes] share

			var parser = new IniDataParser();
			parser.Configuration.CommentRegex = new System.Text.RegularExpressions.Regex(@"^[#;](.*)");
			var iniData = parser.Parse(smbConfContent);

			foreach (var shareIniSection in iniData.Sections) {
				string shareName = shareIniSection.SectionName;
				if (shareName == "global")
					continue;

				if (!shareIniSection.Keys.ContainsKey("path"))
					throw new Exception(String.Format("share {0} doesn't have local path specified", shareName));

				string shareLocalPath = shareIniSection.Keys["path"];

				dstList.AddOrReplace(new Share(shareName, new TokenizedLocalPath(shareLocalPath, '/')));
			}
		}

		/// <summary>
		/// Loads the global samba shares from smb.conf.
		/// </summary>
		public static void LoadGlobalSambaShares(SharesList dstList) {
			string smbConfPath = "/etc/samba/smb.conf";
			if (!File.Exists(smbConfPath))
				throw new FileNotFoundException(smbConfPath);

			string content = File.ReadAllText(smbConfPath);

			ParseSmbConfShareList(dstList, content);
		}
	}
}

