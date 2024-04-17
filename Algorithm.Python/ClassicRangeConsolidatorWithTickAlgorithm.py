
from AlgorithmImports import *
from RangeConsolidatorWithTickAlgorithm import RangeConsolidatorWithTickAlgorithm

### <summary>
### Example algorithm of how to use ClassicRangeConsolidator with Tick resolution
### </summary>
class ClassicRangeConsolidatorWithTickAlgorithm(RangeConsolidatorWithTickAlgorithm):
    def create_range_consolidator(self):
        return ClassicRangeConsolidator(self.get_range())
    
    def on_data_consolidated(self, sender, range_bar):
        super().on_data_consolidated(sender, range_bar)

        if range_bar.volume == 0:
            raise Exception("All RangeBar's should have non-zero volume, but this doesn't")
