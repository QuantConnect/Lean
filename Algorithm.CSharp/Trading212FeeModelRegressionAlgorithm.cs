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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Exercises the Trading 212 fee model and settlement end to end with a GBP account trading a
    /// USD-listed equity: the FX fee is charged on both fills and the US regulatory fees on the sale,
    /// all settled in the account currency. The fills' proceeds settle immediately, so the GBP cash
    /// book ends at the starting cash less the fees and the converted buy, plus the converted sale.
    /// </summary>
    public class Trading212FeeModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private int _day;
        private OrderTicket _buyTicket;
        private OrderTicket _sellTicket;
        private decimal _buyValueInAccountCurrency;
        private decimal _sellValueInAccountCurrency;
        private decimal _feesInAccountCurrency;
        private int _fills;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 21);
            SetAccountCurrency(Currencies.GBP, 100000);
            SetBrokerageModel(BrokerageName.Trading212);
            _symbol = AddEquity("AAPL", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_symbol))
            {
                return;
            }

            switch (++_day)
            {
                case 1:
                    _buyTicket = MarketOrder(_symbol, 10, asynchronous: true);
                    break;

                case 2:
                    if (_buyTicket.Status != OrderStatus.Filled)
                    {
                        throw new RegressionTestException($"Expected the buy to fill, but the status is {_buyTicket.Status}.");
                    }
                    _sellTicket = MarketOrder(_symbol, -10, asynchronous: true);
                    break;

                case 3:
                    if (_sellTicket.Status != OrderStatus.Filled)
                    {
                        throw new RegressionTestException($"Expected the sell to fill, but the status is {_sellTicket.Status}.");
                    }
                    break;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            _fills++;
            var conversionRate = Securities[_symbol].QuoteCurrency.ConversionRate;
            var tradeValueInQuoteCurrency = orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice;
            var tradeValueInAccountCurrency = tradeValueInQuoteCurrency * conversionRate;
            if (orderEvent.Direction == OrderDirection.Sell)
            {
                _sellValueInAccountCurrency += tradeValueInAccountCurrency;
            }
            else
            {
                _buyValueInAccountCurrency += tradeValueInAccountCurrency;
            }

            // The fees are computed in the quote currency and reported, converted, in GBP.
            var expected = 0.0015m * tradeValueInQuoteCurrency;
            if (orderEvent.Direction == OrderDirection.Sell)
            {
                expected += 0.0000206m * tradeValueInQuoteCurrency
                    + Math.Min(0.000195m * orderEvent.AbsoluteFillQuantity, 9.79m);
            }
            expected *= conversionRate;

            AssertFee(orderEvent, Currencies.GBP, expected);

            _feesInAccountCurrency += orderEvent.OrderFee.Value.Amount;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_fills != 2)
            {
                throw new RegressionTestException($"Expected two fills, but there were {_fills}.");
            }

            // Starting cash, less the buy and the fees, plus the sale proceeds, all in GBP.
            var expectedGbp = 100000m - _buyValueInAccountCurrency - _feesInAccountCurrency + _sellValueInAccountCurrency;
            var actualGbp = Portfolio.CashBook[Currencies.GBP].Amount;
            if (Math.Abs(actualGbp - expectedGbp) > 0.0001m)
            {
                throw new RegressionTestException(
                    $"Expected {expectedGbp} GBP after {_feesInAccountCurrency} of fees, but the cash book has {actualGbp} GBP.");
            }

            // The USD cash is fully converted to GBP by the settlement model, so none is left behind.
            var actualUsd = Portfolio.CashBook[Currencies.USD].Amount;
            if (Math.Abs(actualUsd) > 0.000001m)
            {
                throw new RegressionTestException($"Expected no USD cash, but the cash book has {actualUsd} USD.");
            }

            if (Portfolio.UnsettledCash != 0)
            {
                throw new RegressionTestException($"Expected no unsettled cash, but there is {Portfolio.UnsettledCash}.");
            }
        }

        private static void AssertFee(OrderEvent orderEvent, string currency, decimal expected)
        {
            var fee = orderEvent.OrderFee.Value;
            if (fee.Currency != currency)
            {
                throw new RegressionTestException($"Expected the fee in {currency}, but it is in {fee.Currency}.");
            }
            // The fee model values the order at the order price, which can differ slightly from the
            // fill price, so allow a small relative tolerance. The unit tests pin the exact arithmetic.
            var tolerance = Math.Max(Math.Abs(expected) * 0.01m, 0.000001m);
            if (Math.Abs(fee.Amount - expected) > tolerance)
            {
                throw new RegressionTestException($"Expected a fee of about {expected} {currency}, but it is {fee.Amount}.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 101;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 5;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.051%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100002.06"},
            {"Net Profit", "0.002%"},
            {"Sharpe Ratio", "-118.859"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.050%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.007"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.813"},
            {"Tracking Error", "0.133"},
            {"Treynor Ratio", "-26.403"},
            {"Total Fees", "₤0.29"},
            {"Estimated Strategy Capacity", "₤140000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.01%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "2a4c36261b0d1d4fc5a35ec35930ffe2"}
        };
    }
}
