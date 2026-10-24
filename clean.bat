@echo off
pushd "%~dp0"
echo Cleaning project-local build cache...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
popd
echo Done.
