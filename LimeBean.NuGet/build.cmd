@echo off

set MSBUILD=c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release /v:m
set META_FILES=..\LimeBean\AssemblyInfo.cs ..\LimeBean.Dnx\LimeBean\project.json LimeBean.Xamarin.nuspec
set RESULT=ok

if not exist nuget.exe (
    powershell -Command "(New-Object Net.WebClient).DownloadFile('http://nuget.org/nuget.exe', 'nuget.exe')"
)

rd /s/q ..\LimeBean.Dnx\LimeBean\bin
del *.nupkg

for %%f in (%META_FILES%) do (
    copy /y /b %%f %%f.bak
    powershell -ExecutionPolicy RemoteSigned -File write-meta.ps1 %%f
)

call dnu restore ..\LimeBean.Dnx\LimeBean --quiet || goto error
call dnu pack ..\LimeBean.Dnx\LimeBean --configuration Release --quiet || goto error
%MSBUILD% ..\LimeBean.Xamarin\LimeBean.Xamarin.sln || goto error
nuget pack LimeBean.Xamarin.nuspec || goto error
copy ..\LimeBean.Dnx\LimeBean\bin\Release\*.nupkg . || goto error
del *symbols*.nupkg

goto cleanup

:error
set RESULT=fail

:cleanup
for %%f in (%META_FILES%) do (
    copy /y /b %%f.bak %%f
    del %%f.bak
)

if not %RESULT%==ok (
    echo ERRORS!
    exit /b 1
)

echo SUCCESS!