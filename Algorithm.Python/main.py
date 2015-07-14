import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Interfaces")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Indicators")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *

class BasicTemplateAlgorithm(QCAlgorithm):

	def Initialize(self):
		self.SetCash(100000)
		self.SetStartDate(2013,10,07)
		self.SetEndDate(2013,10,11)
		self.AddSecurity(SecurityType.Equity, "SPY")

	def OnData(self, slice):

		if not self.Portfolio.Invested:
			self.SetHoldings("SPY", 1)	
			