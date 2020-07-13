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

if exist "%~1" (
    for /f "eol=- delims=" %%a in (%~1) do set "%%a"
) else (
    set /p image="Enter docker image [default: %default_image%]: "
    set /p config_file="Enter absolute path to Lean config file [default: %default_config_file%]: "
    set /p data_dir="Enter absolute path to Data folder [default: %default_data_dir%]: "
    set /p results_dir="Enter absolute path to store results [default: %default_results_dir%]: "
    set /p custom_algorithm="Are you using a custom algorithm? (Must be defined in config) [Y/N default: N]: "	
)

if "%image%" == "" (
    set image=%default_image%
)

if "%config_file%" == "" (
    set config_file=%default_config_file%
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

if /I "%custom_algorithm%" == "Y" (
    goto attach_custom_algorithm
) else (
    docker run --rm --mount type=bind,source=%config_file%,target=/Lean/Launcher/config.json,readonly^
    --mount type=bind,source=%data_dir%,target=/Data,readonly^
    --mount type=bind,source=%results_dir%,target=/Results^
    -p 55555:55555 -p 5678:5678^
    --name LeanEngine^
    --entrypoint mono^
    %image% --debug --debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555^
    QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json

    goto script_exit
)

:attach_custom_algorithm
set /p question="Is it a C# algorithm? (Ensure compiled if so!) [Y/N default: Y]: "

if /I "%question%" == "N" (
    set /p attach_algorithm="Enter python algorithm name [include .py]: "
    set algorithm_location=%current_dir%Algorithm.Python\%attach_algorithm%
    set algorithm_destination=/Lean/Algorithm.Python/%attach_algorithm%
) else (
    set attach_algorithm=QuantConnect.Algorithm.CSharp.dll
    set algorithm_location=%current_dir%Launcher\bin\Debug\%attach_algorithm%
    set algorithm_destination=/Lean/Launcher/bin/Debug/%attach_algorithm%
)

if not exist "%algorithm_location%" (
    echo Algorithm file %attach_algorithm% does not exist at %algorithm_location%
    goto script_exit
)

docker run --rm --mount type=bind,source=%config_file%,target=/Lean/Launcher/config.json,readonly^
    --mount type=bind,source=%data_dir%,target=/Data,readonly^
    --mount type=bind,source=%results_dir%,target=/Results^
    --mount type=bind,source=%algorithm_location%,target=%algorithm_destination%^
    -p 55555:55555 -p 5678:5678^
    --name LeanEngine^
    --entrypoint mono^
    %image% --debug --debugger-agent=transport=dt_socket,server=y,address=0.0.0.0:55555^
    QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json

:script_exit

set image=
set data_dir=
set results_dir=
set config_file=
set question =
set attach_algorithm=
set algorithm_location=
set algorithm_destination=