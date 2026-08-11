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
### Regression algorithm asserting the scheduling convenience APIs: an every-N-days date rule,
### a time rule with an IANA time zone id and scheduling with a default midnight time rule
### </summary>
class SchedulingConvenienceRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self._every_three_days_times = []
        self._default_time_rule_times = []
        self._london_times = []

        # every 3 days, anchored at the start of the schedule
        self.schedule.on(self.date_rules.every(timedelta(days=3)), self.time_rules.at(12, 0),
            lambda: self._every_three_days_times.append(self.time))

        # no time rule: defaults to midnight in the algorithm time zone
        self.schedule.on(self.date_rules.every_day(), lambda: self._default_time_rule_times.append(self.time))

        # IANA time zone id: 15:00 London (BST in October 2013) is 10:00 New York (EDT)
        self.schedule.on(self.date_rules.every_day(), self.time_rules.at(15, 0, "Europe/London"),
            lambda: self._london_times.append(self.time))

        # date/time rule properties are tolerant to being called as if they were methods, returning the rule itself
        for rule, expected_name in [(self.time_rules.midnight(), "Midnight"), (self.time_rules.noon(), "Noon"),
                                    (self.time_rules.now(), "Now"), (self.date_rules.today(), "TodayOnly"),
                                    (self.date_rules.tomorrow(), "TomorrowOnly")]:
            if rule.name != expected_name:
                raise RegressionTestException(f"Unexpected rule name: expected {expected_name} but got {rule.name}")

    def on_end_of_algorithm(self):
        # the schedule starts the day before the algorithm start, so the rule anchors at Oct 6 and
        # fires Oct 6 (before the start, skipped), Oct 9 and Oct 12 (after the end)
        self._assert_scheduled_times(self._every_three_days_times, [datetime(2013, 10, 9, 12, 0, 0)], "every 3 days")

        # fires at midnight every day, including the algorithm start time itself
        self._assert_scheduled_times(self._default_time_rule_times, [datetime(2013, 10, day) for day in range(7, 12)],
            "default midnight")

        self._assert_scheduled_times(self._london_times, [datetime(2013, 10, day, 10, 0, 0) for day in range(7, 12)],
            "London 15:00")

    def _assert_scheduled_times(self, actual, expected, name):
        if actual != expected:
            raise RegressionTestException(f"Unexpected '{name}' event times: expected {expected} but got {actual}")
