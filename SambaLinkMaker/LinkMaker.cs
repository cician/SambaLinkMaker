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
using System.Text;

namespace SambaLinkMaker {
	public class LinkMaker {
		public static string ComposeLink(LinkFormat linkFormat, string host, Share share, TokenizedLocalPath shareRelativePath) {
			StringBuilder sb = new StringBuilder();
			if (linkFormat == LinkFormat.Smb) {
				sb.Append("smb://");
				sb.Append(Uri.EscapeUriString(host));
				sb.Append('/');
				sb.Append(share.NameEscaped);
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append('/');
					shareRelativePath.Format(sb, true, '/');
				}
			} else if (linkFormat == LinkFormat.Unc) {
				sb.Append("\\\\");
				sb.Append(host);
				sb.Append('\\');
				sb.Append(share.Name);
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append('\\');
					shareRelativePath.Format(sb, false, '\\');
				}
			} else if (linkFormat == LinkFormat.UncEscaped) {
				sb.Append("\\\\");
				sb.Append(Uri.EscapeUriString(host));
				sb.Append('\\');
				sb.Append(share.NameEscaped);
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append('\\');
					shareRelativePath.Format(sb, true, '\\');
				}
			} else if (linkFormat == LinkFormat.File) {
				sb.Append("file://");
				sb.Append(Uri.EscapeUriString(host));
				sb.Append('/');
				sb.Append(share.NameEscaped);
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append('/');
					shareRelativePath.Format(sb, true, '/');
				}
			} else if (linkFormat == LinkFormat.LocalFile) {
				sb.Append("file://");
				share.LocalPath.Format(sb, true, '/');
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append('/');
					shareRelativePath.Format(sb, true, '/');
				}
			} else if (linkFormat == LinkFormat.LocalPath) {
				share.LocalPath.Format(sb, false, Path.DirectorySeparatorChar);
				if (shareRelativePath != null && !shareRelativePath.IsEmpty()) {
					sb.Append(Path.DirectorySeparatorChar);
					shareRelativePath.Format(sb, false, Path.DirectorySeparatorChar);
				}
			} else {
				throw new Exception("unexpected link format: " + linkFormat);
			}

			string link = sb.ToString();
			return link;
		}

		public static string MakeLink(LinkFormat linkFormat, string host, SharesList shares, TokenizedLocalPath localPath) {
			Share share = shares.FindParentShare(localPath);
			if (share != null) {
				TokenizedLocalPath relPath = TokenizedLocalPath.MakeRelative(share.LocalPath, localPath);
				return ComposeLink(linkFormat, host, share, relPath);
			} else {
				return null;
			}
		}
	}
}

