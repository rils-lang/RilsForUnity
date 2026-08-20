@echo off
setlocal
python "%~dp0rils_for_unity.py" close %*
set "exit_code=%errorlevel%"
if not "%exit_code%"=="0" pause
exit /b %exit_code%
