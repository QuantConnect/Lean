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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm where custom a <see cref="FeeModel"/> does not use Account the Currency
    /// </summary>
    public class FeeModelNotUsingAccountCurrency : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        // Adding this so we only trade once, so math is easier and clear
        private bool _alreadyTraded;
        private int _initialEurCash = 10000;
        private decimal _orderFeesInAccountCurrency;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); // Set Start Date
            SetEndDate(2018, 4, 4); // Set End Date
            // Set Strategy Cash (USD) to 0. This is required for
            // SetHoldings(_security.Symbol, 1) not to fail
            SetCash(0);

            // EUR/USD conversion rate will be updated dynamically
            // Note: the conversion rates are required in backtesting (for now) because of this issue:
            // https://github.com/QuantConnect/Lean/issues/1859
            SetCash("EUR", _initialEurCash, 1.23m);

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            _security = AddCrypto("BTCEUR");

            // This is required because in our custom model, NonAccountCurrencyCustomFeeModel,
            // fees will be charged in ETH (not Base, nor Quote, not account currency).
            // Setting the cash allows the system to add a data subscription to fetch required conversion rates.
            SetCash("ETH", 0, 0m);
            _security.FeeModel = new NonAccountCurrencyCustomFeeModel();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && !_alreadyTraded)
            {
                _alreadyTraded = true;
                SetHoldings(_security.Symbol, 1);
                Debug("Purchased Stock");
            }
            else
            {
                Liquidate(_security.Symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
            _orderFeesInAccountCurrency +=
                Portfolio.CashBook.ConvertToAccountCurrency(orderEvent.OrderFee.Value).Amount;
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"CashBook: {Portfolio.CashBook}");
            Log($"Holdings.TotalCloseProfit: {_security.Holdings.TotalCloseProfit()}");
            // Fees will be applied to the corresponding Cash currency. 1 ETH * 2 trades
            if (Portfolio.CashBook["ETH"].Amount != -2)
            {
                throw new Exception("Unexpected ETH cash amount: " +
                    $"{Portfolio.CashBook["ETH"].Amount}");
            }
            if (Portfolio.CashBook["USD"].Amount != 0)
            {
                throw new Exception("Unexpected USD cash amount: " +
                    $"{Portfolio.CashBook["USD"].Amount}");
            }
            if (Portfolio.CashBook["BTC"].Amount != 0)
            {
                throw new Exception("Unexpected BTC cash amount: " +
                    $"{Portfolio.CashBook["BTC"].Amount}");
            }
            if (Portfolio.CashBook.ContainsKey(Currencies.NullCurrency))
            {
                throw new Exception("Unexpected NullCurrency cash");
            }

            var closedTrade = TradeBuilder.ClosedTrades[0];
            var profitInQuoteCurrency = (closedTrade.ExitPrice - closedTrade.EntryPrice)
                * closedTrade.Quantity;
            if (Portfolio.CashBook["EUR"].Amount != _initialEurCash + profitInQuoteCurrency)
            {
                throw new Exception("Unexpected EUR cash amount: " +
                    $"{Portfolio.CashBook["EUR"].Amount}");
            }
            if (closedTrade.TotalFees != _orderFeesInAccountCurrency)
            {
                throw new Exception($"Unexpected closed trades total fees {closedTrade.TotalFees}");
            }
            if (_security.Holdings.TotalFees != _orderFeesInAccountCurrency)
            {
                throw new Exception($"Unexpected closed trades total fees {closedTrade.TotalFees}");
            }
        }

        internal class NonAccountCurrencyCustomFeeModel : FeeModel
        {
            public override OrderFee GetOrderFee(OrderFeeParameters parameters)
            {
                return new OrderFee(new CashAmount(1m, "ETH"));
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
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
            {"Total Fees", "$804.33"},
            {"Fitness Score", "0.504"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-15.574"},
            {"Portfolio Turnover", "2.057"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "-1116140375"}
        };
    }
}
