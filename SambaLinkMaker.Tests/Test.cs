using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SambaLinkMaker.Tests {
	[TestFixture()]
	public class Test {
		[Test()]
		public void ParseUserSharesList() {
			SharesList list = new SharesList();

			SambaShareLoader.ParseNetUserShareList(
				list,
				@"[Share1]
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
		public void ParseSmbConfSharesList() {
			SharesList list = new SharesList();

			SambaShareLoader.ParseSmbConfShareList(
				list,
				@"#
# smb.conf can have comments starting with either '#', or ';'
# character. And usually has lots of 'em.
# Sorry for the clutter, but the best test is to parse a real-world
# smb.conf, so I slapped in mine, which is mostly [k]ubuntu's default.

#======================= Global Settings =======================

[global]

## Browsing/Identification ###

# Change this to the workgroup/NT-domain name your Samba server will part of
   workgroup = WORKGROUP

# server string is the equivalent of the NT Description field
	server string = %h server (Samba, Ubuntu)

# Windows Internet Name Serving Support Section:
# WINS Support - Tells the NMBD component of Samba to enable its WINS Server
#   wins support = no

# WINS Server - Tells the NMBD components of Samba to be a WINS Client
# Note: Samba can be either a WINS Server, or a WINS Client, but NOT both
;   wins server = w.x.y.z

# This will prevent nmbd to search for NetBIOS names through DNS.
   dns proxy = no

#### Networking ####

# The specific set of interfaces / networks to bind to
# This can be either the interface name or an IP address/netmask;
# interface names are normally preferred
;   interfaces = 127.0.0.0/8 eth0

# Only bind to the named interfaces and/or networks; you must use the
# 'interfaces' option above to use this.
# It is recommended that you enable this feature if your Samba machine is
# not protected by a firewall or is a firewall itself.  However, this
# option cannot handle dynamic or non-broadcast interfaces correctly.
;   bind interfaces only = yes



#### Debugging/Accounting ####

# This tells Samba to use a separate log file for each machine
# that connects
   log file = /var/log/samba/log.%m

# Cap the size of the individual log files (in KiB).
   max log size = 1000

# If you want Samba to only log through syslog then set the following
# parameter to 'yes'.
#   syslog only = no

# We want Samba to log a minimum amount of information to syslog. Everything
# should go to /var/log/samba/log.{smbd,nmbd} instead. If you want to log
# through syslog you should set the following parameter to something higher.
   syslog = 0

# Do something sensible when Samba crashes: mail the admin a backtrace
   panic action = /usr/share/samba/panic-action %d


####### Authentication #######

# Server role. Defines in which mode Samba will operate. Possible
# values are ""standalone server"", ""member server"", ""classic primary
# domain controller"", ""classic backup domain controller"", ""active
# directory domain controller"". 
#
# Most people will want ""standalone sever"" or ""member server"".
# Running as ""active directory domain controller"" will require first
# running ""samba-tool domain provision"" to wipe databases and create a
# new domain.
   server role = standalone server

# If you are using encrypted passwords, Samba will need to know what
# password database type you are using.  
   passdb backend = tdbsam

   obey pam restrictions = yes

# This boolean parameter controls whether Samba attempts to sync the Unix
# password with the SMB password when the encrypted SMB password in the
# passdb is changed.
   unix password sync = yes

# For Unix password sync to work on a Debian GNU/Linux system, the following
# parameters must be set (thanks to Ian Kahan <<kahan@informatik.tu-muenchen.de> for
# sending the correct chat script for the passwd program in Debian Sarge).
   passwd program = /usr/bin/passwd %u
   passwd chat = *Enter\snew\s*\spassword:* %n\n *Retype\snew\s*\spassword:* %n\n *password\supdated\ssuccessfully* .

# This boolean controls whether PAM will be used for password changes
# when requested by an SMB client instead of the program listed in
# 'passwd program'. The default is 'no'.
   pam password change = yes

# This option controls how unsuccessful authentication attempts are mapped
# to anonymous connections
   map to guest = bad user

########## Domains ###########

#
# The following settings only takes effect if 'server role = primary
# classic domain controller', 'server role = backup domain controller'
# or 'domain logons' is set 
#

# It specifies the location of the user's
# profile directory from the client point of view) The following
# required a [profiles] share to be setup on the samba server (see
# below)
;   logon path = \\%N\profiles\%U
# Another common choice is storing the profile in the user's home directory
# (this is Samba's default)
#   logon path = \\%N\%U\profile

# The following setting only takes effect if 'domain logons' is set
# It specifies the location of a user's home directory (from the client
# point of view)
;   logon drive = H:
#   logon home = \\%N\%U

# The following setting only takes effect if 'domain logons' is set
# It specifies the script to run during logon. The script must be stored
# in the [netlogon] share
# NOTE: Must be store in 'DOS' file format convention
;   logon script = logon.cmd

# This allows Unix users to be created on the domain controller via the SAMR
# RPC pipe.  The example command creates a user account with a disabled Unix
# password; please adapt to your needs
; add user script = /usr/sbin/adduser --quiet --disabled-password --gecos """" %u

# This allows machine accounts to be created on the domain controller via the 
# SAMR RPC pipe.  
# The following assumes a ""machines"" group exists on the system
; add machine script  = /usr/sbin/useradd -g machines -c ""%u machine account"" -d /var/lib/samba -s /bin/false %u

# This allows Unix groups to be created on the domain controller via the SAMR
# RPC pipe.  
; add group script = /usr/sbin/addgroup --force-badname %g

############ Misc ############

# Using the following line enables you to customise your configuration
# on a per machine basis. The %m gets replaced with the netbios name
# of the machine that is connecting
;   include = /home/samba/etc/smb.conf.%m

# Some defaults for winbind (make sure you're not using the ranges
# for something else.)
;   idmap uid = 10000-20000
;   idmap gid = 10000-20000
;   template shell = /bin/bash

# Setup usershare options to enable non-root users to share folders
# with the net usershare command.

# Maximum number of usershare. 0 (default) means that usershare is disabled.
;   usershare max shares = 100

# Allow users who've been granted usershare privileges to create
# public shares, not just authenticated ones
   usershare allow guests = yes

#======================= Share Definitions =======================

# Un-comment the following (and tweak the other settings below to suit)
# to enable the default home directory shares. This will share each
# user's home directory as \\server\username
;[homes]
;   comment = Home Directories
;   browseable = no

# By default, the home directories are exported read-only. Change the
# next parameter to 'no' if you want to be able to write to them.
;   read only = yes

# File creation mask is set to 0700 for security reasons. If you want to
# create files with group=rw permissions, set next parameter to 0775.
;   create mask = 0700

# Directory creation mask is set to 0700 for security reasons. If you want to
# create dirs. with group=rw permissions, set next parameter to 0775.
;   directory mask = 0700

# By default, \\server\username shares can be connected to by anyone
# with access to the samba server.
# Un-comment the following parameter to make sure that only ""username""
# can connect to \\server\username
# This might need tweaking when using external authentication schemes
;   valid users = %S

# Un-comment the following and create the netlogon directory for Domain Logons
# (you need to configure Samba to act as a domain controller too.)
;[netlogon]
;   comment = Network Logon Service
;   path = /home/samba/netlogon
;   guest ok = yes
;   read only = yes

# Un-comment the following and create the profiles directory to store
# users profiles (see the ""logon path"" option above)
# (you need to configure Samba to act as a domain controller too.)
# The path below should be writable by all users so that their
# profile directory may be created the first time they log on
;[profiles]
;   comment = Users profiles
;   path = /home/samba/profiles
;   guest ok = no
;   browseable = no
;   create mask = 0600
;   directory mask = 0700

[printers]
   comment = All Printers
   browseable = no
   path = /var/spool/samba
   printable = yes
   guest ok = no
   read only = yes
   create mask = 0700

[Share1]
path=/home/user/FA
browseable = yes
read only = yes
guest ok = yes

[share 2]
path=/mnt/movies and music/
browseable = yes
read only = yes
guest ok = yes

# Windows clients look for this share name as a source of downloadable
# printer drivers
[print$]
   comment = Printer Drivers
   path = /var/lib/samba/printers
   browseable = yes
   read only = yes
   guest ok = no
# Uncomment to allow remote administration of Windows print drivers.
# You may need to replace 'lpadmin' with the name of the group your
# admin users are members of.
# Please note that you also need to set appropriate Unix permissions
# to the drivers directory for these users to have write rights in it
;   write list = root, @lpadmin
"

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

