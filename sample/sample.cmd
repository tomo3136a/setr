@echo off
setlocal
cls

setr -i "%~0" -o env.bat
if %ERRORLEVEL% neq 0 (
echo 中断しました。
pause
exit -1
)
call env.bat

echo AAA=%AAA%
echo BBB=%BBB%
echo CCC=%CCC%
echo DDD=%DDD%
echo EEE=%EEE%

endlocal
pause
goto :eof

rem # コメント
rem * AAA=12
rem * -p BBB -m 入力してください。
rem * -y CCC
rem * -f DDD
rem * -g EEE
