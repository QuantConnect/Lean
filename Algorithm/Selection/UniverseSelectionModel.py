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

class UniverseSelectionModel:
    '''Provides a base class for universe selection models.'''

    def get_next_refresh_time_utc(self) -> datetime:
        '''Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.'''
        if hasattr(self, "GetNextRefreshTimeUtc") and callable(self.GetNextRefreshTimeUtc):
            return self.GetNextRefreshTimeUtc()
        return datetime.max

    def create_universes(self, algorithm: QCAlgorithm) -> list[Universe]:
        '''Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        Args:
            algorithm: The algorithm instance to create universes for</param>
        Returns:
            The universes to be used by the algorithm'''
        if hasattr(self, "CreateUniverses") and callable(self.CreateUniverses):
            return self.CreateUniverses(algorithm)
        raise NotImplementedError("Types deriving from 'UniverseSelectionModel' must implement the 'def CreateUniverses(QCAlgorithm) method.")
