# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# This Python script can be loaded in a notebook (ipynb file)
# in order to reference QuantConnect assemblies
#
# Usage:
# %run "start.py"

import clr_loader
import os
from pythonnet import set_runtime

# The runtimeconfig.json is stored alongside start.py, but start.py may be a
# symlink and the directory start.py is stored in is not necessarily the
# current working directory. We therefore construct the absolute path to the
# start.py file, and find the runtimeconfig.json relative to that.
set_runtime(clr_loader.get_coreclr(os.path.join(os.path.dirname(os.path.realpath(__file__)), "QuantConnect.Lean.Launcher.runtimeconfig.json")))

from AlgorithmImports import *

# Used by pythonNet
AddReference("Fasterflect")

Config.Reset()
Initializer.Start()
api = Initializer.GetSystemHandlers().Api
algorithmHandlers = Initializer.GetAlgorithmHandlers(researchMode=True)

# Required to configure pythonpath with additional paths the user may have 
# set in the config, like a project library.
PythonInitializer.Initialize(False)

try:
    get_ipython().run_line_magic('matplotlib', 'inline')
except NameError:
    # can happen if start is triggered from python and not Ipython
    pass
