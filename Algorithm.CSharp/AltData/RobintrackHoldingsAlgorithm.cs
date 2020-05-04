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

using QuantConnect.Data;
using QuantConnect.Data.Custom.Robintrack;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    public class RobintrackHoldingsAlgorithm : QCAlgorithm
    {
        Symbol aapl;
        Symbol aaplHoldings;
        decimal lastValue;
        bool isLong;

        public override void Initialize()
        {
            SetStartDate(2018, 5, 1);
            SetEndDate(2020, 5, 5);
            SetCash(100000);

            aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            aaplHoldings = AddData<RobintrackHoldings>(aapl).Symbol;
            isLong = false;
        }

        public override void OnData(Slice data)
        {
            foreach (var kvp in data.Get<RobintrackHoldings>())
            {
                var holdings = kvp.Value;

                if (lastValue != 0)
                {
                    var percentChange = (holdings.UsersHolding - lastValue) / lastValue;
                    var holdingInfo = $"There are {holdings.UsersHolding} unique users holding {kvp.Key.Underlying} - users holding % of U.S. equities universe: {holdings.UniverseHoldingPercent * 100m}%";

                    if (percentChange >= 0.005m && !isLong)
                    {

                        Log($"{UtcTime} - Buying AAPL - {holdingInfo}");
                        SetHoldings(aapl, 0.5);
                        isLong = true;
                    }
                    else if (percentChange <= -0.005m && isLong)
                    {
                        Log($"{UtcTime} - Shorting AAPL - {holdingInfo}");
                        SetHoldings(aapl, -0.5);
                        isLong = false;
                    }
                }

                lastValue = holdings.UsersHolding;
            }
        }
    }
}
