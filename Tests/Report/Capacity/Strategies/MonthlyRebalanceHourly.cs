/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Report.Capacity.Strategies
{
    public class MonthlyRebalanceHourly : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution.Hour).Symbol;
            AddEquity("GE", Resolution.Hour);
            AddEquity("FB", Resolution.Hour);
            AddEquity("DIS", Resolution.Hour);
            AddEquity("CSCO", Resolution.Hour);
            AddEquity("CRM", Resolution.Hour);
            AddEquity("C", Resolution.Hour);
            AddEquity("BAC", Resolution.Hour);
            AddEquity("BABA", Resolution.Hour);
            AddEquity("AAPL", Resolution.Hour);

            Schedule.On(DateRules.MonthStart(spy), TimeRules.Noon, () =>
            {
                foreach (var symbol in Securities.Keys)
                {
                    SetHoldings(symbol, 0.10);
                }
            });
        }
    }
}
