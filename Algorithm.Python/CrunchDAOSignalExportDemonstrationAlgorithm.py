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

    def initialize(self) -> None:
        self.set_start_date(2023, 5, 22)
        self.set_end_date(2023, 5, 26)
        self.set_cash(1_000_000)

        # Disable automatic exports as we manually set them
        self.signal_export.automatic_export_time_span = None

        # Connect to CrunchDAO
        api_key = ""            # Your CrunchDAO API key
        model = ""              # The Id of your CrunchDAO model
        submission_name = ""    # A name for the submission to distinguish it from your other submissions
        comment = ""            # A comment for the submission
        self.signal_export.add_signal_export_provider(CrunchDAOSignalExport(api_key, model, submission_name, comment))

        self.set_security_initializer(BrokerageModelSecurityInitializer(self.brokerage_model, FuncSecuritySeeder(self.get_last_known_prices)))

        # Add a custom data universe to read the CrunchDAO skeleton
        self.add_universe(CrunchDaoSkeleton, "CrunchDaoSkeleton", Resolution.DAILY, self.select_symbols)

        # Create a Scheduled Event to submit signals every monday before the market opens
        self._week = -1
        self.schedule.on(
            self.date_rules.every([DayOfWeek.MONDAY, DayOfWeek.TUESDAY, DayOfWeek.WEDNESDAY, DayOfWeek.THURSDAY, DayOfWeek.FRIDAY]),
            self.time_rules.at(13, 15, TimeZones.UTC),
            self.submit_signals)

        self.settings.minimum_order_margin_portfolio_percentage = 0

        self.set_warm_up(timedelta(45))

    def select_symbols(self, data: list[CrunchDaoSkeleton]) -> list[Symbol]:
        return [x.symbol for x in data]

    def on_securities_changed(self, changes: SecurityChanges) -> None:
        for security in changes.removed_securities:
            if security in self.crunch_universe:
                self.crunch_universe.remove(security)
        self.crunch_universe.extend(changes.added_securities)

    def submit_signals(self) -> None:
        if self.is_warming_up:
            return

        # Submit signals once per week
        week_num = self.time.isocalendar()[1]
        if self._week == week_num:
            return
        self._week = week_num

        symbols = [security.symbol for security in self.crunch_universe if security.price > 0]

        # Get historical price data
        # close_prices = self.history(symbols, 22, Resolution.DAILY).close.unstack(0)

        # Create portfolio targets
        weight_by_symbol = {symbol: 1/len(symbols) for symbol in symbols} # Add your logic here
        targets = [PortfolioTarget(symbol, weight) for symbol, weight in weight_by_symbol.items()]

        # (Optional) Place trades
        self.set_holdings(targets)

        # Send signals to CrunchDAO
        success = self.signal_export.set_target_portfolio(targets)
        if not success:
            self.debug(f"Couldn't send targets at {self.time}")


class CrunchDaoSkeleton(PythonData):

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        return SubscriptionDataSource("https://tournament.crunchdao.com/data/skeleton.csv", SubscriptionTransportMedium.REMOTE_FILE)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> DynamicData:
        if not line[0].isdigit():
            return None
        skeleton = CrunchDaoSkeleton()
        skeleton.symbol = config.symbol

        try:
            csv = line.split(',')
            skeleton.end_time = datetime.strptime(csv[0], "%Y-%m-%d")
            skeleton.symbol =  Symbol(SecurityIdentifier.generate_equity(csv[1], Market.USA, mapping_resolve_date=skeleton.time), csv[1])
            skeleton["Ticker"] = csv[1]

        except ValueError:
            # Do nothing
            return None

        return skeleton
