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
        protected virtual DateTime StartDate => new DateTime(2020, 1, 6);
        protected virtual DateTime EndDate => new DateTime(2020, 1, 8);

        public override void Initialize()
        {
            SetStartDate(StartDate);
            SetEndDate(EndDate);

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
            // On daily resolution the order fills at the daily close, so a same-day buy + liquidate cannot work
            // (the buy would not fill until after the liquidation). Buy on the first day with available data and
            // liquidate the next day, once the purchase has filled at the previous close.
            Schedule.On(DateRules.On(2020, 1, 7), TimeRules.At(10, 0, 0), () =>
            {
                Ticket = MarketOrder(ESOption, 1);
            });

            Schedule.On(DateRules.On(2020, 1, 8), TimeRules.At(14, 0, 0), () =>
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
        public virtual long DataPoints => 36;

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
            {"Average Win", "1.50%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "640.945%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101497.16"},
            {"Net Profit", "1.497%"},
            {"Sharpe Ratio", "32.826"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "4.881"},
            {"Beta", "1.842"},
            {"Annual Standard Deviation", "0.168"},
            {"Annual Variance", "0.028"},
            {"Information Ratio", "67.238"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "3"},
            {"Total Fees", "$2.84"},
            {"Estimated Strategy Capacity", "$66000.00"},
            {"Lowest Capacity Asset", "ES XCZJLCEYO5XG|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "3.30%"},
            {"Drawdown Recovery", "1"},
            {"OrderListHash", "ca2b881524d4b9307e19a4f84ab4f5d7"}
        };
    }
}

