using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SambaLinkMaker.Tests {
	[TestFixture()]
	public class Test {
		[Test()]
		public void ParseSimpleSharesList() {
			var list = SharesList.ParseShareList(@"[Share1]
				path=/home/user/FA
				comment=
				usershare_acl=Everyone:F,
				guest_ok=y

				[share 2]
				path=/mnt/movies and music/
				comment=
				usershare_acl=Everyone:R,
				guest_ok=n"
			);

			Assert.AreEqual(new TokenizedLocalPath("/home/user/FA"), list.GetShareByName("Share1").LocalPath);
			Assert.AreEqual(new TokenizedLocalPath("/mnt/movies and music/"), list.GetShareByName("share 2").LocalPath);
		}

		[Test()]
		public void FindShareShareSimple() {
			var list = new SharesList();
			list.AddOrReplace("testshare", "/home/user/downloads");
			Assert.IsNotNull(list.FindParentShare("/home/user/downloads/test.txt"));
		}

		[Test()]
		public void FindShareShareSpecifityRule() {
			var list = new SharesList();
			list.AddOrReplace("A", "/home/user");
			list.AddOrReplace("B", "/home/user/downloads");
			Assert.AreEqual("B", list.FindParentShare("/home/user/downloads/test.txt")?.Name);
		}

		[Test()]
		public void ComposeLinkSimple() {
			var share = new Share("share", "/home/user/share");
			var relPath = new TokenizedLocalPath("a", "b.txt");
			Assert.AreEqual("/home/user/share/a/b.txt", LinkMaker.ComposeLink(LinkFormat.LocalPath, "host", share, relPath));
			Assert.AreEqual("file:///home/user/share/a/b.txt", LinkMaker.ComposeLink(LinkFormat.LocalFile, "host", share, relPath));
			Assert.AreEqual("file://host/share/a/b.txt", LinkMaker.ComposeLink(LinkFormat.File, "host", share, relPath));
			Assert.AreEqual(@"\\host\share\a\b.txt", LinkMaker.ComposeLink(LinkFormat.Unc, "host", share, relPath));
			Assert.AreEqual(@"\\host\share\a\b.txt", LinkMaker.ComposeLink(LinkFormat.UncEscaped, "host", share, relPath));
			Assert.AreEqual("smb://host/share/a/b.txt", LinkMaker.ComposeLink(LinkFormat.Smb, "host", share, relPath));
		}

		[Test()]
		public void ComposeLinkSpaces() {
			var share = new Share("my share", "/home/user/my share");
			var relPath = new TokenizedLocalPath("a", "b c.txt");
			Assert.AreEqual("/home/user/my share/a/b c.txt", LinkMaker.ComposeLink(LinkFormat.LocalPath, "host", share, relPath));
			Assert.AreEqual("file:///home/user/my%20share/a/b%20c.txt", LinkMaker.ComposeLink(LinkFormat.LocalFile, "host", share, relPath));
			Assert.AreEqual("file://host/my%20share/a/b%20c.txt", LinkMaker.ComposeLink(LinkFormat.File, "host", share, relPath));
			Assert.AreEqual(@"\\host\my share\a\b c.txt", LinkMaker.ComposeLink(LinkFormat.Unc, "host", share, relPath));
			Assert.AreEqual(@"\\host\my%20share\a\b%20c.txt", LinkMaker.ComposeLink(LinkFormat.UncEscaped, "host", share, relPath));
			Assert.AreEqual("smb://host/my%20share/a/b%20c.txt", LinkMaker.ComposeLink(LinkFormat.Smb, "host", share, relPath));
		}

		[Test()]
		public void ComposeLinkShareItSelf() {
			var share = new Share("share", "/home/user/share");
			var relPath = new TokenizedLocalPath();
			Assert.AreEqual("/home/user/share", LinkMaker.ComposeLink(LinkFormat.LocalPath, "host", share, relPath));
			Assert.AreEqual("file:///home/user/share", LinkMaker.ComposeLink(LinkFormat.LocalFile, "host", share, relPath));
			Assert.AreEqual("file://host/share", LinkMaker.ComposeLink(LinkFormat.File, "host", share, relPath));
			Assert.AreEqual(@"\\host\share", LinkMaker.ComposeLink(LinkFormat.Unc, "host", share, relPath));
			Assert.AreEqual(@"\\host\share", LinkMaker.ComposeLink(LinkFormat.UncEscaped, "host", share, relPath));
			Assert.AreEqual("smb://host/share", LinkMaker.ComposeLink(LinkFormat.Smb, "host", share, relPath));
		}

		[Test()]
		public void MakeLinkSimple() {
			var list = new SharesList();
			list.AddOrReplace("share", "/home/user/share");
			var localPath = new TokenizedLocalPath("/home/user/share/test.txt");
			Assert.AreEqual("/home/user/share/test.txt", LinkMaker.MakeLink(LinkFormat.LocalPath, "host", list, localPath));
			Assert.AreEqual("file:///home/user/share/test.txt", LinkMaker.MakeLink(LinkFormat.LocalFile, "host", list, localPath));
			Assert.AreEqual("file://host/share/test.txt", LinkMaker.MakeLink(LinkFormat.File, "host", list, localPath));
			Assert.AreEqual(@"\\host\share\test.txt", LinkMaker.MakeLink(LinkFormat.Unc, "host", list, localPath));
			Assert.AreEqual(@"\\host\share\test.txt", LinkMaker.MakeLink(LinkFormat.UncEscaped, "host", list, localPath));
			Assert.AreEqual("smb://host/share/test.txt", LinkMaker.MakeLink(LinkFormat.Smb, "host", list, localPath));
		}

		[Test()]
		public void MakeLinkMultiple() {
			var list = new SharesList();
			list.AddOrReplace("share1", "/home/user/share1");
			list.AddOrReplace("share2", "/home/user/share2");
			{
				var localPath = new TokenizedLocalPath("/home/user/share1/test.txt");
				Assert.AreEqual("/home/user/share1/test.txt", LinkMaker.MakeLink(LinkFormat.LocalPath, "host", list, localPath));
				Assert.AreEqual("file:///home/user/share1/test.txt", LinkMaker.MakeLink(LinkFormat.LocalFile, "host", list, localPath));
				Assert.AreEqual("file://host/share1/test.txt", LinkMaker.MakeLink(LinkFormat.File, "host", list, localPath));
				Assert.AreEqual(@"\\host\share1\test.txt", LinkMaker.MakeLink(LinkFormat.Unc, "host", list, localPath));
				Assert.AreEqual(@"\\host\share1\test.txt", LinkMaker.MakeLink(LinkFormat.UncEscaped, "host", list, localPath));
				Assert.AreEqual("smb://host/share1/test.txt", LinkMaker.MakeLink(LinkFormat.Smb, "host", list, localPath));
			}

			{
				var localPath = new TokenizedLocalPath("/home/user/share2/test.txt");
				Assert.AreEqual("/home/user/share2/test.txt", LinkMaker.MakeLink(LinkFormat.LocalPath, "host", list, localPath));
				Assert.AreEqual("file:///home/user/share2/test.txt", LinkMaker.MakeLink(LinkFormat.LocalFile, "host", list, localPath));
				Assert.AreEqual("file://host/share2/test.txt", LinkMaker.MakeLink(LinkFormat.File, "host", list, localPath));
				Assert.AreEqual(@"\\host\share2\test.txt", LinkMaker.MakeLink(LinkFormat.Unc, "host", list, localPath));
				Assert.AreEqual(@"\\host\share2\test.txt", LinkMaker.MakeLink(LinkFormat.UncEscaped, "host", list, localPath));
				Assert.AreEqual("smb://host/share2/test.txt", LinkMaker.MakeLink(LinkFormat.Smb, "host", list, localPath));
			}
		}
	}
}

