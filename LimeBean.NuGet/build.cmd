@echo off

set NUGET=c:\my\apps\nuget
set MSBUILD=c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /p:Configuration=Release /v:m
set META_FILES=..\LimeBean\AssemblyInfo.cs ..\LimeBean\project.json LimeBean.Xamarin.nuspec
set RESULT=ok

rd /s/q ..\LimeBean\bin
del *.nupkg

for %%f in (%META_FILES%) do (
    copy /y /b %%f %%f.bak
    powershell -File write-meta.ps1 %%f
)

call dnu pack ..\LimeBean --configuration Release --quiet || goto error
%MSBUILD% ..\LimeBean.Xamarin.sln || goto error
%NUGET% pack LimeBean.Xamarin.nuspec || goto error
copy ..\LimeBean\bin\Release\*.nupkg . || goto error
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