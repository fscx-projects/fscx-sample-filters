@echo off
set nupkg_ver=0.8.5

del /q *.nupkg

cd sample_functional_filter
call nupack.bat %nupkg_ver%
cd ..

cd sample_inheritable_filter
call nupack.bat %nupkg_ver%
cd ..

cd sample_aspect_filter
call nupack.bat %nupkg_ver%
cd ..
