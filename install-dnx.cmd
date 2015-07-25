@echo off

if _%1==_ (
    echo Usage: %0 clr^|coreclr
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}" || exit /b 1
call %userprofile%\.dnx\bin\dnvm install latest -r %1 || exit /b 1