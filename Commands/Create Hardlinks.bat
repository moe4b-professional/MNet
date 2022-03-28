@echo off

:: BatchGotAdmin
:-------------------------------------
REM  --> Check for permissions
    IF "%PROCESSOR_ARCHITECTURE%" EQU "amd64" (
>nul 2>&1 "%SYSTEMROOT%\SysWOW64\cacls.exe" "%SYSTEMROOT%\SysWOW64\config\system"
) ELSE (
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
)

REM --> If error flag set, we do not have admin.
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    set params= %*
    echo UAC.ShellExecute "cmd.exe", "/c ""%~s0"" %params:"=""%", "", "runas", 1 >> "%temp%\getadmin.vbs"

    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
	
Rem -----------------------------------------------------------------------------------

cd ..

Rem LiteNetLib
rmdir "%cd%\MNet-Unity\Assets\MNet\Third Party\LiteNetLib\Source"
mklink /D "%cd%\MNet-Unity\Assets\MNet\Third Party\LiteNetLib\Source" "%cd%\MNet-Common\Third Party\LiteNetLib"
echo:

rmdir "%cd%\MNet-Core\LiteNetLib\Source"
mklink /D "%cd%\MNet-Core\LiteNetLib\Source" "%cd%\MNet-Common\Third Party\LiteNetLib"
echo:

Rem Shared Library
rmdir "%cd%\MNet-Unity\Assets\MNet\Shared"
mklink /D "%cd%\MNet-Unity\Assets\MNet\Shared" "%cd%\MNet-Common\Shared Library"
echo:

rmdir "%cd%\MNet-Core\Shared\Source"
mklink /D "%cd%\MNet-Core\Shared\Source" "%cd%\MNet-Common\Shared Library"
echo:

Pause