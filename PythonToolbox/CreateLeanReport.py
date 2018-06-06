# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#!/usr/bin/python

# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

"""
Usage:
    CreateLeanReport.py --backtest=hash.json [--output=output.html] [--user=user_data.json]
"""

from sys import argv, exit
from time import time
from quantconnect.LeanReportCreator import LeanReportCreator

if __name__ == "__main__":
    start = time()
    lrc = LeanReportCreator(argv[1:], False)
    lrc.create()
    lrc.clean()
    end = time()
    print(f"Lean report creation took {end-start:.2f} secs. File: {lrc.output}")
    exit(0)