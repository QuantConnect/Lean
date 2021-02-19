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
    public class BeastVsPenny : QCAlgorithm
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(10000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            var penny = AddEquity("ABUS", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(penny, 0.5m);
            });
        }
    }
}
