@echo off
setlocal
cls

setr -i "%~0" -o env.cmd
if %ERRORLEVEL% neq 0 (
echo ���f���܂����B
pause
exit -1
)
call env.cmd

echo AAA=%AAA%
echo BBB=%BBB%
echo CCC=%CCC%
echo DDD=%DDD%
echo EEE=%EEE%

endlocal
pause
goto :eof

rem # �R�����g
rem * AAA=12
rem * -p BBB -m ���͂��Ă��������B
rem * -y CCC
rem * -f DDD
rem * -g EEE
