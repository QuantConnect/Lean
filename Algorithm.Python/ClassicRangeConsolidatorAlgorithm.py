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


from AlgorithmImports import *
from RangeConsolidatorAlgorithm import RangeConsolidatorAlgorithm

### <summary>
### Example algorithm of how to use ClassicRangeConsolidator
### </summary>
class ClassicRangeConsolidatorAlgorithm(RangeConsolidatorAlgorithm):
    def create_range_consolidator(self):
        return ClassicRangeConsolidator(self.get_range())
    
    def on_data_consolidated(self, sender, range_bar):
        super().on_data_consolidated(sender, range_bar)

        if range_bar.volume == 0:
            raise AssertionError("All RangeBar's should have non-zero volume, but this doesn't")

