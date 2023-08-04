
from AlgorithmImports import *
from RangeConsolidatorWithTickAlgorithm import RangeConsolidatorWithTickAlgorithm

### <summary>
### Example algorithm of how to use ClassicRangeConsolidator with Tick resolution
### </summary>
class ClassicRangeConsolidatorWithTickAlgorithm(RangeConsolidatorWithTickAlgorithm):
    def CreateRangeConsolidator(self):
        return ClassicRangeConsolidator(self.GetRange())
    
    def OnDataConsolidated(self, sender, rangeBar):
        super().OnDataConsolidated(sender, rangeBar)

        if rangeBar.Volume == 0:
            raise Exception("All RangeBar's should have non-zero volume, but this doesn't")
