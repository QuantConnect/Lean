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
### This algorithm sends portfolio targets to CrunchDAO API once a week.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="securities and portfolio" />
class CrunchDAOSignalExportDemonstrationAlgorithm(QCAlgorithm):

    crunch_universe = []

    def Initialize(self):
        self.SetStartDate(2023, 5, 22)
        self.SetEndDate(2023, 5, 26)
        self.SetCash(1_000_000)

        # Connect to CrunchDAO
        api_key = ""            # Your CrunchDAO API key
        model = ""              # The Id of your CrunchDAO model
        submission_name = ""    # A name for the submission to distinguish it from your other submissions
        comment = ""            # A comment for the submission
        self.SignalExport.AddSignalExportProviders(CrunchDAOSignalExport(api_key, model, submission_name, comment))

        self.SetSecurityInitializer(BrokerageModelSecurityInitializer(self.BrokerageModel, FuncSecuritySeeder(self.GetLastKnownPrices)))

        # Add a custom data universe to read the CrunchDAO skeleton
        self.AddUniverse(CrunchDaoSkeleton, "CrunchDaoSkeleton", Resolution.Daily, self.select_symbols)

        # Create a Scheduled Event to submit signals every monday before the market opens
        self.week = -1
        self.Schedule.On(
            self.DateRules.Every([DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]), 
            self.TimeRules.At(13, 15, TimeZones.Utc), 
            self.submit_signals)

        self.Settings.MinimumOrderMarginPortfolioPercentage = 0

        self.SetWarmUp(timedelta(45))

    def select_symbols(self, data: List[CrunchDaoSkeleton]) -> List[Symbol]:
        return [x.Symbol for x in data]

    def OnSecuritiesChanged(self, changes):
        for security in changes.RemovedSecurities:
            if security in self.crunch_universe:
                self.crunch_universe.remove(security)
        self.crunch_universe.extend(changes.AddedSecurities)

    def submit_signals(self):
        if self.IsWarmingUp:
            return
        
        # Submit signals once per week
        week_num = self.Time.isocalendar()[1]
        if self.week == week_num:
            return
        self.week = week_num

        symbols = [security.Symbol for security in self.crunch_universe if security.Price > 0]

        # Get historical price data
        # close_prices = self.History(symbols, 22, Resolution.Daily).close.unstack(0)

        # Create portfolio targets
        weight_by_symbol = {symbol: 1/len(symbols) for symbol in symbols} # Add your logic here
        targets = [PortfolioTarget(symbol, weight) for symbol, weight in weight_by_symbol.items()]

        # (Optional) Place trades
        self.SetHoldings(targets)

        # Send signals to CrunchDAO
        success = self.SignalExport.SetTargetPortfolio(targets)
        if not success:
            self.Debug(f"Couldn't send targets at {self.Time}")


class CrunchDaoSkeleton(PythonData):
    
    def GetSource(self, config, date, isLive):
        return SubscriptionDataSource("https://tournament.crunchdao.com/data/skeleton.csv", SubscriptionTransportMedium.RemoteFile)

    def Reader(self, config, line, date, isLive):
        if not line[0].isdigit(): return None
        skeleton = CrunchDaoSkeleton()
        skeleton.Symbol = config.Symbol

        try:
            csv = line.split(',')
            skeleton.EndTime = (datetime.strptime(csv[0], "%Y-%m-%d")).date() 
            skeleton.Symbol =  Symbol(SecurityIdentifier.GenerateEquity(csv[1], Market.USA, mappingResolveDate=skeleton.Time), csv[1])
            skeleton["Ticker"] = csv[1]

        except ValueError:
            # Do nothing
            return None

        return skeleton