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
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Daily regression algorithm trading ADAUSDT binance futures long and short asserting the behavior
    /// </summary>
    public class CryptoFutureHourlyMarginInterestRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, int> _interestPerSymbol = new();
        private decimal _amountAfterTrade;

        protected CryptoFuture AdaUsdt;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            Initialize(Resolution.Hour);
        }

        protected virtual void Initialize(Resolution resolution)
        {
            SetStartDate(2022, 12, 12);
            SetEndDate(2022, 12, 13);

            SetTimeZone(NodaTime.DateTimeZone.Utc);
            SetBrokerageModel(BrokerageName.BinanceCoinFutures, AccountType.Margin);

            AdaUsdt = AddCryptoFuture("ADAUSDT", resolution);

            // Default USD cash, set 1M but it wont be used
            SetCash(1000000);

            // the amount of USDT we need to hold to trade 'ADAUSDT'
            AdaUsdt.QuoteCurrency.SetAmount(200);
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
                _interestPerSymbol.TryGetValue(interestRate.Key, out var count);
                _interestPerSymbol[interestRate.Key] = ++count;

                var cachedInterestRate = Securities[interestRate.Key].Cache.GetData<MarginInterestRate>();
                if (cachedInterestRate != interestRate.Value)
                {
                    throw new Exception($"Unexpected cached margin interest rate for {interestRate.Key}!");
                }
            }

            if(interestRates.Count != data.MarginInterestRates.Count)
            {
                throw new Exception($"Unexpected cached margin interest rate data!");
            }

            if (Portfolio.Invested)
            {
                return;
            }

            Buy(AdaUsdt.Symbol, 1000);

            _amountAfterTrade = Portfolio.CashBook["USDT"].Amount;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_interestPerSymbol.TryGetValue(AdaUsdt.Symbol, out var count) || count != 1)
            {
                throw new Exception($"Unexpected interest rate count {count}");
            }

            // negative because we are long. Rate * Value * Application Count
            var expectedFundingRateDifference = - (0.0001m * AdaUsdt.Holdings.HoldingsValue * 3);
            var finalCash = Portfolio.CashBook["USDT"].Amount;
            if (Math.Abs(finalCash - (_amountAfterTrade + expectedFundingRateDifference)) > Math.Abs(expectedFundingRateDifference * 0.05m))
            {
                throw new Exception($"Unexpected interest rate count {Portfolio.CashBook["USDT"].Amount}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 50;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000200"},
            {"End Equity", "1000207.90"},
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
            {"Total Fees", "$0.15"},
            {"Estimated Strategy Capacity", "$330000000.00"},
            {"Lowest Capacity Asset", "ADAUSDT 18R"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "f3d491f943932e64bc38b85d74eb5129"}
        };
    }
}
