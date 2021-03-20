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
set DEFAULT_IMAGE=quantconnect/research:latest
set DEFAULT_DATA_DIR=%current_dir%..\Data\
set DEFAULT_NOTEBOOK_DIR=%current_dir%Notebooks\
set CONTAINER_NAME=LeanResearch
set WORK_DIR=/Lean/Launcher/bin/Debug/

REM If the arg is a file load in the params from the file (run_docker.cfg)
if exist "%~1" (
    for /f "eol=- delims=" %%a in (%~1) do set "%%a"
    goto verify
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
    set /p DATA_DIR="Enter absolute path to Data folder [default: %DEFAULT_DATA_DIR%]: "
    set /p NOTEBOOK_DIR="Enter absolute path to store notebooks [default: %DEFAULT_NOTEBOOK_DIR%]: "
    set /p UPDATE="Would you like to check for updates on the Docker image? [default: Y]: "	
)

:verify

if "%IMAGE%" == "" (
    set IMAGE=%DEFAULT_IMAGE%
)

if "%NOTEBOOK_DIR%" == "" (
    set NOTEBOOK_DIR=%DEFAULT_NOTEBOOK_DIR%
)

if "%UPDATE%" == "" (
    set UPDATE=Y
)

if not exist "%NOTEBOOK_DIR%" (
    mkdir %NOTEBOOK_DIR%
)

if "%DATA_DIR%" == "" (
    set DATA_DIR=%DEFAULT_DATA_DIR%
)

if not exist "%DATA_DIR%" (
    echo Data directory '%DATA_DIR%' does not exist
    goto script_exit
)

REM Pull the image if we want to update
if /I "%UPDATE%" == "Y" (
    echo Updating Docker Image
    docker pull %IMAGE%
)

echo Starting docker container; container id is:
 docker run -d --rm -p 8888:8888^
    -v %DATA_DIR%:/home/Data:ro^
    -v %NOTEBOOK_DIR%:/Lean/Launcher/bin/Debug/Notebooks^
    --name %CONTAINER_NAME%^
    %IMAGE%

echo Docker container started; will wait 2 seconds before opening web browser.
timeout 2 /nobreak
start "" http://localhost:8888/lab

:script_exit