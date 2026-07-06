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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a symbol can be used without the user having subscribed to it first.
    /// Lean auto-subscribes the symbol on the user's behalf when:
    ///  - registering an indicator or a consolidator for it (<see cref="_spy"/>), and
    ///  - submitting an order for it (<see cref="_aig"/>, see <see cref="QCAlgorithm.MarketOrder"/>).
    /// </summary>
    public class RegisterIndicatorAndConsolidatorWithoutSubscriptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // used without subscription to register an indicator and a consolidator
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        // used without subscription to submit an order, exercising the auto-add done by order submission
        private Symbol _aig = QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA);

        private SimpleMovingAverage _sma;
        private int _consolidatedBarCount;

        private OrderTicket _orderTicket;
        private bool _orderFilled;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            // Note: we never call AddEquity/AddSecurity. Registering an indicator or a consolidator
            // for a symbol that hasn't been subscribed to used to throw. It now auto-subscribes the symbol,
            // mirroring what order submission does.

            _sma = new SimpleMovingAverage(10);
            try
            {
                RegisterIndicator(_spy, _sma, Resolution.Minute);
            }
            catch (Exception ex) 
            { 
                throw new RegressionTestException($"Expected RegisterIndicator to auto-subscribe {_spy}, but it threw: {ex.Message}");
            }

            try
            {
                Consolidate(_spy, TimeSpan.FromMinutes(30), (TradeBar bar) => _consolidatedBarCount++);
            }
            catch (Exception ex)
            {
                throw new RegressionTestException($"Expected Consolidate to auto-subscribe {_spy}, but it threw: {ex.Message}");
            }

            if (!Securities.ContainsKey(_spy))
            {
                throw new RegressionTestException($"Expected {_spy} to have been automatically subscribed to after registering an indicator/consolidator for it.");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // AIG was never subscribed to: order submission will auto-subscribe it before placing the order
                _orderTicket = MarketOrder(_aig, 100);

                if (!Securities.ContainsKey(_aig))
                {
                    throw new RegressionTestException($"Expected {_aig} to have been automatically subscribed to after submitting an order for it.");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _orderFilled = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_sma.IsReady || _sma.Samples == 0)
            {
                throw new RegressionTestException($"Expected the SMA indicator to have received data through its auto-subscription, but Samples={_sma.Samples}.");
            }

            if (_consolidatedBarCount == 0)
            {
                throw new RegressionTestException("Expected the consolidator to have produced bars through its auto-subscription, but it produced none.");
            }

            if (_orderTicket == null)
            {
                throw new RegressionTestException("Expected an order to have been placed for the auto-subscribed symbol, but none was.");
            }

            if (_orderTicket.Status != OrderStatus.Filled || !_orderFilled)
            {
                throw new RegressionTestException($"Expected the order for {_aig} to have been filled, but its status was {_orderTicket.Status}.");
            }

            if (!Portfolio[_aig].Invested)
            {
                throw new RegressionTestException($"Expected to be invested in {_aig} after the order was filled.");
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
        public long DataPoints => 7842;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

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
            {"Compounding Annual Return", "6.467%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100080.15"},
            {"Net Profit", "0.080%"},
            {"Sharpe Ratio", "3.91"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "62.503%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.079"},
            {"Beta", "0.072"},
            {"Annual Standard Deviation", "0.016"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.261"},
            {"Tracking Error", "0.207"},
            {"Treynor Ratio", "0.871"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$11000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.83%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "5bb87da5d4faaf7c85a9e263890c3d64"}
        };
    }
}
