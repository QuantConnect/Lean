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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test for history and warm up using the data available in open source.
    /// </summary>
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="warm up" />
    public class IndicatorWarmupAlgorithm : QCAlgorithm
    {
        private const string SPY    = "SPY";
        private const string GOOG   = "GOOG";
        private const string IBM    = "IBM";
        private const string BAC    = "BAC";
        private const string GOOGL  = "GOOGL";

        private readonly Dictionary<Symbol, SymbolData> _sd = new Dictionary<Symbol, SymbolData>();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 11);

            SetCash(1000000);

            AddSecurity(SecurityType.Equity, SPY, Resolution.Minute);
            AddSecurity(SecurityType.Equity, IBM, Resolution.Minute);
            AddSecurity(SecurityType.Equity, BAC, Resolution.Minute);

            AddSecurity(SecurityType.Equity, GOOG, Resolution.Daily);
            AddSecurity(SecurityType.Equity, GOOGL, Resolution.Daily);

            foreach (var security in Securities)
            {
                _sd.Add(security.Key, new SymbolData(security.Key, this));
            }

            // we want to warm up our algorithm
            SetWarmup(SymbolData.RequiredBarsWarmup);
        }

        public override void OnData(Slice data)
        {
            // we are only using warmup for indicator spooling, so wait for us to be warm then continue
            if (IsWarmingUp) return;

            foreach (var sd in _sd.Values)
            {
                var lastPriceTime = sd.Close.Current.Time;
                // only make decisions when we have data on our requested resolution
                if (lastPriceTime.RoundDown(sd.Security.Resolution.ToTimeSpan()) == lastPriceTime)
                {
                    sd.Update();
                }
            }
        }

        public override void OnOrderEvent(OrderEvent fill)
        {
            SymbolData sd;
            if (_sd.TryGetValue(fill.Symbol, out sd))
            {
                sd.OnOrderEvent(fill);
            }
        }

        class SymbolData
        {
            public const int RequiredBarsWarmup = 40;
            public const decimal PercentTolerance = 0.001m;
            public const decimal PercentGlobalStopLoss = 0.01m;
            private const int LotSize = 10;

            public readonly Symbol Symbol;
            public readonly Security Security;

            public decimal Quantity
            {
                get { return Security.Holdings.Quantity; }
            }

            public readonly Identity Close;
            public readonly AverageDirectionalIndex ADX;
            public readonly ExponentialMovingAverage EMA;
            public readonly MovingAverageConvergenceDivergence MACD;

            private readonly QCAlgorithm _algorithm;

            private OrderTicket _currentStopLoss;

            public SymbolData(Symbol symbol, QCAlgorithm algorithm)
            {
                Symbol = symbol;
                Security = algorithm.Securities[symbol];

                Close = algorithm.Identity(symbol);
                ADX = algorithm.ADX(symbol, 14);
                EMA = algorithm.EMA(symbol, 14);
                MACD = algorithm.MACD(symbol, 12, 26, 9, MovingAverageType.Simple);

                // if we're receiving daily

                _algorithm = algorithm;
            }

            public bool IsReady
            {
                get { return Close.IsReady && ADX.IsReady & EMA.IsReady && MACD.IsReady; }
            }

            public bool IsUptrend
            {
                get
                {
                    const decimal tolerance = 1 + PercentTolerance;

                    return MACD.Signal > MACD*tolerance
                        && EMA > Close*tolerance;
                }
            }

            public bool IsDowntrend
            {
                get
                {
                    const decimal tolerance = 1 - PercentTolerance;

                    return MACD.Signal < MACD*tolerance
                        && EMA < Close*tolerance;
                }
            }

            public void OnOrderEvent(OrderEvent fill)
            {
                if (fill.Status != OrderStatus.Filled)
                {
                    return;
                }

                // if we just finished entering, place a stop loss as well
                if (Security.Invested)
                {
                    var stop = Security.Holdings.IsLong
                        ? fill.FillPrice*(1 - PercentGlobalStopLoss)
                        : fill.FillPrice*(1 + PercentGlobalStopLoss);

                    _currentStopLoss = _algorithm.StopMarketOrder(Symbol, -Quantity, stop, "StopLoss at: " + stop);
                }
                // check for an exit, cancel the stop loss
                else
                {
                    if (_currentStopLoss != null && _currentStopLoss.Status.IsOpen())
                    {
                        // cancel our current stop loss
                        _currentStopLoss.Cancel("Exited position");
                        _currentStopLoss = null;
                    }
                }
            }

            public void Update()
            {
                OrderTicket ticket;
                TryEnter(out ticket);
                TryExit(out ticket);
            }

            public bool TryEnter(out OrderTicket ticket)
            {
                ticket = null;
                if (Security.Invested)
                {
                    // can't enter if we're already in
                    return false;
                }

                int qty = 0;
                decimal limit = 0m;
                if (IsUptrend)
                {
                    // 100 order lots
                    qty = LotSize;
                    limit = Security.Low;
                }
                else if (IsDowntrend)
                {
                    limit = Security.High;
                    qty = -LotSize;
                }
                if (qty != 0)
                {
                    ticket = _algorithm.LimitOrder(Symbol, qty, limit, "TryEnter at: " + limit);
                }
                return qty != 0;
            }

            public bool TryExit(out OrderTicket ticket)
            {
                const decimal exitTolerance = 1 + 2 * PercentTolerance;

                ticket = null;
                if (!Security.Invested)
                {
                    // can't exit if we haven't entered
                    return false;
                }

                decimal limit = 0m;
                if (Security.Holdings.IsLong && Close*exitTolerance < EMA)
                {
                    limit = Security.High;
                }
                else if (Security.Holdings.IsShort && Close > EMA*exitTolerance)
                {
                    limit = Security.Low;
                }
                if (limit != 0)
                {
                    ticket = _algorithm.LimitOrder(Symbol, -Quantity, limit, "TryExit at: " + limit);
                }
                return -Quantity != 0;
            }
        }
    }
}