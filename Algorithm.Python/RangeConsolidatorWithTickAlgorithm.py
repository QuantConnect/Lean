
from AlgorithmImports import *
from RangeConsolidatorAlgorithm import RangeConsolidatorAlgorithm

### <summary>
### Example algorithm of how to use RangeConsolidator with Tick resolution
### </summary>
class RangeConsolidatorWithTickAlgorithm(RangeConsolidatorAlgorithm):
    def GetRange(self):
        return 1

    def GetResolution(self):
        return Resolution.Tick

    def SetStartAndEndDates(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)
