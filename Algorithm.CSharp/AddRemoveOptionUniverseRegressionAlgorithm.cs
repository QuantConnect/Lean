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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;
using QuantConnect.Interfaces;

// ReSharper disable InvokeAsExtensionMethod -- .net 4.7.2 added ToHashSet and it looks like our version of mono has it as well causing ambiguity in the cloud

namespace QuantConnect.Algorithm.CSharp
{
    public class AddRemoveOptionUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionChainSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private readonly HashSet<Symbol> _expectedSecurities = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _expectedData = new HashSet<Symbol>();
        private readonly HashSet<Symbol> _expectedUniverses = new HashSet<Symbol>();
        private bool _expectUniverseSubscription;

        // order of expected contract additions as price moves
        private int _expectedContractIndex;
        private readonly List<Symbol> _expectedContracts = new List<Symbol>
        {
            SymbolRepresentation.ParseOptionTickerOSI("GOOG  151224P00747500"),
            SymbolRepresentation.ParseOptionTickerOSI("GOOG  151224P00750000"),
            SymbolRepresentation.ParseOptionTickerOSI("GOOG  151224P00752500")
        };

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            var goog = AddEquity(UnderlyingTicker);

            // expect GOOG equity
            _expectedData.Add(goog.Symbol);
            _expectedSecurities.Add(goog.Symbol);
            // expect user defined universe holding GOOG equity
            _expectedUniverses.Add(UserDefinedUniverse.CreateSymbol(SecurityType.Equity, Market.USA));
        }

        public override void OnData(Slice data)
        {
            // verify expectations
            if (SubscriptionManager.Subscriptions.Count(x => x.Symbol == OptionChainSymbol)
                != (_expectUniverseSubscription ? 1 : 0))
            {
                Log($"SubscriptionManager.Subscriptions:  {string.Join(" -- ", SubscriptionManager.Subscriptions)}");
                throw new Exception($"Unexpected {OptionChainSymbol} subscription presence");
            }
            if (!data.ContainsKey(Underlying))
            {
                // TODO : In fact, we're unable to properly detect whether or not we auto-added or it was manually added
                // this is because when we auto-add the underlying we don't mark it as an internal security like we do with other auto adds
                // so there's currently no good way to remove the underlying equity without invoking RemoveSecurity(underlying) manually
                // from the algorithm, otherwise we may remove it incorrectly. Now, we could track MORE state, but it would likely be a duplication
                // of the internal flag's purpose, so kicking this issue for now with a big fat note here about it :) to be considerd for any future
                // refactorings of how we manage subscription/security data and track various aspects about the security (thinking a flags enum with
                // things like manually added, auto added, internal, and any other boolean state we need to track against a single security)
                throw new Exception("The underlying equity data should NEVER be removed in this algorithm because it was manually added");
            }
            if (_expectedSecurities.AreDifferent(Securities.Total.Select(x => x.Symbol).ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, _expectedSecurities.OrderBy(s => s.ToString()));
                var actual = string.Join(Environment.NewLine, Securities.Keys.OrderBy(s => s.ToString()));
                throw new Exception($"{Time}:: Detected differences in expected and actual securities{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
            }
            if (_expectedUniverses.AreDifferent(UniverseManager.Keys.ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, _expectedUniverses.OrderBy(s => s.ToString()));
                var actual = string.Join(Environment.NewLine, UniverseManager.Keys.OrderBy(s => s.ToString()));
                throw new Exception($"{Time}:: Detected differences in expected and actual universes{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
            }
            if (_expectedData.AreDifferent(data.Keys.ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, _expectedData.OrderBy(s => s.ToString()));
                var actual = string.Join(Environment.NewLine, data.Keys.OrderBy(s => s.ToString()));
                throw new Exception($"{Time}:: Detected differences in expected and actual slice data keys{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
            }

            // 10AM add GOOG option chain
            if (Time.TimeOfDay.Hours == 10 && Time.TimeOfDay.Minutes == 0)
            {
                if (Securities.ContainsKey(OptionChainSymbol))
                {
                    throw new Exception("The option chain security should not have been added yet");
                }

                var googOptionChain = AddOption(UnderlyingTicker);
                googOptionChain.SetFilter(u =>
                {
                    // we added the universe at 10, the universe selection data should not be from before
                    if (u.Underlying.EndTime.Hour < 10)
                    {
                        throw new Exception($"Unexpected underlying data point {u.Underlying.EndTime} {u.Underlying}");
                    }
                    // find first put above market price
                    return u.IncludeWeeklys()
                        .Strikes(+1, +3)
                        .Expiration(TimeSpan.Zero, TimeSpan.FromDays(1))
                        .Contracts(c => c.Where(s => s.ID.OptionRight == OptionRight.Put));
                });

                _expectedSecurities.Add(OptionChainSymbol);
                _expectedUniverses.Add(OptionChainSymbol);
                _expectUniverseSubscription = true;
            }

            // 11:30AM remove GOOG option chain
            if (Time.TimeOfDay.Hours == 11 && Time.TimeOfDay.Minutes == 30)
            {
                RemoveSecurity(OptionChainSymbol);
                // remove contracts from expected data
                _expectedData.RemoveWhere(s => _expectedContracts.Contains(s));
                // remove option chain universe from expected universes
                _expectedUniverses.Remove(OptionChainSymbol);
                // OptionChainSymbol universe subscription should not be present
                _expectUniverseSubscription = false;
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Any())
            {
                foreach (var added in changes.AddedSecurities)
                {
                    // any option security additions for this algorithm should match the expected contracts
                    if (added.Symbol.SecurityType == SecurityType.Option)
                    {
                        var expectedContract = _expectedContracts[_expectedContractIndex];
                        if (added.Symbol != expectedContract)
                        {
                            throw new Exception($"Expected option contract {expectedContract} to be added but received {added.Symbol}");
                        }

                        _expectedContractIndex++;

                        // purchase for regression statistics
                        MarketOrder(added.Symbol, 1);
                    }

                    _expectedData.Add(added.Symbol);
                    _expectedSecurities.Add(added.Symbol);
                }
            }

            // security removal happens exactly once in this algorithm when the option chain is removed
            // and all child subscriptions (option contracts) should be removed at the same time
            if (changes.RemovedSecurities.Any(x => x.Symbol.SecurityType == SecurityType.Option))
            {
                // receive removed event next timestep at 11:31AM
                if (Time.TimeOfDay.Hours != 11 || Time.TimeOfDay.Minutes != 31)
                {
                    throw new Exception($"Expected option contracts to be removed at 11:31AM, instead removed at: {Time}");
                }

                if (changes.RemovedSecurities
                    .Where(x => x.Symbol.SecurityType == SecurityType.Option)
                    .ToHashSet(s => s.Symbol)
                    .AreDifferent(_expectedContracts.ToHashSet()))
                {
                    throw new Exception("Expected removed securities to equal expected contracts added");
                }
            }

            if (Securities.ContainsKey(Underlying))
            {
                Console.WriteLine($"{Time:o}:: PRICE:: {Securities[Underlying].Price} CHANGES:: {changes}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 200807;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99079"},
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
            {"Total Fees", "$6.00"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "GOOCV 305RBR0BSWIX2|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.49%"},
            {"OrderListHash", "bd115ec8bb7734b1561d6a6cc6c00039"}
        };
    }
}
