@echo off

if _%1==_ (
    echo Usage: %0 clr^|coreclr
    exit /b 1
)

xcopy /I System.Data.SQLite\x86 LimeBean.Tests\x86
xcopy /I System.Data.SQLite\x64 LimeBean.Tests\x64

powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}" || exit /b 1
call dnvm install latest -r %1 || exit /b 1
call dnu restore || exit /b 1
dnx LimeBean.Tests xunit.runner.dnx || exit /b 1