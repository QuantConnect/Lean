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
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that BNFCR can serve as sole collateral for Binance
    /// USDⓈ-M futures for EU/MiCA users. Verifies that buying power, holdings cost,
    /// margin accounting and portfolio properties are all accurate when USDT balance is zero
    /// and only BNFCR (pegged 1:1 to USD) is present.
    /// </summary>
    public class BinanceCryptoFutureBnfcrCollateralRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CryptoFuture _adaUsdt;
        private bool _orderPlaced;

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);
            SetTimeZone(TimeZones.Utc);
            SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin);

            _adaUsdt = AddCryptoFuture("ADAUSDT");

            // EU/MiCA account: no USDT, BNFCR is the sole collateral (pegged 1:1 to USD).
            // CashBook.Convert("BNFCR" -> "USDT") routes through USD: 200 * 1 / 1 = 200.
            SetCash(0);
            SetCash("BNFCR", 200m, 1m);
        }

        public override void OnData(Slice slice)
        {
            if (_adaUsdt.Price == 0)
            {
                return;
            }

            if (!_orderPlaced && !Portfolio.Invested)
            {
                // --- Assert initial state ---

                // BNFCR must be present and positive
                if (!Portfolio.CashBook.ContainsKey("BNFCR") || Portfolio.CashBook["BNFCR"].Amount != 200)
                {
                    throw new RegressionTestException($"Expected 200 BNFCR in CashBook, got {Portfolio.CashBook["BNFCR"].Amount}");
                }

                // Primary collateral (USDT) must be zero
                if (Portfolio.CashBook.ContainsKey("USDT") && Portfolio.CashBook["USDT"].Amount != 0)
                {
                    throw new RegressionTestException($"Expected zero USDT, got {Portfolio.CashBook["USDT"].Amount}");
                }

                // Buying power must reflect BNFCR collateral — not zero
                var buyingPower = _adaUsdt.BuyingPowerModel.GetBuyingPower(new BuyingPowerParameters(Portfolio, _adaUsdt, OrderDirection.Buy));
                if (buyingPower.Value <= 0)
                {
                    throw new RegressionTestException($"Expected positive buying power from BNFCR collateral, got {buyingPower.Value}");
                }

                // --- Place order ---
                var ticket = Buy(_adaUsdt.Symbol, 1000);
                _orderPlaced = true;

                if (ticket.Status == OrderStatus.Invalid)
                {
                    throw new RegressionTestException("Order was rejected — BNFCR collateral should be sufficient to cover margin");
                }

                // --- Assert holdings after fill ---
                var holdings = _adaUsdt.Holdings;
                var expectedNotional = _adaUsdt.Price * _adaUsdt.SymbolProperties.ContractMultiplier * 1000;

                if (Math.Abs(holdings.AbsoluteHoldingsCost - expectedNotional) > 1)
                {
                    throw new RegressionTestException($"Unexpected AbsoluteHoldingsCost {holdings.AbsoluteHoldingsCost}, expected ~{expectedNotional}");
                }

                if (Math.Abs(holdings.TotalSaleVolume - expectedNotional) > 1)
                {
                    throw new RegressionTestException($"Unexpected TotalSaleVolume {holdings.TotalSaleVolume}, expected ~{expectedNotional}");
                }

                // --- Assert margin accounting ---
                var marginUsed = Portfolio.TotalMarginUsed;
                if (marginUsed <= 0)
                {
                    throw new RegressionTestException($"Expected positive TotalMarginUsed after fill, got {marginUsed}");
                }

                var maintenanceMargin = _adaUsdt.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForCurrentHoldings(_adaUsdt));
                if (maintenanceMargin != marginUsed)
                {
                    throw new RegressionTestException($"Maintenance margin {maintenanceMargin} does not match TotalMarginUsed {marginUsed}");
                }

                // Position just opened — unrealized PnL should be near zero (spread only)
                if (Math.Abs(Portfolio.TotalUnrealizedProfit) > 5)
                {
                    throw new RegressionTestException($"Unexpected TotalUnrealizedProfit {Portfolio.TotalUnrealizedProfit}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Transactions.OrdersCount != 1)
            {
                throw new RegressionTestException($"Expected exactly 1 order, got {Transactions.OrdersCount}");
            }

            if (!Portfolio.CashBook.ContainsKey("BNFCR"))
            {
                throw new RegressionTestException("BNFCR must remain in CashBook throughout the algorithm");
            }

            if (!Portfolio.Invested)
            {
                throw new RegressionTestException("Expected an open ADAUSDT position at end of algorithm");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent}");
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
        public long DataPoints => 4322;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200"},
            {"End Equity", "206.86"},
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
            {"Total Fees", "$0.12"},
            {"Estimated Strategy Capacity", "$340000.00"},
            {"Lowest Capacity Asset", "ADAUSDT 18R"},
            {"Portfolio Turnover", "148.31%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "177ae917deb456790cfbcaaaf1ec1f5c"}
        };
    }
}
