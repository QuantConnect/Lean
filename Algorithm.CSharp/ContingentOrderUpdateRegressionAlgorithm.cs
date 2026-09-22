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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of updating contingent orders: orders held waiting for their parent to fill
    /// can be updated, as well as the parent and the orders already working. An order held with a marketable price does not fill
    /// until it's triggered, and once triggered it requires new data to fill, just like any other order.
    /// </summary>
    public class ContingentOrderUpdateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private int _step;
        private OrderTicket _entry;
        private OrderTicket _takeProfit;
        private OrderTicket _stopLoss;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 07);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            var price = Securities[_symbol].Price;
            switch (_step)
            {
                case 0:
                    // the entry is far from the market price, won't fill
                    var tickets = BracketOrder(_symbol, 100, takeProfitPrice: Math.Round(price * 1.1m, 2), stopLossPrice: Math.Round(price * 0.8m, 2),
                        limitPrice: Math.Round(price * 0.9m, 2));
                    _entry = tickets[0];
                    _takeProfit = tickets[1];
                    _stopLoss = tickets[2];
                    break;

                case 1:
                    // update the held orders: the take profit gets a marketable price, below the market price, it would fill if it was working
                    AssertSuccess(_takeProfit.UpdateLimitPrice(Math.Round(price * 0.95m, 2), "Updated take profit"));
                    AssertSuccess(_stopLoss.Update(new UpdateOrderFields { StopPrice = Math.Round(price * 0.85m, 2), Quantity = -100, Tag = "Updated stop loss" }));
                    break;

                case 2:
                case 3:
                    if (_takeProfit.Status != OrderStatus.UpdateSubmitted || !_takeProfit.Contingency.IsWaitingForTrigger || _takeProfit.QuantityFilled != 0
                        || _takeProfit.Tag != "Updated take profit" || _takeProfit.Get(OrderField.LimitPrice) >= price
                        || _stopLoss.Status != OrderStatus.UpdateSubmitted || !_stopLoss.Contingency.IsWaitingForTrigger || _stopLoss.Tag != "Updated stop loss")
                    {
                        throw new RegressionTestException($"Expected the held orders to be updated but not filled: {_takeProfit} | {_stopLoss}");
                    }

                    if (_step == 3)
                    {
                        // update the entry so it fills
                        AssertSuccess(_entry.UpdateLimitPrice(Math.Round(price * 1.01m, 2), "Updated entry"));
                    }
                    break;

                case 4:
                    // the updated entry filled right away triggering its children, which require new data to fill: just like any other order
                    // they don't fill with the data from the time they start working. So the marketable take profit filled with the next data,
                    // canceling the stop loss
                    if (_takeProfit.Status != OrderStatus.Filled || _stopLoss.Status != OrderStatus.Canceled || Portfolio.Invested)
                    {
                        throw new RegressionTestException($"Expected the take profit to be filled and the stop loss canceled: {_takeProfit} | {_stopLoss}");
                    }

                    var entryFillTime = _entry.OrderEvents.Single(x => x.Status == OrderStatus.Filled).UtcTime;
                    var takeProfitFillTime = _takeProfit.OrderEvents.Single(x => x.Status == OrderStatus.Filled).UtcTime;
                    if (takeProfitFillTime != entryFillTime.AddMinutes(1))
                    {
                        throw new RegressionTestException($"Expected the take profit to fill the minute after the entry, entry: {entryFillTime} take profit: {takeProfitFillTime}");
                    }

                    // closed orders can't be updated
                    if (_stopLoss.UpdateStopPrice(1).IsSuccess)
                    {
                        throw new RegressionTestException("Expected the update of a canceled order to fail");
                    }
                    break;
            }
            _step++;
        }

        private static void AssertSuccess(OrderResponse response)
        {
            if (!response.IsSuccess)
            {
                throw new RegressionTestException($"Expected the order request to succeed: {response}");
            }
        }

        /// <summary>
        /// End of algorithm run event handler
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_step < 5)
            {
                throw new RegressionTestException($"Unexpected step count {_step}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 795;

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
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100009.24"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "28.94%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "ce48af81e6d765f281d9ef34d6054056"}
        };
    }
}
