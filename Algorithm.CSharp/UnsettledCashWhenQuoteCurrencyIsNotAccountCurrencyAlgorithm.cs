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
    /// Algorithm asserting that the unsettled cash book is updated correctly when the quote currency is not the account currency.
    /// Reproduces GH issue #6859.
    /// </summary>
    public class UnsettledCashWhenQuoteCurrencyIsNotAccountCurrencyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        private decimal _lastUnsettledCash;

        private DateTime _lastTradeDate;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);

            SetAccountCurrency("EUR");
            SetCash(100000);

            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Cash);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        public override void OnData(Slice data)
        {
            if (Time - _lastTradeDate < TimeSpan.FromHours(1))
            {
                return;
            }

            _lastTradeDate = Time;

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 0.1);
            }
            else
            {
                Liquidate();
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled && orderEvent.Direction == OrderDirection.Sell)
            {
                Debug($"OrderEvent: {orderEvent}");
                Debug($"CashBook:\n{Portfolio.CashBook}\n");
                Debug($"UnsettledCashBook:\n{Portfolio.UnsettledCashBook}\n");

                if (!Portfolio.UnsettledCashBook.TryGetValue(orderEvent.FillPriceCurrency, out var unsettledCash))
                {
                    throw new Exception($"Unsettled cash entry for {orderEvent.FillPriceCurrency} not found");
                }

                var expectedUnsettledCash = Math.Abs(orderEvent.FillPrice * orderEvent.FillQuantity);
                var actualUnsettledCash = unsettledCash.Amount - _lastUnsettledCash;
                if (actualUnsettledCash != expectedUnsettledCash)
                {
                    throw new Exception($"Expected unsettled cash to be {expectedUnsettledCash} but was {actualUnsettledCash}");
                }

                _lastUnsettledCash = unsettledCash.Amount;
            }
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
        public long DataPoints => 1561;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 7622;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "14"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "€10.32"},
            {"Estimated Strategy Capacity", "€7700000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0.796"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.796"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "€0"},
            {"Total Accumulated Estimated Alpha Value", "€0"},
            {"Mean Population Estimated Insight Value", "€0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1cd26ea7fbdc73902f4d63d92929aa0e"}
        };
    }
}
