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
using System.Management;

namespace SambaLinkMaker {
	public class WindowsShareLoader {
		public static void LoadGlobalShares(SharesList dstList) {
			// There are multiple ways actually:
			// 1. Lounch the "net share" command
			// 2. Read shares from registry
			// 3. Call native methods from mpr.dll
			// 4. System.Management

			using (ManagementClass exportedShares = new ManagementClass("Win32_Share")) {
				ManagementObjectCollection shares = exportedShares.GetInstances();
				foreach (ManagementObject share in shares) {
					string shareName = share["Name"].ToString();
					string localPath = share["Path"].ToString();

					dstList.AddOrReplace(shareName, localPath);
				}
			}          
			Console.ReadKey();
		}
	}
}

