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
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Tests.Common.Securities.Options.StrategyMatcher
{
    /// <summary>
    /// Provides array-indexer calling conventions for easily creating option contract symbols.
    /// I suspect I'll update this later to fulfill the original vision of being a full option
    /// chain, where indexing is successively applied, such as Puts[100m] would return a dictionary
    /// keyed by expiration of all puts@100. To pull a specific one, Puts[100m][expiration] or Puts[100m][1]
    /// using the weeks notation used in the indexers in this class.
    /// </summary>
    public static class Option
    {
        public static readonly Symbol Underlying = Symbols.SPY;
        public static readonly DateTime ReferenceDate = new DateTime(2020, 10, 16);

        public const decimal ContractMultiplier = 100m;
        public static Factory Contract { get; } = new Factory();
        public static FactoryRight Put { get; } = new FactoryRight(OptionRight.Put);
        public static FactoryRight Call { get; } = new FactoryRight(OptionRight.Call);

        public class Factory
        {
            public Symbol this[Symbol underlying, OptionRight right, decimal strike, DateTime expiration]
                => Symbol.CreateOption(underlying, underlying.ID.Market, OptionStyle.American, right, strike, expiration);

            public Symbol this[OptionRight right, decimal strike, DateTime expiration]
                => Symbol.CreateOption(Underlying, Market.USA, OptionStyle.American, right, strike, expiration);

            public Symbol this[Symbol underlying, OptionRight right, decimal strike, int weeks = 0]
                => Symbol.CreateOption(underlying, underlying.ID.Market, OptionStyle.American, right, strike, ReferenceDate.AddDays(7 * weeks));

            public Symbol this[OptionRight right, decimal strike, int weeks = 0]
                => Symbol.CreateOption(Underlying, Market.USA, OptionStyle.American, right, strike, ReferenceDate.AddDays(7 * weeks));
        }

        public class FactoryRight
        {
            private readonly OptionRight right;

            public FactoryRight(OptionRight right)
            {
                this.right = right;
            }

            public Symbol this[Symbol underlying, decimal strike, DateTime expiration]
                => Symbol.CreateOption(underlying, underlying.ID.Market, OptionStyle.American, right, strike, expiration);

            public Symbol this[decimal strike, DateTime expiration]
                => Symbol.CreateOption(Symbols.SPY, Market.USA, OptionStyle.American, right, strike, expiration);

            public Symbol this[Symbol underlying, decimal strike, int weeks = 0]
                => Symbol.CreateOption(underlying, underlying.ID.Market, OptionStyle.American, right, strike, ReferenceDate.AddDays(7 * weeks));

            public Symbol this[decimal strike, int weeks = 0]
                => Symbol.CreateOption(Symbols.SPY, Market.USA, OptionStyle.American, right, strike, ReferenceDate.AddDays(7 * weeks));
        }

        public static Symbol WithStrike(this Symbol symbol, decimal strike)
        {
            return Symbol.CreateOption(symbol.Underlying, symbol.ID.Market, symbol.ID.OptionStyle, symbol.ID.OptionRight, strike, symbol.ID.Date);
        }

        public static Symbol WithExpiration(this Symbol symbol, DateTime expiration)
        {
            return Symbol.CreateOption(symbol.Underlying, symbol.ID.Market, symbol.ID.OptionStyle, symbol.ID.OptionRight, symbol.ID.StrikePrice, expiration);
        }

        public static Symbol WithRight(this Symbol symbol, OptionRight right)
        {
            return Symbol.CreateOption(symbol.Underlying, symbol.ID.Market, symbol.ID.OptionStyle, right, symbol.ID.StrikePrice, symbol.ID.Date);
        }

        public static Symbol WithUnderlying(this Symbol symbol, Symbol underlying)
        {
            return Symbol.CreateOption(underlying, symbol.ID.Market, symbol.ID.OptionStyle, symbol.ID.OptionRight, symbol.ID.StrikePrice, symbol.ID.Date);
        }

        public static OptionPosition Position(Symbol symbol, int quantity = +1)
        {
            return new OptionPosition(symbol, quantity);
        }
    }
}
