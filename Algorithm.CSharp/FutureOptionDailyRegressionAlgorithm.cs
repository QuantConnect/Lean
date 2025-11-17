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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests using FutureOptions daily resolution
    /// </summary>
    public class FutureOptionDailyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected OrderTicket Ticket { get; set; }
        protected Symbol ESOption { get; set; }
        protected virtual Resolution Resolution => Resolution.Daily;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 7);
            SetEndDate(2020, 1, 8);

            // Add our underlying future contract
            var futureContract = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 3, 20)),
                Resolution).Symbol;

            // Attempt to fetch a specific future option contract
            ESOption = OptionChain(futureContract)
                .Where(x => x.ID.StrikePrice == 3200m && x.ID.OptionRight == OptionRight.Call)
                .Select(x => AddFutureOptionContract(x, Resolution).Symbol)
                .FirstOrDefault();

            // Validate it is the expected contract
            var expectedContract = QuantConnect.Symbol.CreateOption(futureContract, Market.CME, OptionStyle.American,
                OptionRight.Call, 3200m,
                new DateTime(2020, 3, 20));

            if (ESOption != expectedContract)
            {
                throw new RegressionTestException($"Contract {ESOption} was not the expected contract {expectedContract}");
            }

            ScheduleBuySell();
        }

        protected virtual void ScheduleBuySell()
        {
            // Schedule a purchase of this contract tomorrow at 10AM when the market is open
            Schedule.On(DateRules.Tomorrow, TimeRules.At(10,0,0), () =>
            {
                Ticket = MarketOrder(ESOption, 1);
            });

            // Schedule liquidation tomorrow at 2PM when the market is open
            Schedule.On(DateRules.Tomorrow, TimeRules.At(14,0,0), () =>
            {
                Liquidate();
            });
        }

        public override void OnData(Slice slice)
        {
            // Assert we are only getting data at 5PM NY, for ES future market closes at 17pm NY
            if (slice.Time.Hour != 17)
            {
                throw new ArgumentException($"Expected data at 4PM each day; instead was {slice.Time}");
            }
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="RegressionTestException">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new RegressionTestException($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }

            if (Ticket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("Future option order failed to fill correctly");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 27;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99997.16"},
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
            {"Total Fees", "$2.84"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "ES XCZJLCEYJVLW|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "4.24%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "3917ccec8393e2505e9a5183bccfd237"}
        };
    }
}

