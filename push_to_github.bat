@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist .git (
    git init
    git add .
    git commit -m "Initial commit"
    git branch -M main
    git remote add origin https://github.com/KoTzZiNsKi/Remote.git
) else (
    git remote add origin https://github.com/KoTzZiNsKi/Remote.git 2>nul
    git add .
    git commit -m "Update" 2>nul
)
git push -u origin main
pause
