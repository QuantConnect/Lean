# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License")  import *
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

from clr import AddReference
AddReference("System")

file_absolute_path = os.path.abspath(__file__)
for file in os.listdir(os.path.dirname(file_absolute_path)):
    if file.endswith(".dll"):
        AddReference(file.replace(".dll", ""))

from System  import *
from System.Collections  import *
from System.Collections.Generic  import *
from System.Linq  import *
from System.Globalization  import *
from QuantConnect  import *
from QuantConnect.Algorithm  import *
from QuantConnect.Algorithm.Framework  import *
from QuantConnect.Algorithm.Framework.Selection  import *
from QuantConnect.Algorithm.Framework.Alphas  import *
from QuantConnect.Algorithm.Framework.Portfolio  import *
from QuantConnect.Algorithm.Framework.Execution  import *
from QuantConnect.Algorithm.Framework.Risk  import *
from QuantConnect.Api  import *
from QuantConnect.Parameters  import *
from QuantConnect.Benchmarks  import *
from QuantConnect.Brokerages  import *
from QuantConnect.Util  import *
from QuantConnect.Interfaces  import *
from QuantConnect.Indicators  import *
from QuantConnect.Research  import *
from QuantConnect.Data  import *
from QuantConnect.Data.Consolidators  import *
from QuantConnect.Data.Custom  import *
from QuantConnect.Data.Fundamental  import *
from QuantConnect.Data.Market  import *
from QuantConnect.Data.UniverseSelection  import *
from QuantConnect.Notifications  import *
from QuantConnect.Orders  import *
from QuantConnect.Orders.Fees  import *
from QuantConnect.Orders.Fills  import *
from QuantConnect.Orders.Slippage  import *
from QuantConnect.Scheduling  import *
from QuantConnect.Securities  import *
from QuantConnect.Securities.Equity  import *
from QuantConnect.Securities.Forex  import *
from QuantConnect.Securities.Interfaces  import *
from QuantConnect.Configuration  import *

# Start an instance of an API class
api = Api()
api.Initialize(Config.GetInt("job-user-id", 1), 
    Config.Get("api-access-token", "default"),
    Config.Get("data-folder"))

# Loads composer so that we do not get an exception in the jupyter notebook
Composer.Instance

#Site packages path needs to be added back for jupyter
# otherwise Lean engine cannot import pandas, etc.
import sys
import site
site_packages_path = site.getsitepackages()
for site_package_path in site_packages_path:
    if site_package_path not in sys.path:
        sys.path.append(site_package_path)