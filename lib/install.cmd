@echo off
pushd %~dp0
echo installing...
set p=c:\opt\bin
if not exist %p% mkdir %p%
copy ..\bin\*.exe %p%
popd
pause
