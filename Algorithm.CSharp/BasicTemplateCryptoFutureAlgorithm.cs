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
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Minute resolution regression algorithm trading Coin and USDT binance futures long and short asserting the behavior
    /// </summary>
    public class BasicTemplateCryptoFutureAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, int> _interestPerSymbol = new();
        private CryptoFuture _btcUsd;
        private CryptoFuture _adaUsdt;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2022, 12, 13); // Set Start Date
            SetEndDate(2022, 12, 13); // Set End Date

            SetTimeZone(TimeZones.Utc);

            try
            {
                SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Cash);
            }
            catch (InvalidOperationException)
            {
                // expected, we don't allow cash account type
            }
            SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin);

            _btcUsd = AddCryptoFuture("BTCUSD");
            _adaUsdt = AddCryptoFuture("ADAUSDT");

            _fast = EMA(_btcUsd.Symbol, 30, Resolution.Minute);
            _slow = EMA(_btcUsd.Symbol, 60, Resolution.Minute);

            _interestPerSymbol[_btcUsd.Symbol] = 0;
            _interestPerSymbol[_adaUsdt.Symbol] = 0;

            // Default USD cash, set 1M but it wont be used
            SetCash(1000000);

            // the amount of BTC we need to hold to trade 'BTCUSD'
            _btcUsd.BaseCurrency.SetAmount(0.005m);
            // the amount of USDT we need to hold to trade 'ADAUSDT'
            _adaUsdt.QuoteCurrency.SetAmount(200);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
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

            if (_fast > _slow)
            {
                if (!Portfolio.Invested && Transactions.OrdersCount == 0)
                {
                    var ticket = Buy(_btcUsd.Symbol, 50);
                    if (ticket.Status != OrderStatus.Invalid)
                    {
                        throw new Exception($"Unexpected valid order {ticket}, should fail due to margin not sufficient");
                    }

                    Buy(_btcUsd.Symbol, 1);

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

                    Buy(_adaUsdt.Symbol, 1000);

                    marginUsed = Portfolio.TotalMarginUsed - marginUsed;
                    var adaUsdtHoldings = _adaUsdt.Holdings;

                    // USDT/BUSD futures value is based on it's price
                    var holdingsValueUsdt = _adaUsdt.Price * _adaUsdt.SymbolProperties.ContractMultiplier * 1000;

                    if (Math.Abs(adaUsdtHoldings.TotalSaleVolume - holdingsValueUsdt) > 1)
                    {
                        throw new Exception($"Unexpected TotalSaleVolume {adaUsdtHoldings.TotalSaleVolume}");
                    }
                    if (Math.Abs(adaUsdtHoldings.AbsoluteHoldingsCost - holdingsValueUsdt) > 1)
                    {
                        throw new Exception($"Unexpected holdings cost {adaUsdtHoldings.HoldingsCost}");
                    }
                    if (Math.Abs(adaUsdtHoldings.AbsoluteHoldingsCost * 0.05m - marginUsed) > 1
                        || _adaUsdt.BuyingPowerModel.GetMaintenanceMargin(_adaUsdt) != marginUsed)
                    {
                        throw new Exception($"Unexpected margin used {marginUsed}");
                    }

                    // position just opened should be just spread here
                    var profit = Portfolio.TotalUnrealizedProfit;
                    if ((5 - Math.Abs(profit)) < 0)
                    {
                        throw new Exception($"Unexpected TotalUnrealizedProfit {Portfolio.TotalUnrealizedProfit}");
                    }

                    if (Portfolio.TotalProfit != 0)
                    {
                        throw new Exception($"Unexpected TotalProfit {Portfolio.TotalProfit}");
                    }
                }
            }
            else
            {
                // let's revert our position and double
                if (Time.Hour > 10 && Transactions.OrdersCount == 3)
                {
                    Sell(_btcUsd.Symbol, 3);

                    var btcUsdHoldings = _btcUsd.Holdings;

                    if (Math.Abs(btcUsdHoldings.AbsoluteHoldingsCost - 100 * 2) > 1)
                    {
                        throw new Exception($"Unexpected holdings cost {btcUsdHoldings.HoldingsCost}");
                    }

                    Sell(_adaUsdt.Symbol, 3000);

                    var adaUsdtHoldings = _adaUsdt.Holdings;

                    // USDT/BUSD futures value is based on it's price
                    var holdingsValueUsdt = _adaUsdt.Price * _adaUsdt.SymbolProperties.ContractMultiplier * 2000;

                    if (Math.Abs(adaUsdtHoldings.AbsoluteHoldingsCost - holdingsValueUsdt) > 1)
                    {
                        throw new Exception($"Unexpected holdings cost {adaUsdtHoldings.HoldingsCost}");
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
        }

        public override void OnEndOfAlgorithm()
        {
            if (_interestPerSymbol[_adaUsdt.Symbol] != 1)
            {
                throw new Exception($"Unexpected interest rate count {_interestPerSymbol[_adaUsdt.Symbol]}");
            }

            if (_interestPerSymbol[_btcUsd.Symbol] != 3)
            {
                throw new Exception($"Unexpected interest rate count {_interestPerSymbol[_btcUsd.Symbol]}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
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
        public long DataPoints => 7205;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Start Equity", "1000200.00"},
            {"End Equity", "1000278.02"},
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
            {"Total Fees", "$0.65"},
            {"Estimated Strategy Capacity", "$500000000.00"},
            {"Lowest Capacity Asset", "ADAUSDT 18R"},
            {"Portfolio Turnover", "0.16%"},
            {"OrderListHash", "dcc4f964b5549c753123848c32eaee41"}
        };
    }
}
