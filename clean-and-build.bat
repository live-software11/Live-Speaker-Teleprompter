@echo off
REM Live Speaker Teleprompter - Clean & Build
REM Doppio clic per pulire e ricreare setup + portable (output in release/)
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0clean-and-build.ps1"
if errorlevel 1 pause
