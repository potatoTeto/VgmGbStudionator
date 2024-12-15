@echo off
if not exist input mkdir input
if not exist output mkdir output
for /R %%f in (input\*.vgm) do (
    echo Processing: %%f
    "%~dp0\VgmGbStudionator.exe" "%%f"
    echo.
)
pause
