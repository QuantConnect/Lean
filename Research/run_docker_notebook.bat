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
set default_image=quantconnect/research:latest
set default_data_dir=%current_dir%..\Data\
set default_notebook_dir=%current_dir%Notebooks\
set config_file=%current_dir%Notebooks\config.json
set work_dir=/Lean/Launcher/bin/Debug/

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
    set /p data_dir="Enter absolute path to Data folder [default: %default_data_dir%]: "
    set /p notebook_dir="Enter absolute path to store notebooks [default: %default_notebook_dir%]: "
)

if "%notebook_dir%" == "" (
    set notebook_dir=%default_notebook_dir%
)

if not exist "%notebook_dir%" (
    mkdir %notebook_dir%
)

if not exist "%config_file%" (
    echo { "data-folder": "/home/Data/", "composer-dll-directory": "%work_dir%", "algorithm-language": "Python", "messaging-handler": "QuantConnect.Messaging.Messaging", "job-queue-handler": "QuantConnect.Queues.JobQueue", "api-handler": "QuantConnect.Api.Api" } > %notebook_dir%config.json
)

if "%data_dir%" == "" (
    set data_dir=%default_data_dir%
)

if not exist "%data_dir%" (
    echo Data directory '%data_dir%' does not exist
    goto script_exit
)


docker run -it --rm -p 8888:8888^
    --mount type=bind,source=%data_dir%,target=/home/Data,readonly^
    --mount type=bind,source=%notebook_dir%,target=/Lean/Launcher/bin/Debug/Notebooks^
    quantconnect/research jupyter lab --ip='0.0.0.0' --port=8888 --no-browser --allow-root --notebook-dir="Notebooks"




:script_exit
pause