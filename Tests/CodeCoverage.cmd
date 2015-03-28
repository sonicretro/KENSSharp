@echo off
mkdir Coverage >nul 2>&1
..\packages\OpenCover.4.5.3723\OpenCover.Console.exe -register:user "-target:..\packages\xunit.runner.console.2.0.0\tools\xunit.console.exe" "-targetargs:bin\Debug\SonicRetro.KensSharp.Tests.dll -noshadow" "-output:Coverage\Coverage.xml" "-filter:+[SonicRetro.KensSharp.*]*"
..\packages\ReportGenerator.2.1.4.0\ReportGenerator.exe "-reports:Coverage\Coverage.xml" "-targetdir:.\Coverage"
