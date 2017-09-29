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

from clr import AddReference
AddReference("System.Core")
AddReference("System.Collections")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Algorithm")

from System import *
from System.Collections.Generic import List
from QuantConnect import *
from QuantConnect.Algorithm import QCAlgorithm
from QuantConnect.Data.UniverseSelection import *

### <summary>
### This algorithm shows how you can handle universe selection in anyway you like,
### at any time you like. This algorithm has a list of 10 stocks that it rotates
### through every hour.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="custom universes" />
class UserDefinedUniverseAlgorithm(QCAlgorithm):

	def Initialize(self):
		self.SetCash(100000)
		self.SetStartDate(2015,1,1)
		self.SetEndDate(2015,12,1)
		self.symbols = [ "SPY", "GOOG", "IBM", "AAPL", "MSFT", "CSCO", "ADBE", "WMT"]

		self.UniverseSettings.Resolution = Resolution.Hour
		self.AddUniverse('my_universe_name', Resolution.Hour, self.selection)

	def selection(self, time):
		index = time.hour%len(self.symbols)
		return self.symbols[index]

	def OnData(self, slice):
		pass

	def OnSecuritiesChanged(self, changes):
		for removed in changes.RemovedSecurities:
			if removed.Invested:
				self.Liquidate(removed.Symbol)

		for added in changes.AddedSecurities:
			self.SetHoldings(added.Symbol, 1/float(len(changes.AddedSecurities)))