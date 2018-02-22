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
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm is a test case to count expected data points with forex symbols at daily resolution.
    /// </summary>
    public class ForexDailyDataPointsRegressionAlgorithm : QCAlgorithm
    {
        private int _count;

        public override void Initialize()
        {
            SetStartDate(2013, 12, 10);
            SetEndDate(2013, 12, 12);
            SetCash(100000);

            AddForex("EURUSD", Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("EURUSD", 1m);
            }

            foreach (var kvp in data)
            {
                _count++;

                var symbol = kvp.Key;
                var barTime = kvp.Value.Time;
                var barEndTime = kvp.Value.EndTime;
                var timeZone = Securities[symbol].Exchange.TimeZone;

                Log($"AlgoTime: {Time}, Symbol: {symbol.Value}, BarTime: {barTime}, BarTime(UTC): {barTime.ConvertTo(timeZone, TimeZones.Utc)}, BarEndTime: {barEndTime}, BarEndTime(UTC): {barEndTime.ConvertTo(timeZone, TimeZones.Utc)}, {kvp.Value.Price}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // one point per day
            const int expectedDataPoints = 3;
            Log($"Data points: {_count}");

            if (_count != expectedDataPoints)
            {
                throw new Exception($"Data point count mismatch: expected: {expectedDataPoints}, actual: {_count}");
            }
        }
    }
}