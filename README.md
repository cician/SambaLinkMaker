# About
SambaLinkMaker is a simple command line utility to convert local file paths to
UNC or smb:// links on Linux.

# Usage
This program is run from command line. It accepts one or more local file paths
and outputs the corresponding UNC or smb links to standard output. Default is
UNC paths to share with your Windows running buddies. Just run the program for
full list of options.

# Compiling
For now I'm simply compiling from MonoDevelop. All dependencies are pulled from
NuGet.

# Compiling? What's that?
Just grab the [latest binary release](../../releases/latest).
I develop and test SambaLinkMaker only on [K]Ubuntu Linux only.
You'll still need to install the mono runtime.
    sudo apt-get install mono-runtime

And of course the whole thing only makes sense if you have samba file sharing
configured and running, but it's out of scope of this readme. To access the list
of shares this program uses the Samba's
[net](https://www.samba.org/samba/docs/man/manpages/net.8.html) command. I think
it's installed by default with samba.

# Packaging
To create windows .msi installer you need to install
[wixl](https://wiki.gnome.org/msitools).
    sudo apt-get install wixl
The run
    wixl WindowsInstaller.wxs
    
For .tar.gz releases I'm using MonoDevelop's linux deployment feature.

Regenerating the icons from SVGs:
    mogrify -path Resources -format ico -density 600 -define icon:auto-resize=256,128,64,48,32,24,16 Resources/*.svg

# OS integration
## Windows MSI installer.
Adds a Copy context menu in Explorer with various choices. In current form
it's fairy limited, but works. Know issues/limits:
- Only accepts one file or directory at a time. Overcoming this limitation
requires writing a shell extension. I know there's sharp shell, but seemes a bit
of an overkill. Another option would be to include
(this little program)[https://github.com/zenden2k/context-menu-launcher]
- Copying a drive path results in wrong link. This seems a windows bug to me.
Only tested on 64 bit Windows 7.

# Licensing
SambaLinkMaker is under the [MIT License](LICENSE).