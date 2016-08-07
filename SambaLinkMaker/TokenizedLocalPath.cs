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
using System.Collections;
using System.Collections.Generic;

namespace SambaLinkMaker {
	/// <summary>
	/// A helper class to ease dealing with path names without need for reparsing
	/// and reformatting when applying transforms.
	/// Could probably manage with System.IO.Path without duplicated effort, but
	/// I wanted full control of what happens on different platforms.
	/// </summary>
	public class TokenizedLocalPath : IEnumerable<string> {
		private static readonly char[] separators = { Path.DirectorySeparatorChar };

		private readonly string[] pathElements;

		public TokenizedLocalPath(params string[] pathElements) {
			this.pathElements = pathElements;
		}

		public TokenizedLocalPath(string path) {
			// Here a natural thing to do would be to pass StringSplitOptions.RemoveEmptyEntries.
			// Instead I decided to preserve the empty values and to implicitly make an element
			// for unix root paths with empty string. The rest of the code kinda supposes this
			// to happen. On windows the root will be the drive letter.

			Uri uri = null;
			if (!Uri.TryCreate(path, UriKind.Absolute, out uri)) {
				Uri currentDir = new Uri(System.Environment.CurrentDirectory);
				if (!Uri.TryCreate(currentDir, path, out uri))
					throw new ArgumentException(string.Format("Could not resolve path {0} to URI", path));
			}

			if (!uri.IsFile)
				throw new ArgumentException(string.Format("Could not resolve path {0} to URI. Only file/directory paths are supported.", path));

			// Specific case for URIs like file://host/..., but I haven't seen them in the wild.
			if (uri.Host != null && uri.Host != "")
				throw new ArgumentException(string.Format("Could not resolve path {0} to URI. Only file/directory paths are supported.", path));

			path = uri.LocalPath;

			// But we still want to remove trailing slashes.
			path = path.TrimEnd(separators);

			this.pathElements = path.Split(separators, StringSplitOptions.None);
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator() {
			return ((IEnumerable<string>)pathElements).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return pathElements.GetEnumerator();
		}

		public void Format(StringBuilder sb, bool escape, char pathSeparator) {
			bool first = true;
			foreach (string pathElem in pathElements) {
				if (first) {
					first = false;
				} else {
					sb.Append(pathSeparator);
				}
				if (escape) {
					sb.Append(Uri.EscapeUriString(pathElem));
				} else {
					sb.Append(pathElem);
				}
			}
		}

		public string Format(bool escape, char pathSeparator) {
			StringBuilder sb = new StringBuilder();
			Format(sb, escape, pathSeparator);
			return sb.ToString();
		}

		public bool IsEmpty() {
			return pathElements.Length == 0;
		}

		public override bool Equals(Object obj) {
			if (obj == null || GetType() != obj.GetType()) 
				return false;
			
			TokenizedLocalPath other = (TokenizedLocalPath)obj;
			return Array.Equals(this.pathElements, other.pathElements);
		}

		public override int GetHashCode() {
			return pathElements.GetHashCode();
		}

		public static TokenizedLocalPath Combine(TokenizedLocalPath parentPath, TokenizedLocalPath relativePath) {
			if (relativePath == null || relativePath.IsEmpty())
				return parentPath;

			string[] parentElems = parentPath.pathElements;
			string[] relativeElems = relativePath.pathElements;
			string[] newElems = new string[parentElems.Length + relativeElems.Length];
			Array.Copy(parentElems, newElems, parentElems.Length);
			Array.Copy(relativeElems, 0, newElems, parentElems.Length, relativeElems.Length);
			return new TokenizedLocalPath(newElems);
		}

		public static TokenizedLocalPath MakeRelative(TokenizedLocalPath parentPath, TokenizedLocalPath childPath) {
			string[] parentElems = parentPath.pathElements;
			string[] childElems = childPath.pathElements;

			// Check if the child path is really relative to parent.
			for (int i = 0; i < parentElems.Length; i++) {
				if (parentElems[i] != childElems[i])
					throw new ArgumentException(string.Format("child path {0} is not relative to parent {1}", childPath.ToString(), parentPath.ToString()));
			}

			string[] relativeElems = new string[childElems.Length - parentElems.Length];
			Array.Copy(childElems, parentElems.Length, relativeElems, 0, relativeElems.Length);
			return new TokenizedLocalPath(relativeElems);
		}

		public override string ToString() {
			return Format(false, '/');
		}
	}
}
