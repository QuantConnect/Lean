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

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.USTreasury;

namespace QuantConnect.Algorithm.CSharp
{
    public class USTreasuryYieldCurveRateAlgorithm : QCAlgorithm
    {
        private Symbol _yieldCurve;
        private Symbol _spy;
        private DateTime _lastInversion = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2000, 3, 1);
            SetEndDate(2019, 9, 15);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            _yieldCurve = AddData<USTreasuryYieldCurveRate>("USTYCR", Resolution.Daily).Symbol;

            // Request 60 days of history with the USTreasuryYieldCurveRate custom data Symbol.
            var history = History<USTreasuryYieldCurveRate>(_yieldCurve, 60, Resolution.Daily);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");
        }

        public override void OnData(Slice data)
        {
            if (!data.ContainsKey(_yieldCurve))
            {
                return;
            }

            // Preserve null values by getting the data with `slice.Get<T>`
            // Accessing the data using `data[_yieldCurve]` results in null
            // values becoming `default(decimal)` which is equal to 0
            var rates = data.Get<USTreasuryYieldCurveRate>().Values.First();

            // Check for null before using the values
            if (!rates.TenYear.HasValue || !rates.TwoYear.HasValue)
            {
                return;
            }

            // Only advance if a year has gone by
            if (Time - _lastInversion < TimeSpan.FromDays(365))
            {
                return;
            }

            // if there is a yield curve inversion after not having one for a year, short SPY for two years
            if (!Portfolio.Invested && rates.TwoYear > rates.TenYear)
            {
                Debug($"{Time} - Yield curve inversion! Shorting the market for two years");
                SetHoldings(_spy, -0.5);

                _lastInversion = Time;

                return;
            }

            // If two years have passed, liquidate our position in SPY
            if (Time - _lastInversion >= TimeSpan.FromDays(365 * 2))
            {
                Liquidate(_spy);
            }
        }
    }
}
