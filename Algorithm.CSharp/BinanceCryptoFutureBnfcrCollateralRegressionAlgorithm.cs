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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that BNFCR serves as collateral for Binance USDⓈ-M futures
    /// (EU/MiCA Credits Trading Mode) and that futures with different quote currencies (ADAUSDT, ETHUSDC)
    /// correctly share the BNFCR collateral pool.
    /// </summary>
    public class BinanceCryptoFutureBnfcrCollateralRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CryptoFuture _adaUsdt;
        private CryptoFuture _ethUsdc;
        private bool _orderPlaced;

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);
            SetTimeZone(TimeZones.Utc);
            SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin);

            _adaUsdt = AddCryptoFuture("ADAUSDT");
            _ethUsdc = AddCryptoFuture("ETHUSDC");

            SetCash(0);
            SetCash("BNFCR", 200m, 1m);
            SetCash("ETH", 0, 1600);
            SetCash("USDC", 0, 1);
        }

        public override void OnData(Slice slice)
        {
            if (_adaUsdt.Price == 0 || _orderPlaced)
            {
                return;
            }

            // 1. BNFCR collateral must produce positive buying power (USDT is zero)
            var buyingPower = _adaUsdt.BuyingPowerModel.GetBuyingPower(new BuyingPowerParameters(Portfolio, _adaUsdt, OrderDirection.Buy));
            if (buyingPower.Value <= 0)
            {
                throw new RegressionTestException($"Expected positive buying power from BNFCR, got {buyingPower.Value}");
            }

            // 2. Order must not be rejected
            var ticket = Buy(_adaUsdt.Symbol, 1000);
            _orderPlaced = true;
            if (ticket.Status == OrderStatus.Invalid)
            {
                throw new RegressionTestException("Order rejected — BNFCR collateral should cover margin");
            }

            // 3. Margin must be tracked
            if (Portfolio.TotalMarginUsed <= 0)
            {
                throw new RegressionTestException($"Expected positive TotalMarginUsed, got {Portfolio.TotalMarginUsed}");
            }

            // 4. Shared collateral: ETHUSDC (different quote currency) must deduct ADAUSDT margin
            _ethUsdc.SetMarketPrice(new TradeBar { Time = Time, Symbol = _ethUsdc.Symbol, Close = 1600 });

            var ethBuyingPower = _ethUsdc.BuyingPowerModel.GetBuyingPower(new BuyingPowerParameters(Portfolio, _ethUsdc, OrderDirection.Buy));
            var adaBuyingPower = _adaUsdt.BuyingPowerModel.GetBuyingPower(new BuyingPowerParameters(Portfolio, _adaUsdt, OrderDirection.Buy));

            // ETHUSDC must see less buying power than ADAUSDT - ADAUSDT maintenance margin
            // is deducted from ETHUSDC's shared pool, but ADAUSDT skips itself.
            if (ethBuyingPower.Value >= adaBuyingPower.Value)
            {
                throw new RegressionTestException(
                    $"ETHUSDC buying power ({ethBuyingPower.Value}) must be less than ADAUSDT ({adaBuyingPower.Value}) " +
                    $"— shared BNFCR pool must deduct ADAUSDT maintenance margin");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new RegressionTestException("Expected an open position at end of algorithm");
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
