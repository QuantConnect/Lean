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

set docker_params=

set iqconnect_exec="C:\Program Files (x86)\DTN\IQFeed\iqconnect.exe"

set iqfeed_product_name=
set iqfeed_version=1.0
set iqfeed_login=
set iqfeed_password=

REM Do not edit after this line

set iqconnect_command=%iqconnect_exec%

if "%iqfeed_product_name%" == "" (
    echo Error: iqfeed_product_name must be set.
    goto :eof
) else (
    set iqconnect_command=%iqconnect_command% -product %iqfeed_product_name%
)

if "%iqfeed_version%" == "" (
    echo Error: iqfeed_version must be set.
    goto :eof
) else (
    set iqconnect_command=%iqconnect_command% -version %iqfeed_version%
)

if not "%iqfeed_login%" == "" (
    set iqconnect_command=%iqconnect_command% -login %iqfeed_login%
)

if not "%iqfeed_password%" == "" (
    set iqconnect_command=%iqconnect_command% -password %iqfeed_password%
)

echo IQConnect starting...
start "IQConnect" %iqconnect_command%

timeout /t 30

call run_docker.bat
