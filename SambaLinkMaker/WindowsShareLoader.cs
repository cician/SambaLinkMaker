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
using System.Collections.Generic;

namespace SambaLinkMaker {
	public class WindowsShareLoader {
		private enum ShareType : uint {
			DiskDrive = 0,
			PrintQueue = 1,
			Device = 2,
			IPC = 3,
			DiskDriveAdmin = 2147483648,
			PrintQueueAdmin = 2147483649,
			DeviceAdmin = 2147483650,
			IPCAdmin = 2147483651
		}

		public static void LoadGlobalShares(SharesList dstList) {
			// There are multiple ways actually:
			// 1. Lounch the "net share" command
			// 2. Read shares from registry
			// 3. Call native methods from mpr.dll
			// 4. System.Management

			// Side note: actually there's something like Win32_ShareToDirectory in Windows,
			// but I already have all the nice abstraction around having a share list...

			using (ManagementClass exportedShares = new ManagementClass("Win32_Share")) {
				ManagementObjectCollection shares = exportedShares.GetInstances();

				List<Share> addWithHigherPriority = new List<Share>(shares.Count);
				List<Share> addWithLowerPriority = new List<Share>(shares.Count);

				foreach (ManagementObject share in shares) {
					// Reference: https://msdn.microsoft.com/en-us/library/aa394435.aspx
					ShareType type = (ShareType)Convert.ToUInt32(share["Type"]);

					string shareName = share["Name"].ToString();
					string localPath = share["Path"].ToString();

					if (type == ShareType.DiskDrive || type == ShareType.Device) {
						addWithHigherPriority.Add(new Share(shareName, new TokenizedLocalPath(localPath, '\\')));
					} else if (type == ShareType.DiskDriveAdmin || type == ShareType.DeviceAdmin) {
						// Take it, but add it with a lower priority.
						// This way the explicit shares take priority, as these are usually
						// the ones the user wants to be used and often have lesser
						// access restrictions.
						addWithLowerPriority.Add(new Share(shareName, new TokenizedLocalPath(localPath, '\\')));
					} else {
						// skip everything else
					}
				}

				// First add the shares that should take lower priority.
				foreach (Share share in addWithLowerPriority)
					dstList.AddOrReplace(share);

				// Then add the ones with higher priority, overwriting eventual shares.
				foreach (Share share in addWithHigherPriority)
					dstList.AddOrReplace(share);
			}
		}
	}
}

