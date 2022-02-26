#!/bin/sh

if [ $# -ne 1 ]; then
	echo "this script packages a directory of your choice into a self-unpacking batch script"
	echo "usage: $0 <directory> > my_script.bat"
	echo "uses more/certutil/vbs during unpacking, but those should be available by default!"
	exit 1
fi

# header which chops the base64 zip file at the end of the file
cat <<HEADER
@echo off
more +8 %~0 > temp.base64
certutil -decode temp.base64 temp.zip
HEADER

# a vbs script used to unzip the zip file we just dropped
# we base64 this file too to prevent shenanigans
echo -n "echo "
base64 -w 0 - <<UNZIPPER
@echo off
setlocal
rem cd /d %~dp0
Call :UnZipFile "%~f2" "%~f1"
rem Call :UnZipFile "C:\Temp\" "c:\path\to\batch.zip"
exit /b

:UnZipFile <ExtractTo> <newzipfile>
set vbs="%temp%\_.vbs"
if exist %vbs% del /f /q %vbs%
>%vbs%  echo Set fso = CreateObject("Scripting.FileSystemObject")
>>%vbs% echo If NOT fso.FolderExists(%1) Then
>>%vbs% echo fso.CreateFolder(%1)
>>%vbs% echo End If
>>%vbs% echo set objShell = CreateObject("Shell.Application")
>>%vbs% echo set FilesInZip=objShell.NameSpace(%2).items
>>%vbs% echo objShell.NameSpace(%1).CopyHere(FilesInZip)
>>%vbs% echo Set fso = Nothing
>>%vbs% echo Set objShell = Nothing
cscript //nologo %vbs%
if exist %vbs% del /f /q %vbs%
UNZIPPER
echo " > temp.base64"

# decode the unzipper, call the unzipper, then clean up
cat <<HEADER2
certutil -decode temp.base64 unzip.bat
call unzip.bat temp.zip %~n0
del temp.base64 temp.zip unzip.bat %~0
exit /b
HEADER2

# zip and base64 the directory we are packing
zip -qq -r temp.zip $1
cat temp.zip | base64 -w 0 -
rm -f temp.zip
