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

namespace SambaLinkMaker {
	/// <summary>
	/// Container of LAN shares that abstracts details when searching for
	/// shares corresponding to local paths.
	/// </summary>
	public class SharesList {
		/// <summary>
		/// All shares contained in this list, indexed by name.
		/// </summary>
		private SortedDictionary<string,Share> sharesByName;

		/// <summary>
		/// A tree structure of paths and corresponding shares.
		/// Allows to store shares at different levels of the tree, but
		/// only one share at a specific path.
		/// </summary>
		private ShareTreeEntry treeRoot = new ShareTreeEntry("");

		//private ShareTreeEntry treeRootCaseInsensirive = new ShareTreeEntry("");

		/// <summary>
		/// The support structure for the share tree.
		/// </summary>
		/// <see cref="treeRoot"/>
		private class ShareTreeEntry {
			public string folderName;
			public Share share = null;
			public Dictionary<string,ShareTreeEntry> children;

			public ShareTreeEntry(string folderName) {
				this.folderName = folderName;
				this.children = new Dictionary<string, ShareTreeEntry>();
				this.share = null;
			}
		}

		// TODO decide how to handle case sensitivity, since in theory one can set
		// it per share... Probably should maintain two trees. For now only handling
		// case sensitive searches, since it's default for samba. If we want to support
		// windows then it becomes much more important.
//		/// <summary>
//		/// Should the searches be case-sensitive?. Set to false on Windows.
//		/// </summary>
//		private bool caseSensitive;

		/// <summary>
		/// Initializes a new empty SharesList.
		/// </summary>
		/// <param name="caseSensitive">If set to <c>true</c> case sensitive.</param>
		public SharesList(/*bool caseSensitive = true*/) {
			this.sharesByName = new SortedDictionary<string, Share>();
//			this.caseSensitive = caseSensitive;
		}

		/// <summary>
		/// All shares contained in this list.
		/// </summary>
		public IEnumerable<Share> Shares { get { return sharesByName.Values; } }

		/// <summary>
		/// All shares contained in this list, indexed by name.
		/// </summary>
		public IDictionary<string, Share> SharesByName { get { return sharesByName; } }

		/// <summary>
		/// Gets a share by name.
		/// </summary>
		/// <returns>
		/// The share or null, if it doesn't exist.
		/// </returns>
		/// <param name="shareName">The share name.</param>
		public Share GetShareByName(string shareName) {
			Share share = null;
			sharesByName.TryGetValue(shareName, out share);
			return share;
		}

		/// <summary>
		/// Add the share to this list. If for any reason there's already another
		/// share for this local path, the new share replaces it.
		/// </summary>
		/// <param name="share">The share to add.</param>
		public void AddOrReplace(Share share) {
			if (!sharesByName.ContainsKey(share.Name))
				sharesByName.Remove(share.Name);
			sharesByName.Add(share.Name, share);

			TokenizedLocalPath path = share.LocalPath;
			ShareTreeEntry treeElem = treeRoot; 
			foreach (string elem in share.LocalPath) {
				ShareTreeEntry childElem = null;
				treeElem.children.TryGetValue(elem, out childElem);

				if (childElem == null) {
					childElem = new ShareTreeEntry(elem);
					treeElem.children.Add(elem, childElem);
				}

				treeElem = childElem;
			}
			treeElem.share = share;
		}

		/// <summary>
		/// Find a share that allows access to this local path.
		/// Note that it's possible for more than one share to match a specific path.
		/// In such case the most specific share will be chosen.
		/// For example if share A has local path /home/user and share B has local
		/// path /home/user/downloads, for localPath='/home/user/downloads/file.mp4'
		/// the method returns B.
		/// </summary>
		/// <returns>The parent share or null.</returns>
		/// <param name="localPath">Local absolute path of a file or directory.
		/// It doesn't necessarily need to exist, but needs to be a valid path.
		/// It should be a plain path, not an URI or UNC path and shouldn't be
		/// escaped in any way.
		/// </param>
		public Share FindParentShare(TokenizedLocalPath localPath) {
			Share result = null;

			ShareTreeEntry treeElem = treeRoot;
			foreach (string elem in localPath) {
				ShareTreeEntry childElem = null;
				treeElem.children.TryGetValue(elem, out childElem);

				if (childElem != null) {
					if (childElem.share != null) {
						// Found a matching share! But continue searching in case
						// there's another, more specific share.
						result = childElem.share;
					}

					// Continue diving into the tree.
					treeElem = childElem;
				} else {
					// No more children.
					break;
				}
			}

			return result;
		}
	}
}

