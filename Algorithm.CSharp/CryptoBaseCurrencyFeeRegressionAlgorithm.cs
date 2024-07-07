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
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base crypto account regression algorithm trading in and out
    /// </summary>
    public abstract class CryptoBaseCurrencyFeeRegressionAlgorithm
        : QCAlgorithm,
            IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        /// <summary>
        /// The target account type
        /// </summary>
        protected abstract AccountType AccountType { get; }

        /// <summary>
        /// The target brokerage model name
        /// </summary>
        protected BrokerageName BrokerageName { get; set; }

        /// <summary>
        /// The pair to add and trade
        /// </summary>
        protected string Pair { get; set; }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetBrokerageModel(BrokerageName, AccountType);
            _symbol = AddCrypto(Pair, Resolution.Hour).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                CurrencyPairUtil.DecomposeCurrencyPair(
                    _symbol,
                    out var baseCurrency,
                    out var quoteCurrency
                );

                var initialQuoteCurrency = Portfolio.CashBook[quoteCurrency].Amount;
                var ticket = Buy(_symbol, 0.1m);
                var filledEvent = ticket.OrderEvents.Single(orderEvent =>
                    orderEvent.Status == OrderStatus.Filled
                );

                if (
                    Portfolio.CashBook[baseCurrency].Amount != ticket.QuantityFilled
                    || filledEvent.FillQuantity != ticket.QuantityFilled
                    || (0.1m - filledEvent.OrderFee.Value.Amount) != ticket.QuantityFilled
                )
                {
                    throw new RegressionTestException(
                        $"Unexpected BaseCurrency portfolio status. Event {filledEvent}. CashBook: {Portfolio.CashBook}. "
                    );
                }

                if (
                    Portfolio.CashBook[quoteCurrency].Amount
                    != (initialQuoteCurrency - 0.1m * filledEvent.FillPrice)
                )
                {
                    throw new RegressionTestException(
                        $"Unexpected QuoteCurrency portfolio status. Event {filledEvent}. CashBook: {Portfolio.CashBook}. "
                    );
                }

                if (
                    Securities[_symbol].Holdings.Quantity
                    != (0.1m - filledEvent.OrderFee.Value.Amount)
                )
                {
                    throw new RegressionTestException(
                        $"Unexpected Holdings: {Securities[_symbol].Holdings}. Event {filledEvent}"
                    );
                }
            }
            else
            {
                Liquidate();
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
        public virtual long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
