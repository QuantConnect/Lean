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
set default_config_file=%current_dir%Launcher\bin\Debug\config.json

if exist "%~1" (
    for /f "eol=- delims=" %%a in (%~1) do set "%%a"
) else (
    set /p image="Enter docker image [default: %default_image%]: "
    set /p config_file="Enter absolute path to Lean config file [default: %default_config_file%]: "
    set /p data_dir="Enter absolute path to Data folder [default: %default_data_dir%]: "
    set /p results_dir="Enter absolute path to store results [default: %default_results_dir%]: "
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

docker run --rm --mount type=bind,source=%config_file%,target=/Lean/config.json,readonly^
 --mount type=bind,source=%data_dir%,target=/Data,readonly^
 --mount type=bind,source=%results_dir%,target=/Results^
 %image% --data-folder /Data --results-destination-folder /Results --config /Lean/config.json

:script_exit
set image=
set data_dir=
set results_dir=
set config_file=