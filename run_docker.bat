@echo off

REM QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
REM Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
REM
REM Licensed under the Apache License, Version 2.0 (the "License");
REM you may not use this file except in compliance with the License.
REM You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
REM
REM Unless required by applicable law or agreed to in writing, software
REM distributed under the License is distributed on an "AS IS" BASIS,
REM WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
REM See the License for the specific language governing permissions and
REM limitations under the License.

set current_dir=%~dp0
set default_image=quantconnect/lean:latest
set default_data_dir=%current_dir%Data\
set default_results_dir=%current_dir%
set default_config_file=%current_dir%Launcher\config.json
set default_python_dir=%current_dir%Algorithm.Python\
set csharp_dll=%current_dir%Launcher\bin\Debug\QuantConnect.Algorithm.CSharp.dll
set csharp_pdb=%current_dir%Launcher\bin\Debug\QuantConnect.Algorithm.CSharp.pdb

REM If the arg is a file load in the params from the file (run_docker.cfg)
if exist "%~1" (
    for /f "eol=- delims=" %%a in (%~1) do set "%%a"
    goto build_command
)

REM If the args are just inline args load them in, if not ask questions
:arg_loop
if not "%*"=="" (
    for /F "tokens=1*" %%a in ("%*") do (
        set %%a
        if NOT x%%b==x call :arg_loop %%b
    )
) else (
    set /p image="Enter docker image [default: %default_image%]: "
    set /p config_file="Enter absolute path to Lean config file [default: %default_config_file%]: "
    set /p data_dir="Enter absolute path to Data folder [default: %default_data_dir%]: "
    set /p results_dir="Enter absolute path to store results [default: %default_results_dir%]: "
    set /p debugging="Would you like to debug C#? (Requires mono debugger attachment) [default: N]: "	
)

:build_command

if "%image%" == "" (
    set image=%default_image%
)

if "%config_file%" == "" (
    set config_file=%default_config_file%
)

if "%python_dir%" == "" (
    set python_dir=%default_python_dir%
)

if not exist "%config_file%" (
    echo Lean config file '%config_file%' does not exist
    goto script_exit
)

if "%data_dir%" == "" (
    set data_dir=%default_data_dir%
)

if not exist "%data_dir%" (
    echo Data directory '%data_dir%' does not exist
    goto script_exit
)

if "%results_dir%" == "" (
    set results_dir=%default_results_dir%
)

if not exist "%results_dir%" (
    echo Results directory '%results_dir%' does not exist
    goto script_exit
)

REM First part of the docker command that is static, then we build the rest
set command=docker run --rm --mount type=bind,source=%config_file%,target=/Lean/Launcher/config.json,readonly^
    --mount type=bind,source=%data_dir%,target=/Data,readonly^
    --mount type=bind,source=%results_dir%,target=/Results^
    --name LeanEngine^
    -p 5678:5678

REM If docker_params exist, add them to docker command
if not "%docker_params%" == "" (
    set command=%command% %docker_params%
    echo Applying additional docker parameters to docker command
)

REM If the csharp dll and pdb are present, mount them
if not exist "%csharp_dll%" (
    echo Csharp file at '%csharp_dll%' does not exist; no CSharp files will be mounted
) else (
    set command=%command% --mount type=bind,source=%csharp_dll%,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.dll^
     --mount type=bind,source=%csharp_pdb%,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.pdb
)

REM If python algorithms are present, mount them
if not exist "%python_dir%" (
    echo No Python Algorithm location found at '%python_dir%'; no Python files will be mounted
) else (
    set command=%command% --mount type=bind,source=%python_dir%,target=/Lean/Algorithm.Python
)

REM If debugging is set then set the entrypoint to run mono with a debugger server
if /I "%debugging%" == "Y" (
    set command=%command% -p 55555:55555^
    --entrypoint mono^
    %image% --debug --debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555,suspend=y^
    QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json

    echo Docker container starting, attach to Mono process at localhost:55555 to begin
) else (
    set command=%command% %image% --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json
)

REM Run built docker command
%command%

:script_exit
REM If exit flag is set exit; this is needed for an odd batch bug running from VS Code
if /I "%exit%" == "Y" (
    exit
) else (
    pause
)
