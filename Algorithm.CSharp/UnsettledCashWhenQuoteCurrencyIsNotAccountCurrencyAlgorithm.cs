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
using QuantConnect.Securities.CurrencyConversion;
using QuantConnect.Securities.Equity;

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

        private DateTime _lastUnsettledCashUpdatedDate;

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

        public override void OnData(Slice slice)
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
                    throw new RegressionTestException($"Unsettled cash entry for {orderEvent.FillPriceCurrency} not found");
                }

                // Clear _lastUnsettledCash if the settlement period has elapsed
                if (orderEvent.UtcTime.Date >= _lastUnsettledCashUpdatedDate.AddDays(Equity.DefaultSettlementDays).Date)
                {
                    _lastUnsettledCash = 0;
                }

                var expectedUnsettledCash = Math.Abs(orderEvent.FillPrice * orderEvent.FillQuantity);
                var actualUnsettledCash = unsettledCash.Amount - _lastUnsettledCash;
                if (actualUnsettledCash != expectedUnsettledCash)
                {
                    throw new RegressionTestException($"Expected unsettled cash to be {expectedUnsettledCash} but was {actualUnsettledCash}");
                }

                _lastUnsettledCash = unsettledCash.Amount;
                _lastUnsettledCashUpdatedDate = orderEvent.UtcTime;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var kvp in Portfolio.CashBook)
            {
                var symbol = kvp.Key;
                var cash = kvp.Value;
                var unsettledCash = Portfolio.UnsettledCashBook[symbol];

                if (unsettledCash.ConversionRate != cash.ConversionRate)
                {
                    throw new RegressionTestException($@"Unsettled cash conversion rate for {symbol} is {unsettledCash.ConversionRate} but should be {cash.ConversionRate}");
                }

                var accountCurrency = Portfolio.CashBook.AccountCurrency;

                if (unsettledCash.Symbol == accountCurrency)
                {
                    if (unsettledCash.ConversionRate != 1)
                    {
                        throw new RegressionTestException($@"Conversion rate for {unsettledCash.Symbol} (the account currency) in the UnsettledCashBook should be 1 but was {unsettledCash.ConversionRate}.");
                    }

                    if (unsettledCash.CurrencyConversion.GetType() != typeof(ConstantCurrencyConversion) ||
                        unsettledCash.CurrencyConversion.SourceCurrency != accountCurrency ||
                        unsettledCash.CurrencyConversion.DestinationCurrency != accountCurrency)
                    {
                        throw new RegressionTestException($@"Currency conversion for {unsettledCash.Symbol} (the account currency) in the UnsettledCashBook should be an identity conversion of type {nameof(ConstantCurrencyConversion)}");
                    }
                }
                else
                {
                    if (unsettledCash.CurrencyConversion.GetType() != typeof(SecurityCurrencyConversion))
                    {
                        throw new RegressionTestException($@"Currency conversion for {unsettledCash.Symbol} in the UnsettledCashBook should be of type {nameof(SecurityCurrencyConversion)}");
                    }

                    var sourceCurrency = unsettledCash.CurrencyConversion.SourceCurrency;
                    var destinationCurrency = unsettledCash.CurrencyConversion.DestinationCurrency;

                    if (!(
                        (sourceCurrency == accountCurrency && destinationCurrency == unsettledCash.Symbol) ||
                        (sourceCurrency == unsettledCash.Symbol && destinationCurrency == accountCurrency)
                        ))
                    {
                        throw new RegressionTestException($@"Currency conversion for {unsettledCash.Symbol} in UnsettledCashBook is not correct. Source and destination currency should have been {accountCurrency} and {unsettledCash.Symbol} or vice versa but were {sourceCurrency} and {destinationCurrency}.");
                    }
                }
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
        public long DataPoints => 1561;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 7594;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "14"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99981.05"},
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
            {"Total Fees", "€10.32"},
            {"Estimated Strategy Capacity", "€7700000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "69.61%"},
            {"OrderListHash", "ee7f00badd1a38ca21e51f610ba88044"}
        };
    }
}
