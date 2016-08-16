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
	public class ClipboardHelper {
		public static void CopyText(string text) {
			// Since clipboard is dependent on platform's UI it can get tricky.
			// The simplest thing to di in .net is to call into Windows.Forms,
			// so let's do that... Except mono's implementation fails to set
			// the clipboard before process ends and there's no way to know
			// when it finishes the copying.
			// Also Windows.Forms is can be an unwanted dependency in headless
			// mode and needs a special attribute on the program's main method.
			//
			// For now I'm just waiting a bit for it to finish, but it may still
			// fail, and the interval can be a bit annoying.
			// Alternatives:
			// 1. Launch command line programs. For example "xsel -i -b" on linux,
			// clip on windows and pbcopy on mac.
			// 2. Implement a clipboard abstraction that hooks into native
			// libraries.

			System.Windows.Forms.Clipboard.SetText(text);

			// A workaround for mono where clipboard doesn't save before process closes.
			// I tried some tricks to force message processing etc, but it didn't work.
			bool isOnMono = Type.GetType("Mono.Runtime") != null;
			if (isOnMono) {
				var t = new System.Threading.Thread(() => {
					// 100ms seems to be enough on my PC. Even with a very long text, but
					// just in case.
					if (text.Length > 10000) {
						System.Threading.Thread.Sleep(1000);
					} else {
						System.Threading.Thread.Sleep(100);
					}
					System.Windows.Forms.Application.Exit();
				});
				t.IsBackground = false;
				t.Start();

				System.Windows.Forms.Application.Run();
			}
		}
	}
}

