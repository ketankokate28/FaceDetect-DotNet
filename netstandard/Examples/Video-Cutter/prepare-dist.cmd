cd src
dotnet publish -c Release --self-contained -r win-x64 -o .\publish
vpk pack -u SimpleVideoCutterApp -v 0.31.0 -p .\publish -e SimpleVideoCutter.exe --packTitle "Simple Video Cutter"