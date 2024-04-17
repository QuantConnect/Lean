
from AlgorithmImports import *
from RangeConsolidatorAlgorithm import RangeConsolidatorAlgorithm

### <summary>
### Example algorithm of how to use RangeConsolidator with Tick resolution
### </summary>
class RangeConsolidatorWithTickAlgorithm(RangeConsolidatorAlgorithm):
    def get_range(self):
        return 5

    def get_resolution(self):
        return Resolution.TICK

    def set_start_and_end_dates(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
