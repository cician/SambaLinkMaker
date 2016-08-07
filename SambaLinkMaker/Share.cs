﻿// Copyright (c) 2016 Maciej Kacper Jagiełło <maciej@jagiello.it>
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
	/// <summary>
	/// A LAN share description.
	/// </summary>
	public class Share {
		private string name;
		private TokenizedLocalPath localPath;

		private string nameEscaped;

		// For completeness could add other attributes here like comments and
		// permissions, but it's out of scope for SambaLinkMaker.

		public string Name { get { return name; } }
		public TokenizedLocalPath LocalPath { get { return localPath; } }

		public string NameEscaped { get { return nameEscaped; } }

		public Share(string name, string localPath) : this(name, new TokenizedLocalPath(localPath)) {
		}

		public Share(string name, TokenizedLocalPath localPath) {
			this.name = name;
			this.nameEscaped = Uri.EscapeUriString(name);
			this.localPath = localPath;
		}
	}
}

