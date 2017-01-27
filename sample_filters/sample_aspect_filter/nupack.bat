@echo off
..\..\.nuget\nuget.exe pack sample_filter.nuspec -Properties Version=%1 -OutputDirectory ..
