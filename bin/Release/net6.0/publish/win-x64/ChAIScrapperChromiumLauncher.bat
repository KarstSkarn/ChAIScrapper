@echo off
start "" ".\Chromium\chrome.exe" --remote-debugging-port=9222 --user-data-dir=".\Chromium\UserData"
REM start "" "ChAIScrapper.exe"
exit
