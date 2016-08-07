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

namespace SambaLinkMaker {
	public enum LinkFormat {
		/// <summary>
		/// Just outputs the local absolute path without checking for samba shares.
		/// Kind of out of scope of the program, but I used it to make a kde service
		/// menu that copies local paths and unc/smb links.
		/// </summary>
		LocalPath,

		/// <summary>
		/// A local file URI.
		/// </summary>
		LocalFile,

		/// <summary>
		/// A file URI with remote host.
		/// </summary>
		File,

		/// <summary>
		/// Windows UNC path without character escaping.
		/// </summary>
		Unc,

		/// <summary>
		/// Windows UNC path with character escaping.
		/// </summary>
		UncEscaped,

		/// <summary>
		/// Samba smb:// url.
		/// </summary>
		Smb
	}
}

