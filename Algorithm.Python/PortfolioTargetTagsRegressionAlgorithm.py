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
### Algorithm demonstrating the portfolio target tags usage
### </summary>
class PortfolioTargetTagsRegressionAlgorithm(QCAlgorithm):
    '''Algorithm demonstrating the portfolio target tags usage'''

    def Initialize(self):
        self.SetStartDate(2013,10,7)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.UniverseSettings.Resolution = Resolution.Minute

        symbols = [ Symbol.Create("SPY", SecurityType.Equity, Market.USA) ]

        # set algorithm framework models
        self.SetUniverseSelection(ManualUniverseSelectionModel(symbols))
        self.SetAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(minutes = 20), 0.025, None))

        self.SetPortfolioConstruction(CustomPortfolioConstructionModel())
        self.SetRiskManagement(CustomRiskManagementModel())
        self.SetExecution(CustomExecutionModel(self.SetTargetTagsChecked))

        self.targetTagsChecked = False

    def SetTargetTagsChecked(self):
        self.targetTagsChecked = True

    def OnEndOfAlgorithm(self):
        if not self.targetTagsChecked:
            raise Exception("The portfolio targets tag were not checked")

class CustomPortfolioConstructionModel(EqualWeightingPortfolioConstructionModel):
    def __init__(self):
        super().__init__(Resolution.Daily)

    def CreateTargets(self, algorithm: QCAlgorithm, insights: List[Insight]) -> List[IPortfolioTarget]:
        targets = super().CreateTargets(algorithm, insights)
        return CustomPortfolioConstructionModel.AddPPortfolioTargetsTags(targets)

    @staticmethod
    def GeneratePortfolioTargetTag(target: IPortfolioTarget) -> str:
        return f"Portfolio target tag: {target.Symbol} - {target.Quantity}"

    @staticmethod
    def AddPPortfolioTargetsTags(targets: List[IPortfolioTarget]) -> List[IPortfolioTarget]:
        return [PortfolioTarget(target.Symbol, target.Quantity, CustomPortfolioConstructionModel.GeneratePortfolioTargetTag(target))
                for target in targets]

class CustomRiskManagementModel(MaximumDrawdownPercentPerSecurity):
    def __init__(self):
        super().__init__(0.01)

    def ManageRisk(self, algorithm: QCAlgorithm, targets: List[IPortfolioTarget]) -> List[IPortfolioTarget]:
        riskManagedTargets = super().ManageRisk(algorithm, targets)
        return CustomPortfolioConstructionModel.AddPPortfolioTargetsTags(riskManagedTargets)

class CustomExecutionModel(ImmediateExecutionModel):
    def __init__(self, targetsTagCheckedCallback: Callable) -> None:
        super().__init__()
        self.targetsTagCheckedCallback = targetsTagCheckedCallback

    def Execute(self, algorithm: QCAlgorithm, targets: List[IPortfolioTarget]) -> None:
        if len(targets) > 0:
            self.targetsTagCheckedCallback()

        for target in targets:
            expectedTag = CustomPortfolioConstructionModel.GeneratePortfolioTargetTag(target)
            if target.Tag != expectedTag:
                raise Exception(f"Unexpected portfolio target tag: {target.Tag} - Expected: {expectedTag}")

        super().Execute(algorithm, targets)
