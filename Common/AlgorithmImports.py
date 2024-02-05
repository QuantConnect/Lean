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

import os
import sys

# The runtimeconfig.json is stored alongside start.py, but start.py may be a
# symlink and the directory start.py is stored in is not necessarily the
# current working directory. We therefore construct the absolute path to the
# start.py file, and find the runtimeconfig.json relative to that.
path = os.path.dirname(os.path.realpath(__file__))

from clr import AddReference
AddReference("System")

#Load assemblies
for file in os.listdir(path):
    if file.endswith(".dll") and file.startswith("QuantConnect."):
        AddReference(file.replace(".dll", ""))

from System import *
from System.Drawing import *

from QuantConnect import *
from QuantConnect.Api import *
from QuantConnect.Util import *
from QuantConnect.Data import *
from QuantConnect.Orders import *
from QuantConnect.Python import *
from QuantConnect.Storage import *
from QuantConnect.Research import *
from QuantConnect.Algorithm import *
from QuantConnect.Statistics import *
from QuantConnect.Parameters import *
from QuantConnect.Benchmarks import *
from QuantConnect.Brokerages import *
from QuantConnect.Securities import *
from QuantConnect.Indicators import *
from QuantConnect.Interfaces import *
from QuantConnect.Scheduling import *
from QuantConnect.DataSource import *
from QuantConnect.Orders.Fees import *
from QuantConnect.Data.Custom import *
from QuantConnect.Data.Market import *
from QuantConnect.Lean.Engine import *
from QuantConnect.Orders.Fills import *
from QuantConnect.Configuration import *
from QuantConnect.Notifications import *
from QuantConnect.Data.Auxiliary import *
from QuantConnect.Data.Shortable import *
from QuantConnect.Orders.Slippage import *
from QuantConnect.Securities.Forex import *
from QuantConnect.Data.Fundamental import *
from QuantConnect.Securities.Crypto import *
from QuantConnect.Securities.Option import *
from QuantConnect.Securities.Equity import *
from QuantConnect.Securities.Future import *
from QuantConnect.Data.Consolidators import *
from QuantConnect.Orders.TimeInForces import *
from QuantConnect.Algorithm.Framework import *
from QuantConnect.Algorithm.Selection import *
from QuantConnect.Securities.Positions import *
from QuantConnect.Orders.OptionExercise import *
from QuantConnect.Securities.Volatility import *
from QuantConnect.Securities.Interfaces import *
from QuantConnect.Data.UniverseSelection import *
from QuantConnect.Data.Custom.IconicTypes import *
from QuantConnect.Securities.CryptoFuture import *
from QuantConnect.Algorithm.Framework.Risk import *
from QuantConnect.Algorithm.Framework.Alphas import *
from QuantConnect.Algorithm.Framework.Execution import *
from QuantConnect.Algorithm.Framework.Portfolio import *
from QuantConnect.Algorithm.Framework.Portfolio.SignalExports import *
from QuantConnect.Algorithm.Framework.Selection import *

try:
    import numpy as np
    import pandas as pd
    import matplotlib.pyplot as plt
except:
    pass

from datetime import date, time, datetime, timedelta
from typing import *
import math
import json

QCAlgorithmFramework = QCAlgorithm
QCAlgorithmFrameworkBridge = QCAlgorithm
