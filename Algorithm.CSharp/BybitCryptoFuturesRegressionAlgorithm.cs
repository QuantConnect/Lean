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
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating and ensuring that Bybit crypto futures brokerage model works as expected
    /// </summary>
    public class BybitCryptoFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CryptoFuture _btcUsdt;
        private CryptoFuture _btcUsd;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        private Dictionary<Symbol, int> _interestPerSymbol = new();

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);

            // Set strategy cash (USD)
            SetCash(100000);

            SetBrokerageModel(BrokerageName.Bybit, AccountType.Margin);

            AddCrypto("BTCUSDT", Resolution.Minute);

            _btcUsdt = AddCryptoFuture("BTCUSDT", Resolution.Minute);
            _btcUsd = AddCryptoFuture("BTCUSD", Resolution.Minute);

            // create two moving averages
            _fast = EMA(_btcUsdt.Symbol, 30, Resolution.Minute);
            _slow = EMA(_btcUsdt.Symbol, 60, Resolution.Minute);

            _interestPerSymbol[_btcUsdt.Symbol] = 0;
            _interestPerSymbol[_btcUsd.Symbol] = 0;

            // the amount of USDT we need to hold to trade 'BTCUSDT'
            _btcUsdt.QuoteCurrency.SetAmount(200);
            // the amount of BTC we need to hold to trade 'BTCUSD'
            _btcUsd.BaseCurrency.SetAmount(0.005m);
        }

        public override void OnData(Slice data)
        {
            var interestRates = data.Get<MarginInterestRate>();
            foreach (var interestRate in interestRates)
            {
                _interestPerSymbol[interestRate.Key]++;

                var cachedInterestRate = Securities[interestRate.Key].Cache.GetData<MarginInterestRate>();
                if (cachedInterestRate != interestRate.Value)
                {
                    throw new Exception($"Unexpected cached margin interest rate for {interestRate.Key}!");
                }
            }

            if (!_slow.IsReady)
            {
                return;
            }

            if (_fast > _slow)
            {
                if (!Portfolio.Invested && Transactions.OrdersCount == 0)
                {
                    var ticket = Buy(_btcUsd.Symbol, 1000);
                    if (ticket.Status != OrderStatus.Invalid)
                    {
                        throw new Exception($"Unexpected valid order {ticket}, should fail due to margin not sufficient");
                    }

                    Buy(_btcUsd.Symbol, 100);

                    var marginUsed = Portfolio.TotalMarginUsed;
                    var btcUsdHoldings = _btcUsd.Holdings;

                    // Coin futures value is 100 USD
                    var holdingsValueBtcUsd = 100;
                    if (Math.Abs(btcUsdHoldings.TotalSaleVolume - holdingsValueBtcUsd) > 1)
                    {
                        throw new Exception($"Unexpected TotalSaleVolume {btcUsdHoldings.TotalSaleVolume}");
                    }
                    if (Math.Abs(btcUsdHoldings.AbsoluteHoldingsCost - holdingsValueBtcUsd) > 1)
                    {
                        throw new Exception($"Unexpected holdings cost {btcUsdHoldings.HoldingsCost}");
                    }
                    // margin used is based on the maintenance rate
                    if (Math.Abs(btcUsdHoldings.AbsoluteHoldingsCost * 0.05m - marginUsed) > 1
                        || _btcUsd.BuyingPowerModel.GetMaintenanceMargin(_btcUsd) != marginUsed)
                    {
                        throw new Exception($"Unexpected margin used {marginUsed}");
                    }

                    Buy(_btcUsdt.Symbol, 0.01);

                    marginUsed = Portfolio.TotalMarginUsed - marginUsed;
                    var btcUsdtHoldings = _btcUsdt.Holdings;

                    // USDT futures value is based on it's price
                    var holdingsValueUsdt = _btcUsdt.Price * _btcUsdt.SymbolProperties.ContractMultiplier * 0.01m;

                    if (Math.Abs(btcUsdtHoldings.TotalSaleVolume - holdingsValueUsdt) > 1)
                    {
                        throw new Exception($"Unexpected TotalSaleVolume {btcUsdtHoldings.TotalSaleVolume}");
                    }
                    if (Math.Abs(btcUsdtHoldings.AbsoluteHoldingsCost - holdingsValueUsdt) > 1)
                    {
                        throw new Exception($"Unexpected holdings cost {btcUsdtHoldings.HoldingsCost}");
                    }
                    if (Math.Abs(btcUsdtHoldings.AbsoluteHoldingsCost * 0.05m - marginUsed) > 1
                        || _btcUsdt.BuyingPowerModel.GetMaintenanceMargin(_btcUsdt) != marginUsed)
                    {
                        throw new Exception($"Unexpected margin used {marginUsed}");
                    }

                    // position just opened should be just spread here
                    var unrealizedProfit = Portfolio.TotalUnrealizedProfit;
                    if ((5 - Math.Abs(unrealizedProfit)) < 0)
                    {
                        throw new Exception($"Unexpected TotalUnrealizedProfit {Portfolio.TotalUnrealizedProfit}");
                    }

                    if (Portfolio.TotalProfit != 0)
                    {
                        throw new Exception($"Unexpected TotalProfit {Portfolio.TotalProfit}");
                    }
                }
            }
            // let's revert our position
            else if (Transactions.OrdersCount == 3)
            {
                Sell(_btcUsd.Symbol, 300);

                var btcUsdHoldings = _btcUsd.Holdings;

                if (Math.Abs(btcUsdHoldings.AbsoluteHoldingsCost - 100 * 2) > 1)
                {
                    throw new Exception($"Unexpected holdings cost {btcUsdHoldings.HoldingsCost}");
                }

                Sell(_btcUsdt.Symbol, 0.03);

                var btcUsdtHoldings = _btcUsdt.Holdings;

                // USDT futures value is based on it's price
                var holdingsValueUsdt = _btcUsdt.Price * _btcUsdt.SymbolProperties.ContractMultiplier * 0.02m;

                if (Math.Abs(btcUsdtHoldings.AbsoluteHoldingsCost - holdingsValueUsdt) > 1)
                {
                    throw new Exception($"Unexpected holdings cost {btcUsdtHoldings.HoldingsCost}");
                }

                // position just opened should be just spread here
                var profit = Portfolio.TotalUnrealizedProfit;
                if ((5 - Math.Abs(profit)) < 0)
                {
                    throw new Exception($"Unexpected TotalUnrealizedProfit {Portfolio.TotalUnrealizedProfit}");
                }
                // we barely did any difference on the previous trade
                if ((5 - Math.Abs(Portfolio.TotalProfit)) < 0)
                {
                    throw new Exception($"Unexpected TotalProfit {Portfolio.TotalProfit}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");

            if (_interestPerSymbol.Any(kvp => kvp.Value == 0))
            {
                throw new Exception("Expected interest rate data for all symbols");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 8625;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100285.86"},
            {"End Equity", "100285.26"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.60"},
            {"Estimated Strategy Capacity", "$200000000.00"},
            {"Lowest Capacity Asset", "BTCUSDT 2V3"},
            {"Portfolio Turnover", "1.08%"},
            {"OrderListHash", "0157a5c7c2c8a8c13e984b72721aa0ca"}
        };
    }
}
