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
from datetime import timedelta

### <summary>
### This algorithm demonstrates unified Financial Advisor group orders and authoritative
### post-order account reconciliation. The group and its saved allocation method must
### already exist in TWS, and ib-financial-advisors-unified-groups-enabled must be true.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="financial advisor" />
class FinancialAdvisorDemoAlgorithm(QCAlgorithm):

    _GROUP_NAME = "TestGroupEQ"
    _RECONCILE_RETRY_INTERVAL = timedelta(seconds=5)
    _TOPOLOGY_REFRESH_INTERVAL = timedelta(minutes=1)
    _COMPLETE_REFRESH_TOPOLOGY_TICKS = 3

    def initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must be initialized.

        self.set_start_date(2013,10,7)   #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(100000)           #Set Strategy Cash

        self._symbol = self.add_equity("SPY", Resolution.SECOND).symbol
        self._pre_order_snapshot = None
        self._initial_snapshot_refresh_accepted = False
        self._initial_snapshot_request_generation = -1
        self._next_initial_snapshot_refresh_utc = None
        self._group_order_submitted = False
        self._group_order_id = 0
        self._pending_reconcile_generation = -1
        self._pending_reconcile_terminal_utc = None
        self._next_reconcile_refresh_utc = None
        self._scheduled_refresh_topology_ticks = 0

        # Route every order to the existing group. Leaving fa_method blank makes
        # TWS's saved group allocation method authoritative.
        self.default_order_properties = InteractiveBrokersOrderProperties()
        self.default_order_properties.fa_group = self._GROUP_NAME

        if self.live_mode:
            # Every third one-minute topology tick expands to complete account state.
            self.schedule.on(
                self.date_rules.every_day(),
                self.time_rules.every(self._TOPOLOGY_REFRESH_INTERVAL),
                self._request_scheduled_snapshot_refresh)
            self._try_request_initial_snapshot_refresh(
                self.brokerage_account_snapshot)

    def on_data(self, data):
        # on_data event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        if not self.live_mode:
            if not self.portfolio.invested:
                # when logged into IB as a Financial Advisor, this call will use order properties
                # set in the DefaultOrderProperties property of QCAlgorithm
                self.set_holdings("SPY", 1)
            return

        snapshot = self.brokerage_account_snapshot
        if self._pending_reconcile_generation >= 0:
            if snapshot.is_ready and \
                    snapshot.generation > self._pending_reconcile_generation and \
                    self._pending_reconcile_terminal_utc is not None and \
                    snapshot.collection_started_utc >= self._pending_reconcile_terminal_utc:
                self._reconcile_account_positions(snapshot)
                self._pre_order_snapshot = None
                self._pending_reconcile_generation = -1
                self._pending_reconcile_terminal_utc = None
                self._next_reconcile_refresh_utc = None
            else:
                self._try_request_reconcile_refresh(snapshot)
            return

        if not self._initial_snapshot_refresh_accepted or \
                not snapshot.is_ready or \
                snapshot.generation <= self._initial_snapshot_request_generation:
            self._try_request_initial_snapshot_refresh(snapshot)
            return

        if self._group_order_submitted or \
                self._find_group(snapshot) is None:
            return

        self._pre_order_snapshot = snapshot
        tickets = self.set_holdings(self._symbol, 1, asynchronous=True)
        ticket = next(iter(tickets), None)
        if ticket is None:
            self._pre_order_snapshot = None
            return

        self._group_order_id = ticket.order_id
        self._group_order_submitted = True

    def on_order_event(self, order_event):
        """Requests authoritative account reconciliation after the parent group order becomes terminal."""

        if not self.live_mode or \
                order_event.order_id != self._group_order_id or \
                not order_event.status.is_closed() or \
                self._pending_reconcile_generation >= 0:
            return

        self._pending_reconcile_generation = \
            self.brokerage_account_snapshot.generation
        self._pending_reconcile_terminal_utc = order_event.utc_time
        self._next_reconcile_refresh_utc = self.utc_time
        self._group_order_id = 0
        self._try_request_reconcile_refresh(self.brokerage_account_snapshot)

    def _try_request_initial_snapshot_refresh(self, snapshot):
        if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_initial_snapshot_refresh_utc is not None and
                 self.utc_time < self._next_initial_snapshot_refresh_utc):
            return

        self._next_initial_snapshot_refresh_utc = \
            self.utc_time + self._RECONCILE_RETRY_INTERVAL
        request_generation = snapshot.generation
        self._initial_snapshot_refresh_accepted = \
            self.request_brokerage_account_snapshot_refresh([self._GROUP_NAME])
        if self._initial_snapshot_refresh_accepted:
            self._initial_snapshot_request_generation = request_generation
        else:
            self.error(
                f"The initial snapshot refresh for Financial Advisor group "
                f"'{self._GROUP_NAME}' was not accepted; retrying.")

    def _try_request_reconcile_refresh(self, snapshot):
        if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_reconcile_refresh_utc is not None and
                 self.utc_time < self._next_reconcile_refresh_utc):
            return

        self._next_reconcile_refresh_utc = \
            self.utc_time + self._RECONCILE_RETRY_INTERVAL
        if not self.request_brokerage_account_snapshot_refresh([self._GROUP_NAME]):
            self.error(
                f"The post-order snapshot refresh for Financial Advisor group "
                f"'{self._GROUP_NAME}' was not accepted.")

    def _request_scheduled_snapshot_refresh(self):
        snapshot = self.brokerage_account_snapshot
        if not self.live_mode or \
                self._group_order_id != 0 or \
                self._pending_reconcile_generation >= 0 or \
                snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING:
            return
        if not self._initial_snapshot_refresh_accepted:
            self._try_request_initial_snapshot_refresh(snapshot)
            return

        self._scheduled_refresh_topology_ticks += 1
        complete_refresh = \
            self._scheduled_refresh_topology_ticks == \
            self._COMPLETE_REFRESH_TOPOLOGY_TICKS
        if complete_refresh:
            self._scheduled_refresh_topology_ticks = 0
            accepted = self.request_brokerage_account_snapshot_refresh()
        else:
            accepted = self.request_brokerage_account_snapshot_refresh(
                [self._GROUP_NAME])

        if not accepted:
            purpose = "complete account-state" \
                if complete_refresh \
                else f"topology for group '{self._GROUP_NAME}'"
            self.error(
                f"The scheduled Financial Advisor {purpose} snapshot "
                f"refresh was not accepted.")

    def _reconcile_account_positions(self, current_snapshot):
        group = self._find_group(self._pre_order_snapshot)
        if group is None:
            self.error(
                f"The pre-order snapshot for Financial Advisor group "
                f"'{self._GROUP_NAME}' is unavailable.")
            return

        previous_accounts = {
            account.account_id.casefold(): account
            for account in list(self._pre_order_snapshot.accounts.values)
        }
        current_accounts = {
            account.account_id.casefold(): account
            for account in list(current_snapshot.accounts.values)
        }
        for account_id in group.account_ids:
            account_key = account_id.casefold()
            previous_account = previous_accounts.get(account_key)
            current_account = current_accounts.get(account_key)
            if previous_account is None or current_account is None:
                self.error(
                    f"Account state for '{account_id}' is unavailable during "
                    f"Financial Advisor group reconciliation.")
                continue

            previous_quantity = self._get_position_quantity(previous_account)
            current_quantity = self._get_position_quantity(current_account)
            self.log(
                f"FA reconciliation: account={account_id}, symbol={self._symbol}, "
                f"before={previous_quantity}, after={current_quantity}, "
                f"change={current_quantity - previous_quantity}")

    def _find_group(self, snapshot):
        if snapshot is None:
            return None

        return next((
            group for group in list(snapshot.groups.values)
            if group.name.casefold() == self._GROUP_NAME.casefold()
        ), None)

    def _get_position_quantity(self, account):
        return sum(
            position.quantity
            for position in account.positions
            if position.symbol == self._symbol
        )
