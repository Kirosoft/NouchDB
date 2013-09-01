@REM assumes we're being run from win directory as: build.bat

@ECHO OFF
SETLOCAL

IF "%1"=="Just32rel" GOTO Just32rel
IF "%1"=="Just32dbg" GOTO Just32dbg
IF "%1"=="Just64rel" GOTO Just64rel
IF "%1"=="Just64dbg" GOTO Just64dbg

CALL vc32.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=rel SRC=..
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=dbg SRC=..
IF ERRORLEVEL 1 EXIT /B 1

CALL vc64.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=rel PLATFORM=X64 SRC=..
F ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=dbg PLATFORM=X64 SRC=..
IF ERRORLEVEL 1 EXIT /B 1
goto END

:Just32rel
CALL vc32.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=rel SRC=..
IF ERRORLEVEL 1 EXIT /B 1
goto END

:Just32dbg
CALL vc32.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=dbg SRC=..
IF ERRORLEVEL 1 EXIT /B 1
goto END

:Just64rel
CALL vc64.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=rel PLATFORM=X64 SRC=..
IF ERRORLEVEL 1 EXIT /B 1
goto END

:Just64dbg
CALL vc64.bat
IF ERRORLEVEL 1 EXIT /B 1

nmake -f makefile.msvc CFG=dbg PLATFORM=X64 SRC=..
IF ERRORLEVEL 1 EXIT /B 1
goto END

:END
