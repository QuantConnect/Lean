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
from datetime import datetime, timedelta
from System.Threading import AutoResetEvent
from threading import Lock

### <summary>
### This algorithm demonstrates unified Financial Advisor group orders and authoritative
### post-order account reconciliation. It requires
### ib-financial-advisors-unified-groups-enabled=true and an existing group whose saved
### method is Equal, NetLiq, AvailableEquity, Ratio, or Percent. ContractsOrShares instead
### requires updating and confirming the saved vector before submitting a parent with
### the exact saved total; use FinancialAdvisorGroupAssignmentAlgorithm for that mutation
### and confirmed-readback pattern.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="using quantconnect" />
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="financial advisor" />
class FinancialAdvisorDemoAlgorithm(QCAlgorithm):

    _GROUP_NAME = "TestGroupEQ"
    _RECONCILE_RETRY_INTERVAL = timedelta(seconds=5)
    _INVALID_ORDER_RETRY_INTERVAL = timedelta(seconds=5)
    _TOPOLOGY_REFRESH_INTERVAL = timedelta(seconds=90)
    _MAXIMUM_SNAPSHOT_AGE = timedelta(minutes=5)
    _COMPLETE_REFRESH_TOPOLOGY_TICKS = 3
    _MAXIMUM_INVALID_ORDER_ATTEMPTS = 3
    _MAXIMUM_EMPTY_ORDER_ATTEMPTS = 3

    def initialize(self):
        # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must be initialized.

        self.set_start_date(2013,10,7)   #Set Start Date
        self.set_end_date(2013,10,11)    #Set End Date
        self.set_cash(100000)           #Set Strategy Cash

        self._symbol = self.add_equity("SPY", Resolution.SECOND).symbol
        # Python.NET cannot expose a persistent ref-int to Interlocked, so this
        # provides the same atomic, coalescing set-and-consume semantics.
        self._scheduled_refresh_intent = AutoResetEvent(False)
        self._order_state_lock = Lock()
        self._terminal_events_during_submission = {}
        self._pre_order_snapshot = None
        self._initial_snapshot_refresh_accepted = False
        self._initial_snapshot_request_generation = -1
        self._next_initial_snapshot_refresh_utc = None
        self._group_order_submitted = False
        self._order_submission_in_progress = False
        self._on_data_state_active = False
        self._group_order_id = 0
        self._invalid_order_attempt_count = 0
        self._empty_order_attempt_count = 0
        self._next_group_order_retry_utc = None
        self._pending_reconcile_generation = -1
        self._pending_reconcile_terminal_utc = None
        self._pending_terminal_order_event = None
        self._next_reconcile_refresh_utc = None
        self._scheduled_refresh_topology_ticks = 0

        # Route every order to the existing group. Leaving fa_method blank uses the
        # saved method; this aggregate demo expects a computed, Ratio, or Percent
        # group rather than ContractsOrShares.
        self.default_order_properties = InteractiveBrokersOrderProperties()
        self.default_order_properties.fa_group = self._GROUP_NAME

        if self.live_mode:
            # The 90-second cadence is intentionally offset from minute data so a
            # refresh cannot consume every OnData opportunity. Every third topology
            # tick expands to complete account state.
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

        with self._order_state_lock:
            self._on_data_state_active = True
        try:
            self._on_live_data()
        finally:
            with self._order_state_lock:
                self._on_data_state_active = False

    def _on_live_data(self):
        snapshot = self.brokerage_account_snapshot
        now = self.utc_time
        with self._order_state_lock:
            invalid_order_message, request_reconcile_refresh = \
                self._try_apply_terminal_order_intent_locked(
                    snapshot.generation,
                    now)
        if invalid_order_message is not None or request_reconcile_refresh:
            self._report_terminal_order_action(
                invalid_order_message,
                request_reconcile_refresh,
                snapshot)
            return

        pre_order_snapshot = None
        reconciled_terminal_order_event = None
        reconcile = False
        request_reconcile_refresh = False
        with self._order_state_lock:
            if self._pending_reconcile_generation >= 0:
                if self._is_snapshot_fresh(snapshot) and \
                        snapshot.generation > \
                        self._pending_reconcile_generation and \
                        self._pending_reconcile_terminal_utc is not None and \
                        snapshot.collection_started_utc >= \
                        self._pending_reconcile_terminal_utc:
                    pre_order_snapshot = self._pre_order_snapshot
                    self._pre_order_snapshot = None
                    reconciled_terminal_order_event = \
                        self._pending_terminal_order_event
                    self._pending_terminal_order_event = None
                    self._pending_reconcile_generation = -1
                    self._pending_reconcile_terminal_utc = None
                    self._next_reconcile_refresh_utc = None
                    reconcile = True
                else:
                    request_reconcile_refresh = True
        if reconcile:
            self._reconcile_account_positions(
                pre_order_snapshot,
                snapshot)
            with self._order_state_lock:
                terminal_order_message = \
                    self._complete_terminal_order_reconciliation_locked(
                        reconciled_terminal_order_event,
                        now)
            if terminal_order_message is not None:
                self.error(terminal_order_message)
            return
        if request_reconcile_refresh:
            self._try_request_reconcile_refresh(snapshot)
            return

        with self._order_state_lock:
            request_initial_refresh = \
                not self._initial_snapshot_refresh_accepted or \
                not self._is_snapshot_fresh(snapshot) or \
                snapshot.generation <= \
                self._initial_snapshot_request_generation
        if request_initial_refresh:
            self._try_request_initial_snapshot_refresh(snapshot)
            return
        if self._try_process_scheduled_snapshot_refresh(
                allow_on_data_owner=True):
            return
        with self._order_state_lock:
            submission_snapshot = self.brokerage_account_snapshot
            if self._group_order_submitted or \
                    self._order_submission_in_progress or \
                    self._invalid_order_attempt_count >= \
                    self._MAXIMUM_INVALID_ORDER_ATTEMPTS or \
                    self._empty_order_attempt_count >= \
                    self._MAXIMUM_EMPTY_ORDER_ATTEMPTS or \
                    (self._next_group_order_retry_utc is not None and
                     self.utc_time < self._next_group_order_retry_utc) or \
                    not self._is_snapshot_fresh(submission_snapshot) or \
                    self._find_group(submission_snapshot) is None:
                return

            self._pre_order_snapshot = submission_snapshot
            self._order_submission_in_progress = True
            self._terminal_events_during_submission.clear()
        tickets = self.set_holdings(self._symbol, 1, asynchronous=True)
        ticket = next(iter(tickets), None)
        post_submission_snapshot = self.brokerage_account_snapshot
        post_submission_utc = self.utc_time
        with self._order_state_lock:
            invalid_order_message, request_reconcile_refresh = \
                self._complete_group_order_submission_locked(
                    ticket,
                    post_submission_snapshot.generation,
                    post_submission_utc)
        self._report_terminal_order_action(
            invalid_order_message,
            request_reconcile_refresh,
            post_submission_snapshot)

    def on_order_event(self, order_event):
        """Requests authoritative account reconciliation after the parent group order becomes terminal."""

        if not self.live_mode or \
                not order_event.status.is_closed():
            return

        invalid_order_message = None
        reconcile_refresh_rejected = False
        with self._order_state_lock:
            terminal_order_event = (
                order_event.status,
                order_event.utc_time,
                order_event.message)
            if self._order_submission_in_progress:
                self._terminal_events_during_submission[
                    order_event.order_id] = terminal_order_event
            elif order_event.order_id == self._group_order_id and \
                    self._pending_reconcile_generation < 0:
                snapshot = self.brokerage_account_snapshot
                request_reconcile_refresh = False
                invalid_order_message, request_reconcile_refresh = \
                    self._apply_terminal_order_locked(
                        order_event.order_id,
                        terminal_order_event,
                        snapshot.generation,
                        self.utc_time)
                if request_reconcile_refresh:
                    attempted, accepted = \
                        self._try_request_reconcile_refresh_locked(snapshot)
                    reconcile_refresh_rejected = attempted and not accepted
        if invalid_order_message is not None:
            self.error(invalid_order_message)
        if reconcile_refresh_rejected:
            self._report_reconcile_refresh_rejection()

    def _try_request_initial_snapshot_refresh(self, snapshot):
        with self._order_state_lock:
            attempted, accepted = \
                self._try_request_initial_snapshot_refresh_locked(snapshot)
        if attempted and not accepted:
            self.error(
                f"The initial snapshot refresh for Financial Advisor group "
                f"'{self._GROUP_NAME}' was not accepted; retrying.")

    def _try_request_initial_snapshot_refresh_locked(self, snapshot):
        now = self.utc_time
        if snapshot.status == \
                BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_initial_snapshot_refresh_utc is not None and
                 now < self._next_initial_snapshot_refresh_utc):
            return False, True

        self._next_initial_snapshot_refresh_utc = \
            now + self._RECONCILE_RETRY_INTERVAL
        # The IB implementation only coalesces into its in-memory QueueRefresh
        # while this sample state is protected.
        accepted = self.request_brokerage_account_snapshot_refresh(
            [self._GROUP_NAME])
        self._initial_snapshot_refresh_accepted = accepted
        if accepted:
            self._initial_snapshot_request_generation = snapshot.generation
        return True, accepted

    def _try_request_reconcile_refresh(self, snapshot):
        with self._order_state_lock:
            attempted, accepted = \
                self._try_request_reconcile_refresh_locked(snapshot)
        if attempted and not accepted:
            self._report_reconcile_refresh_rejection()

    def _try_request_reconcile_refresh_locked(self, snapshot):
        now = self.utc_time
        if self._pending_reconcile_generation < 0 or \
                snapshot.status == \
                BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_reconcile_refresh_utc is not None and
                 now < self._next_reconcile_refresh_utc):
            return False, True

        self._next_reconcile_refresh_utc = \
            now + self._RECONCILE_RETRY_INTERVAL
        # The IB implementation only coalesces into its in-memory QueueRefresh
        # while this sample state is protected.
        accepted = self.request_brokerage_account_snapshot_refresh(
            [self._GROUP_NAME])
        return True, accepted

    def _request_scheduled_snapshot_refresh(self):
        self._scheduled_refresh_intent.set()
        if not self._try_process_snapshot_request_priority():
            self._try_process_scheduled_snapshot_refresh()

    def _try_process_scheduled_snapshot_refresh(
            self,
            allow_on_data_owner=False):
        accepted = True
        with self._order_state_lock:
            if (self._on_data_state_active and
                    not allow_on_data_owner) or \
                    self._group_order_id != 0 or \
                    self._order_submission_in_progress or \
                    self._pending_reconcile_generation >= 0 or \
                    not self._initial_snapshot_refresh_accepted:
                return False
            snapshot = self.brokerage_account_snapshot
            if snapshot.generation <= \
                    self._initial_snapshot_request_generation or \
                    snapshot.status == \
                    BrokerageAccountSnapshotStatus.REFRESHING:
                return False
            if not self._scheduled_refresh_intent.wait_one(0):
                return False

            self._scheduled_refresh_topology_ticks += 1
            complete_refresh = \
                self._scheduled_refresh_topology_ticks == \
                self._COMPLETE_REFRESH_TOPOLOGY_TICKS
            if complete_refresh:
                self._scheduled_refresh_topology_ticks = 0
            # The IB implementation only coalesces into its in-memory
            # QueueRefresh while this sample state is protected.
            if complete_refresh:
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
        return True

    def _try_process_snapshot_request_priority(self):
        snapshot = None
        pre_order_snapshot = None
        reconciled_terminal_order_event = None
        reconcile = False
        initial_refresh_rejected = False
        reconcile_refresh_rejected = False
        with self._order_state_lock:
            if self._on_data_state_active:
                return True

            snapshot = self.brokerage_account_snapshot
            if self._pending_reconcile_generation >= 0:
                if self._is_snapshot_fresh(snapshot) and \
                        snapshot.generation > \
                        self._pending_reconcile_generation and \
                        self._pending_reconcile_terminal_utc is not None and \
                        snapshot.collection_started_utc >= \
                        self._pending_reconcile_terminal_utc:
                    pre_order_snapshot = self._pre_order_snapshot
                    self._pre_order_snapshot = None
                    reconciled_terminal_order_event = \
                        self._pending_terminal_order_event
                    self._pending_terminal_order_event = None
                    self._pending_reconcile_generation = -1
                    self._pending_reconcile_terminal_utc = None
                    self._next_reconcile_refresh_utc = None
                    reconcile = True
                else:
                    attempted, accepted = \
                        self._try_request_reconcile_refresh_locked(snapshot)
                    reconcile_refresh_rejected = attempted and not accepted
            elif not self._initial_snapshot_refresh_accepted or \
                    not self._is_snapshot_fresh(snapshot) or \
                    snapshot.generation <= \
                    self._initial_snapshot_request_generation:
                attempted, accepted = \
                    self._try_request_initial_snapshot_refresh_locked(snapshot)
                initial_refresh_rejected = attempted and not accepted
            else:
                return False

        if reconcile:
            self._reconcile_account_positions(
                pre_order_snapshot,
                snapshot)
            with self._order_state_lock:
                terminal_order_message = \
                    self._complete_terminal_order_reconciliation_locked(
                        reconciled_terminal_order_event,
                        self.utc_time)
            if terminal_order_message is not None:
                self.error(terminal_order_message)
        if initial_refresh_rejected:
            self.error(
                f"The initial snapshot refresh for Financial Advisor group "
                f"'{self._GROUP_NAME}' was not accepted; retrying.")
        if reconcile_refresh_rejected:
            self._report_reconcile_refresh_rejection()
        return True

    def _report_reconcile_refresh_rejection(self):
        self.error(
            f"The post-order snapshot refresh for Financial Advisor group "
            f"'{self._GROUP_NAME}' was not accepted.")

    def _reconcile_account_positions(
            self,
            pre_order_snapshot,
            current_snapshot):
        group = self._find_group(pre_order_snapshot)
        if group is None:
            self.error(
                f"The pre-order snapshot for Financial Advisor group "
                f"'{self._GROUP_NAME}' is unavailable.")
            return

        previous_accounts = {
            account.account_id.casefold(): account
            for account in list(pre_order_snapshot.accounts.values)
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

    def _complete_group_order_submission_locked(
            self,
            ticket,
            snapshot_generation,
            now):
        self._order_submission_in_progress = False
        if ticket is None:
            self._pre_order_snapshot = None
            self._group_order_submitted = False
            self._terminal_events_during_submission.clear()
            self._empty_order_attempt_count += 1
            retry_allowed = \
                self._empty_order_attempt_count < \
                self._MAXIMUM_EMPTY_ORDER_ATTEMPTS
            self._next_group_order_retry_utc = \
                now + self._INVALID_ORDER_RETRY_INTERVAL \
                if retry_allowed else datetime.max
            if retry_allowed:
                return None, False
            return (
                f"SetHoldings returned no Financial Advisor parent order "
                f"ticket on {self._MAXIMUM_EMPTY_ORDER_ATTEMPTS} attempts; "
                f"the bounded retry limit was reached.",
                False)

        self._empty_order_attempt_count = 0
        self._group_order_id = ticket.order_id
        self._group_order_submitted = True
        ticket_status = ticket.status
        terminal_order_event = \
            self._terminal_events_during_submission.get(ticket.order_id)
        self._terminal_events_during_submission.clear()
        if terminal_order_event is None and ticket_status.is_closed():
            response = ticket.submit_request.response
            terminal_order_event = (
                ticket_status,
                now,
                response.error_message)
        if terminal_order_event is None:
            self._invalid_order_attempt_count = 0
            return None, False

        return self._apply_terminal_order_locked(
            ticket.order_id,
            terminal_order_event,
            snapshot_generation,
            now)

    def _try_apply_terminal_order_intent_locked(
            self,
            snapshot_generation,
            now):
        if self._order_submission_in_progress or \
                self._group_order_id == 0 or \
                self._pending_reconcile_generation >= 0:
            return None, False

        terminal_order_event = \
            self._terminal_events_during_submission.pop(
                self._group_order_id,
                None)
        if terminal_order_event is None:
            return None, False
        return self._apply_terminal_order_locked(
            self._group_order_id,
            terminal_order_event,
            snapshot_generation,
            now)

    def _apply_terminal_order_locked(
            self,
            order_id,
            terminal_order_event,
            snapshot_generation,
            now):
        status, terminal_utc, message = terminal_order_event
        self._group_order_id = 0
        invalid_order_message = None
        if status == OrderStatus.INVALID:
            reason = message \
                if message is not None and str(message).strip() \
                else "No rejection reason was supplied."
            invalid_order_message = (
                f"Financial Advisor group order {order_id} was rejected: "
                f"{reason} A newer account snapshot will be reconciled "
                f"before retry eligibility is decided.")
        # Publish the causal timestamps before making the generation pending.
        self._pending_reconcile_terminal_utc = terminal_utc
        self._pending_terminal_order_event = (
            order_id,
            terminal_order_event)
        self._next_reconcile_refresh_utc = now
        self._pending_reconcile_generation = snapshot_generation
        return invalid_order_message, True

    def _complete_terminal_order_reconciliation_locked(
            self,
            terminal_order_event,
            now):
        if terminal_order_event is None or \
                terminal_order_event[1][0] != OrderStatus.INVALID:
            self._invalid_order_attempt_count = 0
            return None

        self._invalid_order_attempt_count += 1
        self._group_order_submitted = False
        retry_allowed = \
            self._invalid_order_attempt_count < \
            self._MAXIMUM_INVALID_ORDER_ATTEMPTS
        self._next_group_order_retry_utc = \
            now + self._INVALID_ORDER_RETRY_INTERVAL \
            if retry_allowed else datetime.max
        order_id = terminal_order_event[0]
        return (
            f"Financial Advisor group order {order_id} was reconciled after "
            f"rejection. Attempt {self._invalid_order_attempt_count} of "
            f"{self._MAXIMUM_INVALID_ORDER_ATTEMPTS} was rejected; the next "
            f"attempt is scheduled."
            if retry_allowed
            else
            f"Financial Advisor group order {order_id} was reconciled after "
            f"rejection. The bounded retry limit was reached.")

    def _report_terminal_order_action(
            self,
            invalid_order_message,
            request_reconcile_refresh,
            snapshot):
        if invalid_order_message is not None:
            self.error(invalid_order_message)
        if request_reconcile_refresh:
            self._try_request_reconcile_refresh(snapshot)

    def _find_group(self, snapshot):
        if snapshot is None:
            return None

        return next((
            group for group in list(snapshot.groups.values)
            if group.name.casefold() == self._GROUP_NAME.casefold()
        ), None)

    def _is_snapshot_fresh(self, snapshot):
        if snapshot is None or not snapshot.is_ready:
            return False

        authority_utc = snapshot.collection_started_utc
        if authority_utc == datetime.min:
            authority_utc = snapshot.last_successful_update_utc
        return authority_utc >= self.utc_time - self._MAXIMUM_SNAPSHOT_AGE

    def _get_position_quantity(self, account):
        return sum(
            position.quantity
            for position in account.positions
            if position.symbol == self._symbol
        )
