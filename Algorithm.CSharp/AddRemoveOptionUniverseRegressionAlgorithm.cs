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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    public class AddRemoveOptionUniverseRegressionAlgorithm : QCAlgorithm
    {
        private const string UnderlyingTicker = "GOOG";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionChainSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private HashSet<Symbol> ExpectedSecurities = new HashSet<Symbol>();
        private HashSet<Symbol> ExpectedData = new HashSet<Symbol>();
        private HashSet<Symbol> ExpectedUniverses = new HashSet<Symbol>();

        // order of expected contract additions as price moves
        private int expectedContractIndex;
        private List<Symbol> ExpectedContracts = new List<Symbol>
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
            ExpectedData.Add(goog.Symbol);
            ExpectedSecurities.Add(goog.Symbol);
            // expect user defined universe holding GOOG equity
            ExpectedUniverses.Add(UserDefinedUniverse.CreateSymbol(SecurityType.Equity, Market.USA));
        }

        public override void OnData(Slice data)
        {
            // verify expectations
            if (!data.ContainsKey(Underlying))
            {
                // TODO : In fact, we're unable to properly detect whether or not we auto-added or it was manually added
                // this is because when we auto-add the underlying we don't mark it as an internal security like we do with other auto adds
                // so there's currently no good way to remove the underlying equity without invoking RemoveSecurity(underlying) manually
                // from the algorithm, otherwise we may remove it incorrectly. Now, we could track MORE state, but it would likely be a duplication
                // of the internal flag's purpose, so kicking this issue for now with a big fat note here about it :) to be considerd for any future
                // refactorings of how we manay subscription/security data and track various aspects about the security (thinking a flags enum with
                // things like manually added, auto added, and any other boolean state we need to track against a single security)
                throw new Exception("The underlying equity data should NEVER be removed in this algorithm because it was manually added");
            }
            if (ExpectedSecurities.AreDifferent(Securities.Keys.ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, ExpectedSecurities.OrderBy(s => s.ToString()));
                var actual = string.Join(Environment.NewLine, Securities.Keys.OrderBy(s => s.ToString()));
                throw new Exception($"{Time}:: Detected differences in expected and actual securities{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
            }
            if (ExpectedUniverses.AreDifferent(UniverseManager.Keys.ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, ExpectedUniverses.OrderBy(s => s.ToString()));
                var actual = string.Join(Environment.NewLine, UniverseManager.Keys.OrderBy(s => s.ToString()));
                throw new Exception($"{Time}:: Detected differences in expected and actual universes{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
            }
            if (ExpectedData.AreDifferent(data.Keys.ToHashSet()))
            {
                var expected = string.Join(Environment.NewLine, ExpectedData.OrderBy(s => s.ToString()));
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
                    // find first put above market price
                    return u.IncludeWeeklys()
                        .Strikes(+1, +1)
                        .Expiration(TimeSpan.Zero, TimeSpan.FromDays(1))
                        .Contracts(c => c.Where(s => s.ID.OptionRight == OptionRight.Put));
                });

                ExpectedSecurities.Add(OptionChainSymbol);
                ExpectedUniverses.Add(OptionChainSymbol);
            }

            // 11:30AM remove GOOG option chain
            if (Time.TimeOfDay.Hours == 11 && Time.TimeOfDay.Minutes == 30)
            {
                RemoveSecurity(OptionChainSymbol);
                // remove contracts from expected data
                ExpectedData.RemoveWhere(s => ExpectedContracts.Contains(s));
                // remove option chain universe from expected universes
                ExpectedUniverses.Remove(OptionChainSymbol);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Count > 1)
            {
                throw new Exception(
                    $"This algorithm intends to add a single security at a time but added: {changes.AddedSecurities.Count}");
            }

            // any security additions for this algorithm should match the expected contracts
            if (changes.AddedSecurities.Any())
            {
                var added = changes.AddedSecurities[0];
                if (added.Symbol.SecurityType == SecurityType.Option)
                {
                    var expectedContract = ExpectedContracts[expectedContractIndex];
                    if (added.Symbol != expectedContract)
                    {
                        throw new Exception(
                            $"Expected option contract {expectedContract} to be added but received {added.Symbol}");
                    }

                    expectedContractIndex++;
                }

                // purchase for regression statistics
                MarketOrder(added.Symbol, 1);

                ExpectedData.Add(added.Symbol);
                ExpectedSecurities.Add(added.Symbol);
            }

            // security removal happens exactly once in this algorithm when the option chain is removed
            // and all child subscriptions (option contracts) should be removed at the same time
            if (changes.RemovedSecurities.Any())
            {
                // receive removed event next timestep at 11:31AM
                if (Time.TimeOfDay.Hours != 11 || Time.TimeOfDay.Minutes != 31)
                {
                    throw new Exception($"Expected option contracts to be removed at 11:31AM, instead removed at: {Time}");
                }

                if (changes.RemovedSecurities.ToHashSet(s => s.Symbol).AreDifferent(ExpectedContracts.ToHashSet()))
                {
                    throw new Exception("Expected removed securities to equal expected contracts added");
                }
            }

            Console.WriteLine($"{Time:o}:: PRICE:: {Securities["GOOG"].Price} CHANGES:: {changes}");
        }
    }
}