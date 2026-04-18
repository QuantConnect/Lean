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
### Regression algorithm demonstrating how to get order events in custom execution models
### and asserting that they match the algorithm's order events.
### </summary>
class ExecutionModelOrderEventsRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self._order_events = []
        self.universe_settings.resolution = Resolution.MINUTE

        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self.set_universe_selection(ManualUniverseSelectionModel(Symbol.create("SPY", SecurityType.EQUITY, Market.USA)))
        self.set_alpha(ConstantAlphaModel(InsightType.PRICE, InsightDirection.UP, timedelta(minutes=20), 0.025, None))
        self.set_portfolio_construction(EqualWeightingPortfolioConstructionModel(Resolution.DAILY))

        self._execution_model = CustomImmediateExecutionModel()
        self.set_execution(self._execution_model)
        self.set_risk_management(MaximumDrawdownPercentPerSecurity(0.01))

    def on_order_event(self, order_event):
        self._order_events.append(order_event)

    def on_end_of_algorithm(self):
        if len(self._execution_model.order_events) != len(self._order_events):
            raise Exception(f"Order events count mismatch. Execution model: {len(self._execution_model.order_events)}, Algorithm: {len(self._order_events)}")

        for i, (model_event, algo_event) in enumerate(zip(self._execution_model.order_events, self._order_events)):
            if (model_event.id != algo_event.id or
                model_event.order_id != algo_event.order_id or
                model_event.status != algo_event.status):
                raise Exception(f"Order event mismatch at index {i}. Execution model: {model_event}, Algorithm: {algo_event}")

class CustomImmediateExecutionModel(ExecutionModel):
    def __init__(self):
        self._targets_collection = PortfolioTargetCollection()
        self._order_tickets = {}
        self.order_events = []

    def execute(self, algorithm, targets):
        self._targets_collection.add_range(targets)
        if not self._targets_collection.is_empty:
            for target in self._targets_collection.order_by_margin_impact(algorithm):
                security = algorithm.securities[target.symbol]

                # calculate remaining quantity to be ordered
                quantity = OrderSizing.get_unordered_quantity(algorithm, target, security, True)

                if (quantity != 0 and
                    BuyingPowerModelExtensions.above_minimum_order_margin_portfolio_percentage(security.buying_power_model,
                        security, quantity, algorithm.portfolio, algorithm.settings.minimum_order_margin_portfolio_percentage)):
                    ticket = algorithm.market_order(security.symbol, quantity, asynchronous=True, tag=target.tag)
                    self._order_tickets[ticket.order_id] = ticket

            self._targets_collection.clear_fulfilled(algorithm)

    def on_order_event(self, algorithm, order_event):
        algorithm.log(f"{algorithm.time} - Order event received: {order_event}")

        # This method will get events for all orders, but if we save the tickets in Execute we can filter
        # to process events for orders placed by this model
        if order_event.order_id in self._order_tickets:
            ticket = self._order_tickets[order_event.order_id]
            if order_event.status.is_fill():
                algorithm.debug(f"Purchased Stock: {order_event.symbol}")
            if order_event.status.is_closed():
                del self._order_tickets[order_event.order_id]

        self.order_events.append(order_event)
