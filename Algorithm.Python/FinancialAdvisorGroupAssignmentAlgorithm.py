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
from decimal import Decimal, InvalidOperation
import re


### <summary>
### Demonstrates alias-driven Financial Advisor group assignment, confirmed mutation
### results, and cash-change-driven snapshot reconciliation.
###
### LIVE PREREQUISITES:
### - ib-financial-advisors-group-filter must be empty. A configured filter rejects
###   movement to any other destination group.
### - ib-financial-advisors-group-management-enabled=true is required and implies
###   ib-financial-advisors-unified-groups-enabled=true.
### - fa-alias-pattern is a case-insensitive regular expression.
### - An exactly empty fa-target-group removes matched accounts from every group.
### </summary>
### <meta name="tag" content="financial advisor" />
### <meta name="tag" content="brokerage account groups" />
### <meta name="tag" content="live trading" />
class FinancialAdvisorGroupAssignmentAlgorithm(QCAlgorithm):

    _REFRESH_RETRY_INTERVAL = timedelta(seconds=5)
    _TOPOLOGY_REFRESH_INTERVAL = timedelta(minutes=1)
    _COMPLETE_REFRESH_TOPOLOGY_TICKS = 3
    _COMPUTED_ALLOCATION_METHODS = {
        "netliq",
        "availableequity",
        "equal"
    }
    _USER_SPECIFIED_ALLOCATION_METHODS = {
        "contractsorshares",
        "ratio",
        "percent"
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
        try:
            self._allocation_value = Decimal(str(self.get_parameter(
                "fa-allocation-value",
                "1")))
            self._cash_change_threshold = Decimal(str(self.get_parameter(
                "fa-cash-change-threshold",
                "1000")))
        except InvalidOperation as error:
            raise ValueError(
                "fa-allocation-value and fa-cash-change-threshold must be "
                "decimal numbers.") from error
        if self._allocation_value <= 0:
            raise ValueError("fa-allocation-value must be positive.")
        if self._cash_change_threshold < 0:
            raise ValueError(
                "fa-cash-change-threshold cannot be negative.")

        self._snapshot_request_generation = None
        self._snapshot_request_is_confirmation = False
        self._next_snapshot_refresh_utc = None
        self._scheduled_refresh_topology_ticks = 0

        self._last_ready_snapshot = None
        self._last_observed_ready_generation = -1
        self._cash_confirmation_trigger_generation = None

        self._active_assignment_generation = None
        self._active_assignment_account_id = None
        self._active_assignment_snapshot_generation = -1
        self._minimum_ready_generation = None
        self._last_membership_evaluation_generation = -1
        self._next_assignment_retry_utc = None

        # Refreshes are permitted during Initialize; mutations deliberately wait
        # for OnData, after LEAN has locked the initialized algorithm.
        if self.live_mode:
            # Every third one-minute topology tick expands to complete account state.
            self.schedule.on(
                self.date_rules.every_day(),
                self.time_rules.every(self._TOPOLOGY_REFRESH_INTERVAL),
                self._request_scheduled_snapshot_refresh)
            self._try_request_snapshot_refresh(
                self.brokerage_account_snapshot,
                is_confirmation=False)

    def on_data(self, data):
        if not self.live_mode:
            return

        snapshot = self.brokerage_account_snapshot
        self._poll_assignment()
        if self._active_assignment_generation is not None:
            return

        self._observe_ready_snapshot(snapshot)
        self._drive_snapshot_refresh(snapshot)

        if self._minimum_ready_generation is not None:
            if snapshot.is_ready and \
                    snapshot.generation > self._minimum_ready_generation:
                self._minimum_ready_generation = None
            else:
                self._try_request_snapshot_refresh(
                    snapshot,
                    is_confirmation=False)
                return

        if self._active_assignment_generation is not None or \
                self._snapshot_request_generation is not None or \
                self._cash_confirmation_trigger_generation is not None or \
                not snapshot.is_ready:
            return

        self._try_assign_next_account(snapshot)

    def _observe_ready_snapshot(self, snapshot):
        if not snapshot.is_ready or \
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

    def _drive_snapshot_refresh(self, snapshot):
        if self._snapshot_request_generation is not None:
            if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING:
                return
            if snapshot.is_ready and \
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
                self.error(
                    f"The FA {purpose} snapshot refresh completed as "
                    f"{snapshot.status}; retrying.")
                self._snapshot_request_generation = None
                self._snapshot_request_is_confirmation = False
            else:
                return

        if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING:
            return

        if self._cash_confirmation_trigger_generation is not None:
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=True)
            return

        if not snapshot.is_ready:
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False)

    def _try_request_snapshot_refresh(
            self,
            snapshot,
            is_confirmation,
            group_names=None):
        if snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_snapshot_refresh_utc is not None and
                 self.utc_time < self._next_snapshot_refresh_utc):
            return

        self._next_snapshot_refresh_utc = \
            self.utc_time + self._REFRESH_RETRY_INTERVAL
        request_generation = snapshot.generation
        accepted = self.request_brokerage_account_snapshot_refresh() \
            if group_names is None \
            else self.request_brokerage_account_snapshot_refresh(group_names)
        if accepted:
            self._snapshot_request_generation = request_generation
            self._snapshot_request_is_confirmation = is_confirmation
            return

        purpose = "cash confirmation" \
            if is_confirmation \
            else "discovery"
        self.error(
            f"The FA {purpose} snapshot refresh was not accepted; retrying.")

    def _request_scheduled_snapshot_refresh(self):
        snapshot = self.brokerage_account_snapshot
        self._drive_snapshot_refresh(snapshot)
        if not self.live_mode or \
                self._snapshot_request_generation is not None or \
                self._active_assignment_generation is not None or \
                self._minimum_ready_generation is not None or \
                self._cash_confirmation_trigger_generation is not None or \
                snapshot.status == BrokerageAccountSnapshotStatus.REFRESHING or \
                (self._next_snapshot_refresh_utc is not None and
                 self.utc_time < self._next_snapshot_refresh_utc):
            return

        self._scheduled_refresh_topology_ticks += 1
        if self._scheduled_refresh_topology_ticks == \
                self._COMPLETE_REFRESH_TOPOLOGY_TICKS:
            self._scheduled_refresh_topology_ticks = 0
            self._try_request_snapshot_refresh(
                snapshot,
                is_confirmation=False)
            return

        if self._target_group_name:
            target_group = self._find_target_group(snapshot)
            group_names = [
                self._target_group_name
                if target_group is None
                else target_group.name
            ]
        else:
            group_names = [
                group.name
                for group in list(snapshot.all_groups.values)
            ]
            if not group_names:
                self._try_request_snapshot_refresh(
                    snapshot,
                    is_confirmation=False)
                return

        self._try_request_snapshot_refresh(
            snapshot,
            is_confirmation=False,
            group_names=group_names)

    def _poll_assignment(self):
        if self._active_assignment_generation is None:
            return

        assignment = self.brokerage_account_group_assignment
        if assignment.generation != self._active_assignment_generation or \
                assignment.account_id.casefold() != \
                self._active_assignment_account_id.casefold() or \
                assignment.status == \
                BrokerageAccountGroupAssignmentStatus.UNAVAILABLE or \
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

        self._active_assignment_generation = None
        self._active_assignment_account_id = None
        self._active_assignment_snapshot_generation = -1

    def _try_assign_next_account(self, snapshot):
        if snapshot.generation <= \
                self._last_membership_evaluation_generation or \
                (self._next_assignment_retry_utc is not None and
                 self.utc_time < self._next_assignment_retry_utc):
            return

        target_group = self._find_target_group(snapshot)
        if self._target_group_name and target_group is None:
            self.error(
                f"FA destination group '{self._target_group_name}' was not "
                f"present in Ready snapshot generation "
                f"{snapshot.generation}.")
            self._last_membership_evaluation_generation = \
                snapshot.generation
            return

        account = self._find_assignment_candidate(
            snapshot,
            target_group)
        if account is None:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            return

        canonical_target_group_name = "" \
            if target_group is None \
            else target_group.name
        allocation_value = self._get_target_allocation_value(
            target_group)
        if allocation_value is False:
            self._last_membership_evaluation_generation = \
                snapshot.generation
            return

        accepted = self.request_brokerage_account_group_assignment(
            account.account_id,
            canonical_target_group_name,
            allocation_value,
            snapshot)
        if not accepted:
            self._next_assignment_retry_utc = \
                self.utc_time + self._REFRESH_RETRY_INTERVAL
            self.error(
                f"FA assignment for account '{account.account_id}' was not "
                f"accepted; retrying against snapshot generation "
                f"{snapshot.generation}.")
            return

        # Capture only after acceptance. The static Unavailable result is terminal,
        # so generation correlation is required before IsCompleted is meaningful.
        assignment = self.brokerage_account_group_assignment
        self._active_assignment_generation = assignment.generation
        self._active_assignment_account_id = account.account_id
        self._active_assignment_snapshot_generation = snapshot.generation
        self._last_membership_evaluation_generation = snapshot.generation
        self._next_assignment_retry_utc = None
        self.log(
            f"FA assignment accepted: account={account.account_id}, "
            f"target='{canonical_target_group_name}', "
            f"snapshotGeneration={snapshot.generation}, "
            f"assignmentGeneration={assignment.generation}")

    def _find_target_group(self, snapshot):
        if not self._target_group_name:
            return None

        target_key = self._target_group_name.casefold()
        return next((
            group for group in list(snapshot.all_groups.values)
            if group.name.casefold() == target_key
        ), None)

    def _find_assignment_candidate(self, snapshot, target_group):
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
                    return account
            elif target_key not in current_groups:
                return account

        return None

    def _is_alias_match(self, account):
        return account.relationship == \
            BrokerageAccountRelationship.MANAGED and \
            bool(account.account_alias) and \
            self._alias_pattern.search(account.account_alias) is not None

    def _get_target_allocation_value(self, target_group):
        if target_group is None:
            return None

        allocation_method = target_group.allocation_method.casefold()
        if allocation_method in self._COMPUTED_ALLOCATION_METHODS:
            # NetLiq, AvailableEquity and Equal are calculated by TWS.
            return None
        if allocation_method in \
                self._USER_SPECIFIED_ALLOCATION_METHODS:
            # ContractsOrShares, Ratio and Percent require an explicit
            # positive value for the account being added.
            return self._allocation_value

        self.error(
            f"FA destination group '{target_group.name}' uses unsupported "
            f"allocation method '{target_group.allocation_method}'.")
        return False
