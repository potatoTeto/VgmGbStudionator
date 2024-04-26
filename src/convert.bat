@echo off
if not exist input mkdir input
if not exist output mkdir output
cd input
for /R %%f in (*.vgm) do %~dp0/furGBVGMHeaderRemover.exe "%%f"
pause