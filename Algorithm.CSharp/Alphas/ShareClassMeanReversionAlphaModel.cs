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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class ShareClassMeanReversionAlphaModel : QCAlgorithmFrameworkBridge
    {
        private string[] _tickers = new string[] { "GOOG", "GOOGL" };
        private List<Symbol> _symbols = new List<Symbol>();
        private TradeBar position_bar;
        private SimpleMovingAverage _sma = new SimpleMovingAverage(20);
        private RollingWindow<decimal> _position = new RollingWindow<decimal>(2);
        private decimal? _alpha;
        private decimal? _beta;
        private decimal _positionValue;
        private bool _invested;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash
            
            foreach (string t in _tickers)
            {
                var s = AddSecurity(SecurityType.Equity, t, Resolution.Minute);
                s.FeeModel = new ConstantFeeModel(0m);
                _symbols.Add(s.Symbol);
            }

            SetWarmUp(20);
        }

        public override void OnData(Slice data)
        {
            if (!data.Bars.ContainsKey(_symbols[0]) || !data.Bars.ContainsKey(_symbols[1]))
            {
                return;
            }

            if (!_alpha.HasValue && !_beta.HasValue)
            {
                _alpha = CalculateOrderQuantity(_symbols[0], 0.5m);
                _beta = CalculateOrderQuantity(_symbols[1], 0.5m);
            }

            if (!_sma.IsReady)
            {
                _positionValue = (_alpha * data[_symbols[0]].Close) - (_beta * data[_symbols[1]].Close);
                _sma.Update(data[_symbols[0]].EndTime, _positionValue);
                _position.Add(_positionValue);
                return;
            }

            _positionValue = (_alpha * data[_symbols[0]].Close) - (_beta * data[_symbols[1]].Close);
            _sma.Update(data[_symbols[0]].EndTime, _positionValue);
            _position.Add(_positionValue);

            if (!_invested)
            {
                if (_positionValue >= _sma.Current.Value)
                {
                    var insight1 = Insight.Price(_symbols[1], TimeSpan.FromMinutes(5), InsightDirection.Up);
                    var insight2 = Insight.Price(_symbols[0], TimeSpan.FromMinutes(5), InsightDirection.Down);
                    Insight.Group(insight1, insight2);
                    EmitInsights(insight1, insight2);

                    SetHoldings(_symbols[1], 0.5m);
                    SetHoldings(_symbols[0], -0.5m);
                    _invested = true;
                }
                else if (_positionValue < _sma.Current.Value)
                {
                    var insight1 = Insight.Price(_symbols[1], TimeSpan.FromMinutes(5), InsightDirection.Down);
                    var insight2 = Insight.Price(_symbols[0], TimeSpan.FromMinutes(5), InsightDirection.Up);
                    Insight.Group(insight1, insight2);
                    EmitInsights(insight1, insight2);

                    SetHoldings(_symbols[1], -0.5m);
                    SetHoldings(_symbols[0], 0.5m);
                    _invested = true;
                }
            }

            if (_invested && CrossedSma(_position, _sma))
            {
                Liquidate();
                _invested = false;
            }
        }

        private bool CrossedSma(RollingWindow<decimal> position, SimpleMovingAverage sma)
        {
            if ((position[0] >= sma.Current.Value) && (position[1] < sma.Current.Value))
            {
                return true;
            }
            else if ((position[0] < sma.Current.Value) && (position[1] >= sma.Current.Value))
            {
                return true;
            }

            return false;
        }
    }
}