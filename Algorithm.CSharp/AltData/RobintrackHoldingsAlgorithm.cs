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
    /// <summary>
    /// Looks at users holding the stock AAPL at a given point in time
    /// and keeps track of changes in retail investor sentiment.
    ///
    /// We go long if the sentiment increases by 0.5%, and short if it decreases by -0.5%
    /// </summary>
    public class RobintrackHoldingsAlgorithm : QCAlgorithm
    {
        private Symbol _aapl;
        private Symbol _aaplHoldings;
        private decimal _lastValue;
        private bool _isLong;

        public override void Initialize()
        {
            SetStartDate(2018, 5, 1);
            SetEndDate(2020, 5, 5);
            SetCash(100000);

            _aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            _aaplHoldings = AddData<RobintrackHoldings>(_aapl).Symbol;
            _isLong = false;
        }

        public override void OnData(Slice data)
        {
            foreach (var kvp in data.Get<RobintrackHoldings>())
            {
                var holdings = kvp.Value;

                if (_lastValue != 0)
                {
                    var percentChange = (holdings.UsersHolding - _lastValue) / _lastValue;
                    var holdingInfo = $"There are {holdings.UsersHolding} unique users holding {kvp.Key.Underlying} - users holding % of U.S. equities universe: {holdings.UniverseHoldingPercent * 100m}%";

                    if (percentChange >= 0.005m && !_isLong)
                    {
                        Log($"{UtcTime} - Buying AAPL - {holdingInfo}");
                        SetHoldings(_aapl, 0.5);
                        _isLong = true;
                    }
                    else if (percentChange <= -0.005m && _isLong)
                    {
                        Log($"{UtcTime} - Shorting AAPL - {holdingInfo}");
                        SetHoldings(_aapl, -0.5);
                        _isLong = false;
                    }
                }

                _lastValue = holdings.UsersHolding;
            }
        }
    }
}
