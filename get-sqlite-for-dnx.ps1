$v = "3090200"

wget "https://www.sqlite.org/2015/sqlite-dll-win32-x86-$v.zip" -OutFile "sqlite.x86.zip"
wget "https://www.sqlite.org/2015/sqlite-dll-win64-x64-$v.zip" -OutFile "sqlite.x64.zip"

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory("sqlite.x86.zip", "LimeBean.Dnx\LimeBean.Tests\x86")
[IO.Compression.ZipFile]::ExtractToDirectory("sqlite.x64.zip", "LimeBean.Dnx\LimeBean.Tests\x64")
