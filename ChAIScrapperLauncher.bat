@echo off
REM Replace with your username in the path below. Example: "C:\Users\Karst\AppData\Local\Google\Chrome\User Data"
start "" "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="C:\Users\YOUR_WINDOWS_USER_NAME_HERE\AppData\Local\Google\Chrome\User Data"
start "" "ChAIScrapper.exe"
exit
