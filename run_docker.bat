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

set CURRENT_DIR=%~dp0
set DEFAULT_IMAGE=quantconnect/lean:latest
set DEFAULT_DATA_DIR=%CURRENT_DIR%Data\
set DEFAULT_RESULTS_DIR=%CURRENT_DIR%Results
set DEFAULT_CONFIG_FILE=%CURRENT_DIR%Launcher\config.json
set DEFAULT_PYTHON_DIR=%CURRENT_DIR%Algorithm.Python\
set CSHARP_DLL=%CURRENT_DIR%Launcher\bin\Debug\QuantConnect.Algorithm.CSharp.dll
set CSHARP_PDB=%CURRENT_DIR%Launcher\bin\Debug\QuantConnect.Algorithm.CSharp.pdb
set CONTAINER_NAME=LeanEngine

REM If the arg is a file load in the params from the file (run_docker.cfg)
if exist "%~1" (
    for /f "eol=- delims=" %%a in (%~1) do set "%%a"
    goto build_COMMAND
)

REM If the args are just inline args load them in, if not ask questions
:arg_loop
if not "%*"=="" (
    for /F "tokens=1*" %%a in ("%*") do (
        set %%a
        if NOT x%%b==x call :arg_loop %%b
    )
) else (
    set /p IMAGE="Enter docker image [default: %DEFAULT_IMAGE%]: "
    set /p CONFIG_FILE="Enter absolute path to Lean config file [default: %DEFAULT_CONFIG_FILE%]: "
    set /p DATA_DIR="Enter absolute path to Data folder [default: %DEFAULT_DATA_DIR%]: "
    set /p RESULTS_DIR="Enter absolute path to store results [default: %DEFAULT_RESULTS_DIR%]: "
    set /p PYTHON_DIR="Enter absolute path to Python directory [default: %DEFAULT_PYTHON_DIR%]: "
    set /p DEBUGGING="Would you like to debug C#? (Requires mono debugger attachment) [default: N]: "
    set /p UPDATE="Would you like to check for updates on the Docker image? [default: Y]: "	
)

:build_COMMAND

if "%IMAGE%" == "" (
    set IMAGE=%DEFAULT_IMAGE%
)

if "%CONFIG_FILE%" == "" (
    set CONFIG_FILE=%DEFAULT_CONFIG_FILE%
)

if "%PYTHON_DIR%" == "" (
    set PYTHON_DIR=%DEFAULT_PYTHON_DIR%
)

if "%UPDATE%" == "" (
    set UPDATE=Y
)

if not exist "%CONFIG_FILE%" (
    echo Lean config file '%CONFIG_FILE%' does not exist
    goto script_EXIT
)

if "%DATA_DIR%" == "" (
    set DATA_DIR=%DEFAULT_DATA_DIR%
)

if not exist "%DATA_DIR%" (
    echo Data directory '%DATA_DIR%' does not exist
    goto script_EXIT
)

if "%RESULTS_DIR%" == "" (
    set RESULTS_DIR=%DEFAULT_RESULTS_DIR%
)

if not exist "%RESULTS_DIR%" (
    echo Results directory '%RESULTS_DIR%' does not exist; creating it now;
    mkdir %RESULTS_DIR%
)

REM First part of the docker COMMAND that is static, then we build the rest
set COMMAND=docker run --rm^
    --mount type=bind,source=%CONFIG_FILE%,target=/Lean/Launcher/config.json,readonly^
    -v %DATA_DIR%:/Data:ro^
    -v %RESULTS_DIR%:/Results^
    --name %CONTAINER_NAME%^
    -p 5678:5678^
    --expose 6000

REM If DOCKER_PARAMS exist, add them to docker COMMAND
if not "%DOCKER_PARAMS%" == "" (
    set COMMAND=%COMMAND% %DOCKER_PARAMS%
    echo Applying additional docker parameters to docker COMMAND
)

REM If the csharp dll and pdb are present, mount them
if not exist "%CSHARP_DLL%" (
    echo Csharp file at '%CSHARP_DLL%' does not exist; no CSharp files will be mounted
) else (
    set COMMAND=%COMMAND% --mount type=bind,source=%CSHARP_DLL%,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.dll^
     --mount type=bind,source=%CSHARP_PDB%,target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.pdb
)

REM If python algorithms are present, mount them
if not exist "%PYTHON_DIR%" (
    echo No Python Algorithm location found at '%PYTHON_DIR%'; no Python files will be mounted
) else (
    set COMMAND=%COMMAND% -v %PYTHON_DIR%:/Lean/Algorithm.Python
)

REM If DEBUGGING is set then set the entrypoint to run mono with a debugger server
if /I "%DEBUGGING%" == "Y" (
    set COMMAND=%COMMAND% -p 55555:55555^
    --entrypoint mono^
    %IMAGE% --debug --debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555,suspend=y^
    QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json

    echo Docker container starting, attach to Mono process at localhost:55555 to begin
) else (
    set COMMAND=%COMMAND% %IMAGE% --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json
)

REM Pull the image if we want to update
if /I "%UPDATE%" == "Y" (
    echo Updating Docker Image
    docker pull %IMAGE%
)

REM Run built docker COMMAND
echo "Launching LeanEngine with command: "
echo "%COMMAND%"
%COMMAND%

:script_EXIT
REM If EXIT flag is set EXIT; this is needed for an odd batch bug running from VS Code
if /I "%EXIT%" == "Y" (
    exit
) else (
    pause
)
