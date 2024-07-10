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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example of custom volatility model
    /// </summary>
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="reality modelling" />
    public class CustomVolatilityModelAlgorithm : QCAlgorithm
    {
        private Security _equity;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);   //Set Start Date
            SetEndDate(2015, 7, 15);     //Set End Date
            SetCash(100000);           //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            _equity = AddEquity("SPY", Resolution.Daily);
            _equity.SetVolatilityModel(new CustomVolatilityModel(10));
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !(_equity.VolatilityModel.Volatility > 0))
                SetHoldings("SPY", 1);
        }
    }

    public class CustomVolatilityModel : IVolatilityModel
    {
        private DateTime _lastUpdate = DateTime.MinValue;
        private decimal _lastPrice = 0m;
        private bool _needsUpdate = false;
        private TimeSpan _periodSpan = TimeSpan.FromDays(1);
        private RollingWindow<decimal> _window;

        // Volatility is a mandatory field
        public decimal Volatility { get; set; } = 0m;
        public CustomVolatilityModel(int periods)
        {
            _window = new RollingWindow<decimal>(periods);
        }

        // Updates this model using the new price information in the specified security instance
        // Update is a mandatory method
        public void Update(Security security, BaseData data)
        {
            var timeSinceLastUpdate = data.EndTime - _lastUpdate;
            if (timeSinceLastUpdate >= _periodSpan && data.Price > 0m)
            {
                if (_lastPrice > 0)
                {
                    _window.Add(data.Price / _lastPrice - 1.0m);
                    _needsUpdate = _window.IsReady;
                }

                _lastUpdate = data.EndTime;
                _lastPrice = data.Price;
            }

            if (_window.Count < 2)
            {
                Volatility = 0;
                return;
            }

            if (_needsUpdate)
            {
                _needsUpdate = false;
                var mean = _window.Average();
                var std = Math.Sqrt((double)_window.Sum(x => (x - mean)*(x - mean)) / _window.Count);
                Volatility = (std * Math.Sqrt(252d)).SafeDecimalCast();
            }
        }

        // Returns history requirements for the volatility model expressed in the form of history request
        // GetHistoryRequirements is a mandatory method
        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        // For simplicity's sake, we will not set a history requirement
        {
            return Enumerable.Empty<HistoryRequest>();
        }
    }
}
