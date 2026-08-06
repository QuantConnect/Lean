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
from decimal import Decimal, InvalidOperation
from System.Threading import AutoResetEvent
from threading import Lock
import re


### <summary>
### Demonstrates alias-driven Financial Advisor group assignment, confirmed mutation
### results, and confirming snapshots after material cash or net-liquidation changes.
###
### LIVE PREREQUISITES:
### - ib-financial-advisors-group-filter must be empty. A configured filter rejects
###   movement to any other destination group.
### - ib-financial-advisors-group-management-enabled=true is required and implies
###   ib-financial-advisors-unified-groups-enabled=true.
### - fa-alias-pattern is a case-insensitive regular expression.
### - Setting fa-target-group to an empty string requests removal of a matched account from every group,
###   but this sample refuses to remove the final member of a source group.
### - ContractsOrShares child values may be fractional, but their saved total must
###   be lot-aligned; for a lot size of one, 12.5 + 7.5 = 20 is valid.
### - Do not request configuration mutations during on_end_of_algorithm or teardown:
###   a request may be accepted but is not guaranteed to reach the broker or publish a result.
### </summary>
### <meta name="tag" content="financial advisor" />
### <meta name="tag" content="brokerage account groups" />
### <meta name="tag" content="live trading" />
class FinancialAdvisorGroupAssignmentAlgorithm(QCAlgorithm):

    _SYSTEM_DECIMAL_MAX_COEFFICIENT = \
        79228162514264337593543950335
    _REFRESH_RETRY_INTERVAL = timedelta(seconds=5)
    _TOPOLOGY_REFRESH_INTERVAL = timedelta(seconds=90)
    _MAXIMUM_SNAPSHOT_AGE = timedelta(minutes=5)
    _COMPLETE_REFRESH_TOPOLOGY_TICKS = 3
    _COMPUTED_ALLOCATION_METHODS = {
        "netliq",
        "availableequity",
        "equal"
    }

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)
        self.add_equity("SPY", Resolution.MINUTE)

        alias_pattern = self.get_parameter("fa-alias-pattern", "^MOVE-")
        try:
            self._alias_pattern = re.compile(alias_pattern, re.IGNORECASE)
        except re.error as error:
            raise ValueError(
                f"Invalid fa-alias-pattern '{alias_pattern}': {error}") from error

        self._target_group_name = self.get_parameter(
            "fa-target-group",
            "TargetGroup")
        self._allocation_value = self._get_decimal_parameter(
            "fa-allocation-value",
            "1")
        self._cash_change_threshold = self._get_decimal_parameter(
            "fa-cash-change-threshold",
            "1000")
        if self._cash_change_threshold < 0:
            raise ValueError(
                "fa-cash-change-threshold cannot be negative.")

        self._snapshot_request_generation = None
        self._snapshot_request_is_confirmation = False
        self._initial_snapshot_refresh_accepted = False
        self._initial_snapshot_request_generation = -1
        self._next_snapshot_refresh_utc = None
        self._scheduled_refresh_topology_ticks = 0
        # The provider queues the asynchronous refresh without socket I/O, allowing request
        # acceptance and sample state to be updated in one critical section.
        self._order_state_lock = Lock()
        # Python.NET cannot expose a persistent ref-int to Interlocked, so this
        # provides the same atomic, coalescing set-and-consume semantics.
        self._scheduled_refresh_intent = AutoResetEvent(False)

        self._last_ready_snapshot = None
        self._last_observed_ready_generation = -1
        self._cash_confirmation_trigger_generation = None

        self._active_assignment_generation = None
        self._assignment_generation_before_request = None
        self._state_machine_active = False
        self._active_assignment_account_id = None
        self._active_assignment_snapshot_generation = -1
        self._minimum_ready_generation = None
        self._last_membership_evaluation_generation = -1
        self._last_final_source_member_block_key = None
        self._last_unsupported_allocation_method_key = None

        if self.live_mode:
            # Scoped group refreshes run every 90 seconds; every third tick expands to a
            # complete-discovery request. The algorithm owns this freshness policy.
            self.schedule.on(
                self.date_rules.every_day(),
                self.time_rules.every(self._TOPOLOGY_REFRESH_INTERVAL),
                self._request_scheduled_snapshot_refresh)
            self._try_request_snapshot_refresh(
                self.brokerage_account_snapshot,
                is_confirmation=False,
                is_initial_request=True)

    def on_data(self, data):
        if not self.live_mode:
            return

        self._try_run_live_state_machine()

    def _try_run_live_state_machine(self):
        with self._order_state_lock:
            if self._state_machine_active:
                return False
            self._state_machine_active = True
        try:
            self._on_live_data()
            return True
        finally:
            with self._order_state_lock:
                self._state_machine_active = False

    def _on_live_data(self):
        snapshot = self.brokerage_account_snapshot
        if not self._update_snapshot_request_state(snapshot):
            return

        self._poll_assignment()
        if self._assignment_generation_before_request is not None:
            return

        if self._minimum_ready_generation is not None:
            if self._is_snapshot_fresh(snapshot) and \
                    snapshot.generation > self._minimum_ready_generation:
                self._minimum_ready_generation = None
            else:
                self._try_request_snapshot_refresh(
                    snapshot,
                    is_confirmation=False)
                return

        self._observe_ready_snapshot(snapshot)
        if self._cash_confirmation_trigger_generation is not None:
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=True)
            return

        if not self._initial_snapshot_refresh_accepted or \
                snapshot.generation <= \
                self._initial_snapshot_request_generation:
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False,
                is_initial_request=True)
            return

        if self._snapshot_request_generation is not None:
            return

        if not self._is_snapshot_fresh(snapshot):
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False)
            return

        if self._has_scheduled_refresh_intent():
            self._try_process_scheduled_snapshot_refresh()
            return

        if self._try_assign_next_account(snapshot):
            return

        self._try_process_scheduled_snapshot_refresh()

    def _observe_ready_snapshot(self, snapshot):
        if not self._is_snapshot_fresh(snapshot) or \
                not snapshot.is_complete or \
                snapshot.generation <= self._last_observed_ready_generation:
            return

        is_cash_confirmation = \
            self._cash_confirmation_trigger_generation is not None and \
            snapshot.generation > \
            self._cash_confirmation_trigger_generation
        if is_cash_confirmation:
            self.log(
                f"FA cash-change confirmation completed at snapshot "
                f"generation {snapshot.generation}.")
            self._cash_confirmation_trigger_generation = None
        elif self._last_ready_snapshot is not None:
            changed_account = self._find_material_financial_change(
                self._last_ready_snapshot,
                snapshot)
            if changed_account is not None:
                self._cash_confirmation_trigger_generation = \
                    snapshot.generation
                self._next_snapshot_refresh_utc = self.utc_time
                self.log(
                    f"FA cash or net-liquidation change exceeded "
                    f"{self._cash_change_threshold} for account "
                    f"'{changed_account}'. Requesting a confirming snapshot "
                    f"before re-evaluating group membership.")

        self._last_ready_snapshot = snapshot
        self._last_observed_ready_generation = snapshot.generation

    def _find_material_financial_change(self, previous, current):
        previous_accounts = {
            account.account_id.casefold(): account
            for account in list(previous.accounts.values)
        }
        current_accounts = {
            account.account_id.casefold(): account
            for account in list(current.accounts.values)
        }
        matched_accounts = {
            account.account_id.casefold()
            for account in list(current.account_directory.values)
            if self._is_alias_match(account)
        }

        for account_key in (
                previous_accounts.keys() &
                current_accounts.keys() &
                matched_accounts):
            previous_account = previous_accounts[account_key]
            current_account = current_accounts[account_key]
            cash_change = self._absolute_change(
                previous_account.total_cash_value,
                current_account.total_cash_value)
            net_liquidation_change = self._absolute_change(
                previous_account.net_liquidation,
                current_account.net_liquidation)
            if (cash_change is not None and
                    cash_change > self._cash_change_threshold) or \
                    (net_liquidation_change is not None and
                     net_liquidation_change >
                     self._cash_change_threshold):
                return current_account.account_id

        return None

    @staticmethod
    def _absolute_change(previous, current):
        if previous is None or current is None:
            return None
        return abs(Decimal(str(current)) - Decimal(str(previous)))

    def _update_snapshot_request_state(self, snapshot):
        with self._order_state_lock:
            can_continue, error_message = \
                self._update_snapshot_request_state_locked(snapshot)
        if error_message is not None:
            self.error(error_message)
        return can_continue

    def _update_snapshot_request_state_locked(self, snapshot):
        error_message = None
        can_continue = True
        if self._snapshot_request_generation is not None:
            if snapshot.status == \
                    BrokerageAccountSnapshotStatus.REFRESHING:
                can_continue = False
            elif snapshot.is_ready and \
                    snapshot.generation > \
                    self._snapshot_request_generation:
                self._snapshot_request_generation = None
                self._snapshot_request_is_confirmation = False
            elif snapshot.status in (
                    BrokerageAccountSnapshotStatus.FAILED,
                    BrokerageAccountSnapshotStatus.STALE,
                    BrokerageAccountSnapshotStatus.UNAVAILABLE):
                purpose = "cash confirmation" \
                    if self._snapshot_request_is_confirmation \
                    else "discovery"
                error_message = (
                    f"The FA {purpose} snapshot refresh completed as "
                    f"{snapshot.status}; retrying.")
                self._snapshot_request_generation = None
                self._snapshot_request_is_confirmation = False
            else:
                can_continue = False
        return can_continue, error_message

    def _try_request_snapshot_refresh(
            self,
            snapshot,
            is_confirmation,
            group_names=None,
            is_initial_request=False):
        error_message = None
        with self._order_state_lock:
            error_message = self._try_request_snapshot_refresh_locked(
                snapshot,
                is_confirmation,
                group_names,
                is_initial_request)
        if error_message is not None:
            self.error(error_message)

    def _try_request_snapshot_refresh_locked(
            self,
            snapshot,
            is_confirmation,
            group_names,
            is_initial_request=False):
        if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_snapshot_refresh_utc is not None and
                 self.utc_time < self._next_snapshot_refresh_utc):
            return None

        self._next_snapshot_refresh_utc = \
            self.utc_time + self._REFRESH_RETRY_INTERVAL
        request_generation = snapshot.generation
        scheduled_refresh_intent = \
            self._scheduled_refresh_intent.wait_one(0)
        accepted = self.request_brokerage_account_snapshot_refresh() \
            if group_names is None \
            else self.request_brokerage_account_snapshot_refresh(group_names)
        if is_initial_request:
            self._initial_snapshot_refresh_accepted = accepted
            if accepted:
                self._initial_snapshot_request_generation = request_generation
        if accepted:
            self._snapshot_request_generation = request_generation
            self._snapshot_request_is_confirmation = is_confirmation
            return None

        if scheduled_refresh_intent:
            self._scheduled_refresh_intent.set()
        purpose = "cash confirmation" \
            if is_confirmation \
            else "discovery"
        return f"The FA {purpose} snapshot refresh was not accepted; retrying."

    def _request_scheduled_snapshot_refresh(self):
        self._try_run_live_state_machine()
        with self._order_state_lock:
            refresh_request_outstanding = \
                self._snapshot_request_generation is not None
        if not refresh_request_outstanding:
            self._scheduled_refresh_intent.set()
            self._try_run_live_state_machine()

    def _has_scheduled_refresh_intent(self):
        if not self._scheduled_refresh_intent.wait_one(0):
            return False

        self._scheduled_refresh_intent.set()
        return True

    def _try_process_scheduled_snapshot_refresh(self):
        error_messages = []
        with self._order_state_lock:
            snapshot = self.brokerage_account_snapshot
            if self._snapshot_request_generation is not None or \
                    self._assignment_generation_before_request is not None or \
                    self._minimum_ready_generation is not None or \
                    self._cash_confirmation_trigger_generation is not None or \
                    not self._is_snapshot_fresh(snapshot) or \
                    snapshot.status == \
                    BrokerageAccountSnapshotStatus.REFRESHING or \
                    (self._next_snapshot_refresh_utc is not None and
                     self.utc_time < self._next_snapshot_refresh_utc):
                return False

            group_names = self._get_scheduled_group_names(snapshot)
            if not self._scheduled_refresh_intent.wait_one(0):
                return False

            self._scheduled_refresh_topology_ticks += 1
            complete_refresh = \
                self._scheduled_refresh_topology_ticks == \
                self._COMPLETE_REFRESH_TOPOLOGY_TICKS
            if complete_refresh:
                self._scheduled_refresh_topology_ticks = 0
                group_names = None
            elif not group_names:
                return True
            request_error = self._try_request_snapshot_refresh_locked(
                snapshot,
                is_confirmation=False,
                group_names=group_names)
            if request_error is not None:
                self._scheduled_refresh_intent.set()
                error_messages.append(request_error)
        for error_message in error_messages:
            self.error(error_message)
        return True

    def _poll_assignment(self):
        if self._assignment_generation_before_request is None:
            return

        assignment = self.brokerage_account_group_assignment
        if self._active_assignment_generation is None:
            if assignment.generation <= \
                    self._assignment_generation_before_request or \
                    assignment.account_id.casefold() != \
                    self._active_assignment_account_id.casefold() or \
                    assignment.status == \
                    BrokerageAccountGroupAssignmentStatus.UNAVAILABLE:
                return
            self._active_assignment_generation = assignment.generation

        if assignment.generation != self._active_assignment_generation or \
                assignment.account_id.casefold() != \
                self._active_assignment_account_id.casefold() or \
                not assignment.is_completed:
            return

        resulting_groups = ", ".join(
            list(assignment.resulting_group_names))
        if assignment.status == \
                BrokerageAccountGroupAssignmentStatus.SUCCEEDED:
            self.log(
                f"FA assignment succeeded: account="
                f"{assignment.account_id}, groups=[{resulting_groups}]")
            self._minimum_ready_generation = \
                self._active_assignment_snapshot_generation
        else:
            self.error(
                f"FA assignment failed: account={assignment.account_id}, "
                f"error={assignment.error_message}, "
                f"groups=[{resulting_groups}]")
            self._minimum_ready_generation = \
                self._active_assignment_snapshot_generation

        self._active_assignment_generation = None
        self._assignment_generation_before_request = None
        self._active_assignment_account_id = None
        self._active_assignment_snapshot_generation = -1

    def _try_assign_next_account(self, snapshot):
        if snapshot.generation <= \
                self._last_membership_evaluation_generation:
            return False

        target_group = self._find_target_group(snapshot)
        if self._target_group_name and target_group is None:
            self.error(
                f"FA destination group '{self._target_group_name}' was not "
                f"present in Ready snapshot generation "
                f"{snapshot.generation}.")
            self._last_membership_evaluation_generation = \
                snapshot.generation
            return True

        candidates = list(self._find_assignment_candidates(
            snapshot,
            target_group))
        if not candidates:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            return False

        canonical_target_group_name = "" \
            if target_group is None \
            else target_group.name
        selected_group_keys = {
            group.name.casefold()
            for group in list(snapshot.groups.values)
        }
        account = None
        required_group_names = None
        first_blocked_account = None
        first_final_source_group = None
        for candidate in candidates:
            candidate_group_names = self._get_assignment_group_names(
                target_group,
                candidate)
            final_source_group = self._find_final_source_group(
                snapshot,
                target_group,
                candidate)
            if final_source_group is not None:
                if first_blocked_account is None:
                    first_blocked_account = candidate
                    first_final_source_group = final_source_group
                continue

            if any(
                    group_name.casefold() not in selected_group_keys
                    for group_name in candidate_group_names):
                self._try_request_snapshot_refresh(
                    snapshot,
                    is_confirmation=False,
                    group_names=candidate_group_names)
                return True

            account = candidate
            required_group_names = candidate_group_names
            break

        if account is None:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            block_key = snapshot.group_configuration_version
            if block_key != self._last_final_source_member_block_key:
                self._last_final_source_member_block_key = block_key
                self.error(
                    f"FA assignment for account "
                    f"'{first_blocked_account.account_id}' cannot remove the "
                    f"final member of source group "
                    f"'{first_final_source_group.name}'. Waiting for a "
                    f"scheduled topology refresh before reevaluating.")
            return True
        self._last_final_source_member_block_key = None

        requires_allocation_value = target_group is not None and \
            account.account_id.casefold() not in {
                    account_id.casefold()
                    for account_id in list(target_group.account_ids)
                }
        allocation_value = None
        if target_group is not None:
            allocation_value = self._get_target_allocation_value(
                target_group,
                snapshot.group_configuration_version,
                requires_allocation_value)
            if allocation_value is False:
                self._last_membership_evaluation_generation = \
                    snapshot.generation
                return True

        assignment_generation_before_request = \
            self.brokerage_account_group_assignment.generation
        accepted = False
        try:
            accepted = self.request_brokerage_account_group_assignment(
                account.account_id,
                canonical_target_group_name,
                allocation_value,
                snapshot)
            if accepted:
                with self._order_state_lock:
                    self._assignment_generation_before_request = \
                        assignment_generation_before_request
                    self._active_assignment_account_id = account.account_id
                    self._active_assignment_snapshot_generation = \
                        snapshot.generation
        except Exception as error:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            self._minimum_ready_generation = snapshot.generation
            self.error(
                f"FA assignment for account '{account.account_id}' was "
                f"rejected synchronously for target "
                f"'{canonical_target_group_name}' against snapshot "
                f"generation {snapshot.generation}: {error}. Retrying after "
                f"a newer snapshot.")
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False,
                group_names=required_group_names)
            return True
        if not accepted:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            self._minimum_ready_generation = snapshot.generation
            self.error(
                f"FA assignment for account '{account.account_id}' was not "
                f"accepted against snapshot generation "
                f"{snapshot.generation}; retrying after a newer snapshot.")
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False,
                group_names=required_group_names)
            return True

        self._last_membership_evaluation_generation = snapshot.generation
        self.log(
            f"FA assignment accepted: account={account.account_id}, "
            f"target='{canonical_target_group_name}', "
            f"snapshotGeneration={snapshot.generation}, "
            f"assignmentGeneration>{assignment_generation_before_request}")
        return True

    def _find_target_group(self, snapshot):
        if not self._target_group_name:
            return None

        target_key = self._target_group_name.casefold()
        return next((
            group for group in list(snapshot.all_groups.values)
            if group.name.casefold() == target_key
        ), None)

    def _find_assignment_candidates(self, snapshot, target_group):
        target_key = None \
            if target_group is None \
            else target_group.name.casefold()
        candidates = sorted(
            list(snapshot.account_directory.values),
            key=lambda entry: entry.account_id.casefold())
        for account in candidates:
            if not self._is_alias_match(account):
                continue

            current_groups = {
                group_name.casefold()
                for group_name in account.group_names
            }
            if target_key is None:
                if current_groups:
                    yield account
            elif current_groups != {target_key}:
                yield account

    def _find_assignment_candidate(self, snapshot, target_group):
        return next(
            self._find_assignment_candidates(snapshot, target_group),
            None)

    @staticmethod
    def _find_final_source_group(snapshot, target_group, account):
        target_key = None \
            if target_group is None \
            else target_group.name.casefold()
        source_group_keys = {
            group_name.casefold()
            for group_name in account.group_names
            if target_key is None or group_name.casefold() != target_key
        }
        account_key = account.account_id.casefold()
        return next((
            group for group in list(snapshot.all_groups.values)
            if group.name.casefold() in source_group_keys and
            len(list(group.account_ids)) == 1 and
            list(group.account_ids)[0].casefold() == account_key
        ), None)

    def _is_alias_match(self, account):
        return account.relationship == \
            BrokerageAccountRelationship.MANAGED and \
            bool(account.account_alias) and \
            self._alias_pattern.search(account.account_alias) is not None

    def _get_target_allocation_value(
            self,
            target_group,
            group_configuration_version,
            required):
        allocation_method = target_group.allocation_method.casefold()
        if allocation_method in self._COMPUTED_ALLOCATION_METHODS:
            # IB calculates NetLiq, AvailableEquity, and Equal allocations, so the
            # assignment request does not provide a child allocation value.
            return None
        if allocation_method == "contractsorshares":
            if not required:
                return None
            if self._allocation_value < 0:
                self.error(
                    f"FA destination group '{target_group.name}' uses "
                    f"ContractsOrShares, so fa-allocation-value cannot be "
                    f"negative.")
                return False
            return self._allocation_value
        if allocation_method == "ratio":
            if not required:
                return None
            if self._allocation_value <= 0:
                self.error(
                    f"FA destination group '{target_group.name}' uses "
                    f"{target_group.allocation_method}, so "
                    f"fa-allocation-value must be positive.")
                return False
            return self._allocation_value
        if allocation_method == "percent":
            if not required:
                return None
            if self._allocation_value <= 0 or \
                    self._allocation_value > 100 or \
                    (self._allocation_value == 100 and
                     len(list(target_group.account_ids)) != 0):
                self.error(
                    f"FA destination group '{target_group.name}' uses "
                    f"Percent, so fa-allocation-value must be greater than zero "
                    f"and less than 100 when the group already has members.")
                return False
            return self._allocation_value

        unsupported_method_key = (
            target_group.name.casefold(),
            allocation_method,
            group_configuration_version)
        if unsupported_method_key != \
                self._last_unsupported_allocation_method_key:
            self._last_unsupported_allocation_method_key = \
                unsupported_method_key
            if allocation_method == "pctchange":
                self.error(
                    f"FA destination group '{target_group.name}' uses saved "
                    f"PctChange. IB paper TWS accepts that configuration, but "
                    f"this LEAN sample does not mutate PctChange groups.")
            else:
                self.error(
                    f"FA destination group '{target_group.name}' uses "
                    f"unsupported saved allocation method "
                    f"'{target_group.allocation_method}'.")
        return False

    def _get_scheduled_group_names(self, snapshot):
        if not self._target_group_name:
            group_names = [
                group.name
                for group in list(snapshot.all_groups.values)
            ]
            return group_names

        target_group = self._find_target_group(snapshot)
        target_name = self._target_group_name \
            if target_group is None else target_group.name
        candidate = self._find_assignment_candidate(
            snapshot,
            target_group)
        return self._get_assignment_group_names(
            target_group,
            candidate,
            target_name)

    @staticmethod
    def _get_assignment_group_names(
            target_group,
            account,
            target_name=None):
        group_names = {}
        if target_group is not None:
            group_names[target_group.name.casefold()] = target_group.name
        elif target_name:
            group_names[target_name.casefold()] = target_name
        if account is not None:
            for group_name in account.group_names:
                group_names.setdefault(
                    group_name.casefold(),
                    group_name)
        return list(group_names.values())

    def _is_snapshot_fresh(self, snapshot):
        if snapshot is None or not snapshot.is_ready:
            return False

        authority_utc = snapshot.collection_started_utc
        if authority_utc == datetime.min:
            authority_utc = snapshot.last_successful_update_utc
        return authority_utc >= self.utc_time - self._MAXIMUM_SNAPSHOT_AGE

    def _get_decimal_parameter(self, name, default_value):
        raw_value = self.get_parameter(name, default_value)
        try:
            value = Decimal(str(raw_value))
        except (InvalidOperation, ValueError) as error:
            raise ValueError(
                f"Algorithm parameter '{name}' must be representable as "
                "System.Decimal.") from error

        if not self._is_system_decimal(value):
            raise ValueError(
                f"Algorithm parameter '{name}' must be representable as "
                "System.Decimal.")
        return value

    @classmethod
    def _is_system_decimal(cls, value):
        if not value.is_finite() or \
                value.copy_abs() > cls._SYSTEM_DECIMAL_MAX_COEFFICIENT:
            return False

        _, digits, exponent = value.as_tuple()
        if not any(digits):
            return True

        digits = list(digits)
        while exponent < 0 and digits[-1] == 0:
            digits.pop()
            exponent += 1

        if exponent < -28:
            return False

        coefficient = int("".join(str(digit) for digit in digits))
        if exponent > 0:
            coefficient *= 10 ** exponent
        return coefficient <= cls._SYSTEM_DECIMAL_MAX_COEFFICIENT
