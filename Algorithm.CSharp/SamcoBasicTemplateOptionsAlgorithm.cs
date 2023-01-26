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

using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add options for a given underlying equity security. It also
    /// shows how you can prefilter contracts easily based on strikes and expirations, and how you
    /// can inspect the option chain to pick a specific option contract to trade.
    /// </summary>
    /// <meta name="tag" content="using data"/>
    /// <meta name="tag" content="options"/>
    /// <meta name="tag" content="filter selection"/>
    public class SamcoBasicTemplateOptionsAlgorithm : QCAlgorithm
    {
        private bool subscribed = false;
        private const string UnderlyingTicker = "NIFTY";
        public Symbol IndexSymbol;
        public Securities.Option.Option canonicalOption; 
        public IEnumerable<Symbol> filteredOptions;
        private HashSet<Symbol> _subscribedOptoinsContract= new();
        public override void Initialize()
        {
            SetTimeZone(TimeZones.Kolkata);
            SetBrokerageModel(BrokerageName.Samco, AccountType.Margin);
            SetAccountCurrency(Currencies.INR);

            var index = AddIndex(UnderlyingTicker,resolution:Resolution.Minute, market: Market.India);
            var option = AddIndexOption(index.Symbol, resolution: Resolution.Minute, market: Market.India);
            //OptionSymbol = option.Symbol;
            IndexSymbol = index.Symbol;
            //addOptions();
            
            // set our strike/expiry filter for this option chain
            /*option.SetFilter(u => u.Strikes(-2, +2)
                                                    //.Expiration(0, 180));
                                                    .Expiration(TimeSpan.Zero, TimeSpan.FromDays(180)));
            */
        }
        private void addOptions()
        {
            var contracts = OptionChainProvider.GetOptionContractList(IndexSymbol, Time);
            filteredOptions = (from symbol in contracts
                                   where ((symbol.ID.Date - Time).TotalDays < 6)
                                   select symbol);
            foreach (var contract in filteredOptions)
            {
                canonicalOption = AddIndexOptionContract(contract, Resolution.Minute);
                _subscribedOptoinsContract.Add(contract);
            }
        }
        private void removeOptions()
        {
            foreach(var contract in _subscribedOptoinsContract)
            {
                RemoveOptionContract(contract);
                
            }
            _subscribedOptoinsContract.Clear();
            subscribed = false;
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for
        /// receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            /*
            if (!subscribed) { addOptions(); //subscribed = true;
                                             }
            
            if (slice.ContainsKey(IndexSymbol))
            {
                var myData = slice.Bars[IndexSymbol];
                Log($"Nifty bar : {myData}");
            }
            
            if (!Portfolio.Invested )
            {
                OptionChain chain;
                if (slice.OptionChains.TryGetValue(canonicalOption.Symbol, out chain))
                {
                    // we find at the money (ATM) put contract with farthest expiration
                    var atmContract = filteredOptions
                        .OrderByDescending(x => x.ID.Date)
                        .ThenBy(x => Math.Abs(chain.Underlying.Price - x.ID.StrikePrice))
                        .ThenByDescending(x => x.ID.OptionRight)
                        .FirstOrDefault();

                    if (atmContract != null)
                    {
                        // if found, trade it
                        //MarketOrder(atmContract.Symbol, 1);
                        //MarketOnCloseOrder(atmContract.Symbol, -1);
                        var myData = slice.Bars[atmContract];
                        Log($"Nifty atm contract {atmContract.ID.Underlying.ToString()} {atmContract.ID.StrikePrice} {atmContract.ID.Date} {atmContract.ID.OptionRight} bar : {myData}");
                    }
                }
            }
            */
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to
        /// this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        /// <remarks>
        /// This method can be called asynchronously and so should only be used by seasoned C#
        /// experts. Ensure you use proper locks on thread-unsafe objects
        /// </remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean
        /// repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is
        /// written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are
        /// from running the algorithm
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
            {"Total Fees", "$2.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
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
            {"OrderListHash", "1130102123"}
        };
    }
}
