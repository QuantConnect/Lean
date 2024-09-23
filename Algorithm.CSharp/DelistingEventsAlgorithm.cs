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
 *
*/

using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using the Delisting event in your algorithm. Assets are delisted on their last day of trading, or when their contract expires.
    /// This data is not included in the open source project.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="data event handlers" />
    /// <meta name="tag" content="delisting event" />
    public class DelistingEventsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _receivedDelistedWarningEvent;
        private bool _receivedDelistedEvent;
        private int _receivedSecurityChangesEvent;
        private int _dataCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2007, 05, 15);  //Set Start Date
            SetEndDate(2007, 05, 25);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "AAA.1", Resolution.Daily);
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            _dataCount += slice.Bars.Count;
            if (Transactions.OrdersCount == 0)
            {
                SetHoldings("AAA.1", 1);
                Debug("Purchased Stock");
            }

            foreach (var kvp in slice.Bars)
            {
                var symbol = kvp.Key;
                var tradeBar = kvp.Value;
                Debug($"OnData(Slice): {Time}: {symbol}: {tradeBar.Close.ToStringInvariant("0.00")}");
            }

            // the slice can also contain delisting data: data.Delistings in a dictionary string->Delisting

            var aaa = Securities["AAA.1"];
            if (aaa.IsDelisted && aaa.IsTradable)
            {
                throw new RegressionTestException("Delisted security must NOT be tradable");
            }
            if (!aaa.IsDelisted && !aaa.IsTradable)
            {
                throw new RegressionTestException("Securities must be marked as tradable until they're delisted or removed from the universe");
            }

            foreach (var kvp in slice.Delistings)
            {
                var symbol = kvp.Key;
                var delisting = kvp.Value;
                if (delisting.Type == DelistingType.Warning)
                {
                    _receivedDelistedWarningEvent = true;
                    Debug($"OnData(Delistings): {Time}: {symbol} will be delisted at end of day today.");

                    // liquidate on delisting warning
                    SetHoldings(symbol, 0);
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    _receivedDelistedEvent = true;
                    Debug($"OnData(Delistings): {Time}: {symbol} has been delisted.");

                    // fails because the security has already been delisted and is no longer tradable
                    SetHoldings(symbol, 1);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"OnOrderEvent(OrderEvent): {Time}: {orderEvent}");
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var removedSecurity in changes.RemovedSecurities)
            {
                if (removedSecurity.Symbol.Value == "AAA.1")
                {
                    _receivedSecurityChangesEvent++;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedDelistedEvent)
            {
                throw new RegressionTestException("Did not receive expected delisted event");
            }
            if (!_receivedDelistedWarningEvent)
            {
                throw new RegressionTestException("Did not receive expected delisted warning event");
            }
            if (_dataCount != 13)
            {
                throw new RegressionTestException($"Unexpected data count {_dataCount}. Expected 13");
            }
            if (_receivedSecurityChangesEvent != 1)
            {
                throw new RegressionTestException($"Did not receive expected security changes removal! Got {_receivedSecurityChangesEvent}");
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
        public long DataPoints => 86;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-5.58%"},
            {"Compounding Annual Return", "-85.973%"},
            {"Drawdown", "5.600%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "94421.6"},
            {"Net Profit", "-5.578%"},
            {"Sharpe Ratio", "-5.495"},
            {"Sortino Ratio", "-10.306"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.585"},
            {"Beta", "-1.085"},
            {"Annual Standard Deviation", "0.15"},
            {"Annual Variance", "0.023"},
            {"Information Ratio", "-5.081"},
            {"Tracking Error", "0.206"},
            {"Treynor Ratio", "0.76"},
            {"Total Fees", "$36.70"},
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "18.33%"},
            {"OrderListHash", "1450ea23a3a1ef4ee2398ec757c39223"}
        };
    }
}
