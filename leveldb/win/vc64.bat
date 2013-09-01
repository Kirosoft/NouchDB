@ECHO OFF

:TRY_VS10
CALL "%ProgramFiles%\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x64 2>NUL
IF NOT ERRORLEVEL 1 EXIT /B

:TRY_VS10_X86
CALL "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x64 2>NUL
IF NOT ERRORLEVEL 1 EXIT /B

REM Fail, if no Visual Studio installation has been found
ECHO Visual Studio 2010 doesn't seem to be installed
EXIT /B 1
