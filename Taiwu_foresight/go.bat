//go.bat $(SolutionName) $(TargetPath)
set TAIWUDIR=G:\\Steam\\steamapps\\workshop\\content\\838350\\2875708969
set Project=%1%
set Plugin=%2%
::echo start
::echo %1%
::echo %2%
xcopy /y "..\\config.lua" "%TAIWUDIR%\\"
xcopy /y "..\\cover.jpg" "%TAIWUDIR%\\"
::xcopy /y "..\\settings.lua" "%TAIWUDIR%\\Mod\\%Project%\\"
xcopy /y "%Plugin%" "%TAIWUDIR%\\Plugins\\"