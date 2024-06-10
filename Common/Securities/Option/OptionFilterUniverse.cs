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
using QuantConnect.Logging;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents options symbols universe used in filtering.
    /// </summary>
    public class OptionFilterUniverse : ContractSecurityFilterUniverse<OptionFilterUniverse>
    {
        // Fields used in relative strikes filter
        private List<decimal> _uniqueStrikes;
        private bool _refreshUniqueStrikes;
        private DateTime _lastExchangeDate;
        private readonly decimal _underlyingScaleFactor = 1;

        /// <summary>
        /// The underlying price data
        /// </summary>
        protected BaseData UnderlyingInternal { get; set; }

        /// <summary>
        /// The underlying price data
        /// </summary>
        public BaseData Underlying
        {
            get
            {
                return UnderlyingInternal;
            }
        }

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        /// <param name="option">The canonical option chain security</param>
        public OptionFilterUniverse(Option.Option option)
        {
            _underlyingScaleFactor = option.SymbolProperties.StrikeMultiplier;
        }

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        /// <remarks>Used for testing only</remarks>
        public OptionFilterUniverse(IEnumerable<Symbol> allSymbols, BaseData underlying, decimal underlyingScaleFactor = 1)
            : base(allSymbols, underlying.EndTime)
        {
            UnderlyingInternal = underlying;
            _refreshUniqueStrikes = true;
            _underlyingScaleFactor = underlyingScaleFactor;
        }

        /// <summary>
        /// Refreshes this option filter universe and allows specifying if the exchange date changed from last call
        /// </summary>
        /// <param name="allSymbols">All the options contract symbols</param>
        /// <param name="underlying">The current underlying last data point</param>
        /// <param name="localTime">The current local time</param>
        public void Refresh(IEnumerable<Symbol> allSymbols, BaseData underlying, DateTime localTime)
        {
            base.Refresh(allSymbols, localTime);

            UnderlyingInternal = underlying;
            _refreshUniqueStrikes = _lastExchangeDate != localTime.Date;
            _lastExchangeDate = localTime.Date;
        }

        /// <summary>
        /// Determine if the given Option contract symbol is standard
        /// </summary>
        /// <returns>True if standard</returns>
        protected override bool IsStandard(Symbol symbol)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.FutureOption:
                    return FutureOptionSymbol.IsStandard(symbol);
                case SecurityType.IndexOption:
                    return IndexOptionSymbol.IsStandard(symbol);
                default:
                    return OptionSymbol.IsStandard(symbol);
            }
        }

        /// <summary>
        /// Applies filter selecting options contracts based on a range of strikes in relative terms
        /// </summary>
        /// <param name="minStrike">The minimum strike relative to the underlying price, for example, -1 would filter out contracts further than 1 strike below market price</param>
        /// <param name="maxStrike">The maximum strike relative to the underlying price, for example, +1 would filter out contracts further than 1 strike above market price</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Strikes(int minStrike, int maxStrike)
        {
            if (UnderlyingInternal == null)
            {
                return this;
            }

            if (_refreshUniqueStrikes || _uniqueStrikes == null)
            {
                // Each day we need to recompute the unique strikes list.
                _uniqueStrikes = AllSymbols.Select(x => x.ID.StrikePrice)
                    .Distinct()
                    .OrderBy(strikePrice => strikePrice)
                    .ToList();
                _refreshUniqueStrikes = false;
            }

            // find the current price in the list of strikes
            // When computing the strike prices we need to take into account
            // that some option's strike prices are based on a fraction of
            // the underlying. Thus we need to scale the underlying internal
            // price so that we can find it among the strike prices
            // using BinarySearch() method(as it is used below)
            var exactPriceFound = true;
            var index = _uniqueStrikes.BinarySearch(UnderlyingInternal.Price / _underlyingScaleFactor);

            // Return value of BinarySearch (from MSDN):
            // The zero-based index of item in the sorted List<T>, if item is found;
            // otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item
            // or, if there is no larger element, the bitwise complement of Count.
            if (index < 0)
            {
                // exact price not found
                exactPriceFound = false;

                if (index == ~_uniqueStrikes.Count)
                {
                    // there is no greater price, return empty
                    AllSymbols = Enumerable.Empty<Symbol>();
                    return this;
                }

                index = ~index;
            }

            // compute the bounds, no need to worry about rounding and such
            var indexMinPrice = index + minStrike;
            var indexMaxPrice = index + maxStrike;
            if (!exactPriceFound)
            {
                if (minStrike < 0 && maxStrike > 0)
                {
                    indexMaxPrice--;
                }
                else if (minStrike > 0)
                {
                    indexMinPrice--;
                    indexMaxPrice--;
                }
            }

            if (indexMinPrice < 0)
            {
                indexMinPrice = 0;
            }
            else if (indexMinPrice >= _uniqueStrikes.Count)
            {
                // price out of range: return empty
                AllSymbols = Enumerable.Empty<Symbol>();
                return this;
            }

            if (indexMaxPrice < 0)
            {
                // price out of range: return empty
                AllSymbols = Enumerable.Empty<Symbol>();
                return this;
            }
            if (indexMaxPrice >= _uniqueStrikes.Count)
            {
                indexMaxPrice = _uniqueStrikes.Count - 1;
            }

            var minPrice = _uniqueStrikes[indexMinPrice];
            var maxPrice = _uniqueStrikes[indexMaxPrice];

            AllSymbols = AllSymbols
                .Where(symbol =>
                    {
                        var price = symbol.ID.StrikePrice;
                        return price >= minPrice && price <= maxPrice;
                    }
                ).ToList();

            return this;
        }

        /// <summary>
        /// Sets universe of call options (if any) as a selection
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallsOnly()
        {
            return Contracts(contracts => contracts.Where(x => x.ID.OptionRight == OptionRight.Call));
        }

        /// <summary>
        /// Sets universe of put options (if any) as a selection
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutsOnly()
        {
            return Contracts(contracts => contracts.Where(x => x.ID.OptionRight == OptionRight.Put));
        }

        /// <summary>
        /// Sets universe of a single call contract with the closest match to criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire days till expiry from the current time</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Naked Call, Covered Call, and Protective Call Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse SingleCall(int daysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return SingleContract(OptionRight.Call, daysTillExpiry, strikeFromAtm);
        }

        /// <summary>
        /// Sets universe of a single put contract with the closest match to criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire days till expiry from the current time</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Naked Put, Covered Put, and Protective Put Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse SinglePut(int daysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return SingleContract(OptionRight.Put, daysTillExpiry, strikeFromAtm);
        }

        private OptionFilterUniverse SingleContract(OptionRight right, int daysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            // Select the expiry as the nearest to set days later
            var expiry = AllSymbols.OrderBy(x => Math.Abs((x.ID.Date - _lastExchangeDate.AddDays(daysTillExpiry)).Days))
                .First().ID.Date;
            var contracts = Contracts(contract => contract.Where(x => x.ID.Date == expiry && x.ID.OptionRight == right));
            // Select strike price
            var selected = contracts.OrderBy(x => Math.Abs(x.ID.StrikePrice - Underlying.Price - strikeFromAtm)).First();

            return contracts.WhereContains(new List<Symbol> { selected });
        }

        /// <summary>
        /// Sets universe of 2 call contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire days till expiry from the current time</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Call Spread and Bull Call Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallSpread(int daysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Spread(OptionRight.Call, daysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
        }

        /// <summary>
        /// Sets universe of 2 put contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire days till expiry from the current time</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Put Spread and Bull Put Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutSpread(int daysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Spread(OptionRight.Put, daysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
        }

        private OptionFilterUniverse Spread(OptionRight right, int daysTillExpiry, decimal higherStrikeFromAtm, decimal? lowerStrikeFromAtm = null)
        {
            if (!lowerStrikeFromAtm.HasValue)
            {
                lowerStrikeFromAtm = -higherStrikeFromAtm;
            }

            if (higherStrikeFromAtm <= lowerStrikeFromAtm)
            {
                throw new ArgumentException("Spread(): strike price arguments must be in descending order, ",
                    $"{nameof(higherStrikeFromAtm)}, {nameof(lowerStrikeFromAtm)}");
            }

            // Select the expiry as the nearest to set days later
            var expiry = AllSymbols.OrderBy(x => Math.Abs((x.ID.Date - _lastExchangeDate.AddDays(daysTillExpiry)).Days))
                .First().ID.Date;
            var contracts = Contracts(contract => contract.Where(x => x.ID.Date == expiry && x.ID.OptionRight == right));
            
            // Select the strike prices with the set spread range
            var higherStrikeContract = contracts.OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + higherStrikeFromAtm)).First();
            var lowerStrikeContract = contracts.OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + (decimal)lowerStrikeFromAtm)).First();

            return contracts.WhereContains(new List<Symbol> { higherStrikeContract, lowerStrikeContract });
        }

        /// <summary>
        /// Sets universe of 2 call contracts with the same strike price and different expiration dates, with closest match to the criteria given
        /// </summary>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <param name="nearDaysTillExpiry">The desire days till expiry of the closer contract from the current time</param>
        /// <param name="farDaysTillExpiry">The desire days till expiry of the further conrtact from the current time</param>
        /// <remarks>Applicable to Long and Short Call Calendar Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallCalendarSpread(decimal strikeFromAtm = 0, int nearDaysTillExpiry = 30, int farDaysTillExpiry = 60)
        {
            return CalendarSpread(OptionRight.Call, strikeFromAtm, nearDaysTillExpiry, farDaysTillExpiry);
        }

        /// <summary>
        /// Sets universe of 2 put contracts with the same strike price and different expiration dates, with closest match to the criteria given
        /// </summary>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <param name="nearDaysTillExpiry">The desire days till expiry of the closer contract from the current time</param>
        /// <param name="farDaysTillExpiry">The desire days till expiry of the further conrtact from the current time</param>
        /// <remarks>Applicable to Long and Short Put Calendar Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutCalendarSpread(decimal strikeFromAtm = 0, int nearDaysTillExpiry = 30, int farDaysTillExpiry = 60)
        {
            return CalendarSpread(OptionRight.Put, strikeFromAtm, nearDaysTillExpiry, farDaysTillExpiry);
        }

        private OptionFilterUniverse CalendarSpread(OptionRight right, decimal strikeFromAtm, int nearDaysTillExpiry, int farDaysTillExpiry)
        {
            if (farDaysTillExpiry <= nearDaysTillExpiry)
            {
                throw new ArgumentException("CalendarSpread(): expiry arguments must be in ascending order, ",
                    $"{nameof(nearDaysTillExpiry)}, {nameof(farDaysTillExpiry)}");
            }

            if (nearDaysTillExpiry < 0)
            {
                throw new ArgumentException("CalendarSpread(): near expiry argument must be positive.");
            }
            
            // Select the set strike
            var strike = AllSymbols.OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + strikeFromAtm))
                .First().ID.StrikePrice;
            var contracts = AllSymbols.Where(x => x.ID.StrikePrice == strike && x.ID.OptionRight == right);
            
            // Select the expiries
            var nearExpiryContract = contracts.OrderBy(x => Math.Abs((_lastExchangeDate.AddDays(nearDaysTillExpiry) - x.ID.Date).Days)).First();
            var furtherContracts = contracts.Where(x => x.ID.Date > nearExpiryContract.ID.Date).ToList();
            
            if (furtherContracts.Count == 0)
            {
                return this.WhereContains( new List<Symbol>() );
            }

            var farExpiryContract = furtherContracts.OrderBy(x => Math.Abs((_lastExchangeDate.AddDays(farDaysTillExpiry) - x.ID.Date).Days)).First();

            return this.WhereContains(new List<Symbol> { nearExpiryContract, farExpiryContract });
        }

        /// <summary>
        /// Sets universe of an OTM call contract and an OTM put contract with the same expiry, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire strike price distance from the current underlying price</param>
        /// <param name="callStrikeFromAtm">The desire strike price distance from the current underlying price of the OTM call. It must be positive.</param>
        /// <param name="putStrikeFromAtm">The desire strike price distance from the current underlying price of the OTM put. It must be negative.</param>
        /// <remarks>Applicable to Long and Short Strangle Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Strangle(int daysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            if (callStrikeFromAtm < 0)
            {
                throw new ArgumentException($"Strangle(): {nameof(callStrikeFromAtm)} must be positive");
            }

            if (putStrikeFromAtm > 0)
            {
                throw new ArgumentException($"Strangle(): {nameof(putStrikeFromAtm)} must be negative");
            }

            return CallPutSpread(daysTillExpiry, callStrikeFromAtm, putStrikeFromAtm);
        }

        /// <summary>
        /// Sets universe of a an ATM call contract and an ATM put contract with the same expiry, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Straddle Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Straddle(int daysTillExpiry = 30)
        {
            return CallPutSpread(daysTillExpiry, 0, 0);
        }

        /// <summary>
        /// Sets universe of an call contract and an put contract with the same expiry but lower strike price, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire strike price distance from the current underlying price</param>
        /// <param name="callStrikeFromAtm">The desire strike price distance from the current underlying price of the call.</param>
        /// <param name="putStrikeFromAtm">The desire strike price distance from the current underlying price of the put.</param>
        /// <remarks>Applicable to Protective Collar Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse ProtectiveCollar(int daysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            if (callStrikeFromAtm <= putStrikeFromAtm)
            {
                throw new ArgumentException("ProtectiveCollar(): strike price arguments must be in descending order, ",
                    $"{nameof(callStrikeFromAtm)}, {nameof(putStrikeFromAtm)}");
            }

            return CallPutSpread(daysTillExpiry, callStrikeFromAtm, putStrikeFromAtm);
        }

        /// <summary>
        /// Sets universe of a an ATM call contract and an ATM put contract with the same expiry, with closest match to the criteria given
        /// </summary>
        /// <param name="daysTillExpiry">The desire strike price distance from the current underlying price</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Conversion and Reverse Conversion Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Conversion(int daysTillExpiry = 30, decimal strikeFromAtm = 5)
        {
            return CallPutSpread(daysTillExpiry, strikeFromAtm, strikeFromAtm);
        }

        private OptionFilterUniverse CallPutSpread(int daysTillExpiry, decimal callStrikeFromAtm, decimal putStrikeFromAtm)
        {
            // Select the expiry as the nearest to set days later
            var expiry = AllSymbols.OrderBy(x => Math.Abs((x.ID.Date - _lastExchangeDate.AddDays(daysTillExpiry)).Days))
                .First().ID.Date;
            var contracts = AllSymbols.Where(x => x.ID.Date == expiry);

            // Select the strike prices with the set spread range
            var call = contracts.Where(x => x.ID.OptionRight == OptionRight.Call)
                .OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + callStrikeFromAtm))
                .FirstOrDefault();
            var put = contracts.Where(x => x.ID.OptionRight == OptionRight.Put)
                .OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + putStrikeFromAtm))
                .FirstOrDefault();

            // Select the contracts
            return this.WhereContains( new List<Symbol> { call, put } );
        }
    }

    /// <summary>
    /// Extensions for Linq support
    /// </summary>
    public static class OptionFilterUniverseEx
    {
        /// <summary>
        /// Filters universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="predicate">Bool function to determine which Symbol are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse Where(this OptionFilterUniverse universe, Func<Symbol, bool> predicate)
        {
            universe.AllSymbols = universe.AllSymbols.Where(predicate).ToList();
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse Select(this OptionFilterUniverse universe, Func<Symbol, Symbol> mapFunc)
        {
            universe.AllSymbols = universe.AllSymbols.Select(mapFunc).ToList();
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse SelectMany(this OptionFilterUniverse universe, Func<Symbol, IEnumerable<Symbol>> mapFunc)
        {
            universe.AllSymbols = universe.AllSymbols.SelectMany(mapFunc).ToList();
            return universe;
        }

        /// <summary>
        /// Updates universe to only contain the symbols in the list
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="filterList">List of Symbols to keep in the Universe</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse WhereContains(this OptionFilterUniverse universe, List<Symbol> filterList)
        {
            universe.AllSymbols = universe.AllSymbols.Where(filterList.Contains).ToList();
            return universe;
        }
    }
}
