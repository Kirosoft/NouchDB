This is a binary distribution of LevelDB dlls for Windows.

Directory "32bit" contains 32-bit version of the dll and directory
"64bit" - 64-bit verision of the dll.

They both contain:
* libleveldb.dll and libleveldb.lib - dll and import library for the
  linker
* libleveldb.pdb - pdb file for debugging

Directory "include" contains header files needed to develop against
libleveldb.dll. You only need to include c.h file.

The dll was compiled with Visual Studio 2010 but should be usable
from any compiler that understn

For more information and latest release visit:
http://blog.kowalczyk.info/software/leveldb-for-windows/

To find out more about LevelDB visit http://code.google.com/p/leveldb/
