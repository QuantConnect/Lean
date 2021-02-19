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
    public class MonthlyRebalanceDaily : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2019, 12, 31);
            SetEndDate(2020, 4, 5);
            SetCash(100000);

            var spy = AddEquity("SPY", Resolution.Daily).Symbol;
            AddEquity("GE", Resolution.Daily);
            AddEquity("FB", Resolution.Daily);
            AddEquity("DIS", Resolution.Daily);
            AddEquity("CSCO", Resolution.Daily);
            AddEquity("CRM", Resolution.Daily);
            AddEquity("C", Resolution.Daily);
            AddEquity("BAC", Resolution.Daily);
            AddEquity("BABA", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);

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
