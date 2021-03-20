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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Api")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Configuration")
AddReference("QuantConnect.Research")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Api import *
from QuantConnect.Configuration import *
from QuantConnect.Data import *
from QuantConnect.Research import *
from QuantConnect.Indicators import *

# Start an instance of an API class
api = Api()
api.Initialize(Config.GetInt("job-user-id", 1), 
    Config.Get("api-access-token", "default"),
    Config.Get("data-folder"))