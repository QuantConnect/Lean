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

### <summary>
### Algorithm asserting the correct values for the deployment target and algorithm mode.
### </summary>
class AlgorithmModeAndDeploymentTargetAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10, 7)
        self.SetEndDate(2013,10,11)
        self.SetCash(100000)

        #translate commented code from c# to python
        self.Debug(f"Algorithm Mode: {self.AlgorithmMode}. Is Live Mode: {self.LiveMode}. Deployment Target: {self.DeploymentTarget}.")

        if self.AlgorithmMode != AlgorithmMode.Backtesting:
            raise Exception(f"Algorithm mode is not backtesting. Actual: {self.AlgorithmMode}")

        if self.LiveMode:
            raise Exception("Algorithm should not be live")

        if self.DeploymentTarget != DeploymentTarget.LocalPlatform:
            raise Exception(f"Algorithm deployment target is not local. Actual{self.DeploymentTarget}")

        # For a live deployment these checks should pass:
        # if self.AlgorithmMode != AlgorithmMode.Live: raise Exception("Algorithm mode is not live")
        # if not self.LiveMode: raise Exception("Algorithm should be live")

        # For a cloud deployment these checks should pass:
        # if self.DeploymentTarget != DeploymentTarget.CloudPlatform: raise Exception("Algorithm deployment target is not cloud")

        self.Quit()
