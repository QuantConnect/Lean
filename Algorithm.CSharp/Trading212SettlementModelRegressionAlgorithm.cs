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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Exercises the Trading 212 settlement model end to end with a GBP account trading a
    /// USD-listed equity: the fill, its fee and the settlement are all booked in GBP, so the
    /// account never carries a USD balance.
    /// </summary>
    public class Trading212SettlementModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private int _day;
        private OrderTicket _buyTicket;
        private OrderTicket _sellTicket;
        private decimal _cashAfterBuy;
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

            // The GBP account converts every trade, so no USD cash is ever left behind.
            var usdCash = Portfolio.CashBook[Currencies.USD].Amount;
            if (usdCash != 0)
            {
                throw new RegressionTestException($"Expected no USD cash after a fill, but the cash book has {usdCash} USD.");
            }
            if (Portfolio.UnsettledCash != 0)
            {
                throw new RegressionTestException($"Expected immediate settlement, but {Portfolio.UnsettledCash} is unsettled.");
            }

            var gbpCash = Portfolio.CashBook[Currencies.GBP].Amount;
            if (orderEvent.Direction == OrderDirection.Buy)
            {
                if (gbpCash >= 100000)
                {
                    throw new RegressionTestException($"Expected the buy to debit GBP cash, but the balance is {gbpCash}.");
                }
                _cashAfterBuy = gbpCash;
            }
            else if (gbpCash <= _cashAfterBuy)
            {
                throw new RegressionTestException(
                    $"Expected the sale proceeds to be credited to GBP cash, but the balance went from {_cashAfterBuy} to {gbpCash}.");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_fills != 2)
            {
                throw new RegressionTestException($"Expected two fills, but there were {_fills}.");
            }
            if (Securities[_symbol].Holdings.Quantity != 0)
            {
                throw new RegressionTestException(
                    $"Expected a flat position, but the holding is {Securities[_symbol].Holdings.Quantity}.");
            }

            Log($"Cash book: {Portfolio.CashBook}");
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
            {"Total Orders", "2"}
        };
    }
}
