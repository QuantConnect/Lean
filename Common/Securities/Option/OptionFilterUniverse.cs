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
using System.Runtime.CompilerServices;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents options symbols universe used in filtering.
    /// </summary>
    public class OptionFilterUniverse : ContractSecurityFilterUniverse<OptionFilterUniverse, OptionUniverse>
    {
        private Option.Option _option;

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
            _option = option;
            _underlyingScaleFactor = option.SymbolProperties.StrikeMultiplier;
        }

        /// <summary>
        /// Constructs OptionFilterUniverse
        /// </summary>
        /// <remarks>Used for testing only</remarks>
        public OptionFilterUniverse(Option.Option option, IEnumerable<OptionUniverse> allData, BaseData underlying, decimal underlyingScaleFactor = 1)
            : base(allData, underlying.EndTime)
        {
            _option = option;
            UnderlyingInternal = underlying;
            _refreshUniqueStrikes = true;
            _underlyingScaleFactor = underlyingScaleFactor;
        }

        /// <summary>
        /// Refreshes this option filter universe and allows specifying if the exchange date changed from last call
        /// </summary>
        /// <param name="allContractsData">All data for the option contracts</param>
        /// <param name="underlying">The current underlying last data point</param>
        /// <param name="localTime">The current local time</param>
        public void Refresh(IEnumerable<OptionUniverse> allContractsData, BaseData underlying, DateTime localTime)
        {
            base.Refresh(allContractsData, localTime);

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
        /// Creates a new instance of the data type for the given symbol
        /// </summary>
        /// <returns>A data instance for the given symbol</returns>
        protected override OptionUniverse CreateDataInstance(Symbol symbol)
        {
            return new OptionUniverse()
            {
                Symbol = symbol,
                Time = LocalTime
            };
        }

        /// <summary>
        /// Adjusts the date to the next trading day if the current date is not a trading day, so that expiration filter is properly applied.
        /// e.g. Selection for Mondays happen on Friday midnight (Saturday start), so if the minimum time to expiration is, say 0,
        /// contracts expiring on Monday would be filtered out if the date is not properly adjusted to the next trading day (Monday).
        /// </summary>
        /// <param name="referenceDate">The date to be adjusted</param>
        /// <returns>The adjusted date</returns>
        protected override DateTime AdjustExpirationReferenceDate(DateTime referenceDate)
        {
            // Check whether the reference time is a tradable date:
            if (!_option.Exchange.Hours.IsDateOpen(referenceDate))
            {
                referenceDate = _option.Exchange.Hours.GetNextTradingDay(referenceDate);
            }

            return referenceDate;
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
                    return Empty();
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
                return Empty();
            }

            if (indexMaxPrice < 0)
            {
                // price out of range: return empty
                return Empty();
            }
            if (indexMaxPrice >= _uniqueStrikes.Count)
            {
                indexMaxPrice = _uniqueStrikes.Count - 1;
            }

            var minPrice = _uniqueStrikes[indexMinPrice];
            var maxPrice = _uniqueStrikes[indexMaxPrice];

            Data = Data
                .Where(data =>
                    {
                        var price = data.ID.StrikePrice;
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
            return Contracts(contracts => contracts.Where(x => x.Symbol.ID.OptionRight == OptionRight.Call));
        }

        /// <summary>
        /// Sets universe of put options (if any) as a selection
        /// </summary>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutsOnly()
        {
            return Contracts(contracts => contracts.Where(x => x.Symbol.ID.OptionRight == OptionRight.Put));
        }

        /// <summary>
        /// Sets universe of a single call contract with the closest match to criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Naked Call, Covered Call, and Protective Call Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse NakedCall(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return SingleContract(OptionRight.Call, minDaysTillExpiry, strikeFromAtm);
        }

        /// <summary>
        /// Sets universe of a single put contract with the closest match to criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Naked Put, Covered Put, and Protective Put Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse NakedPut(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return SingleContract(OptionRight.Put, minDaysTillExpiry, strikeFromAtm);
        }

        private OptionFilterUniverse SingleContract(OptionRight right, int minDaysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            // Select the expiry as the nearest to set days later
            var contractsForExpiry = GetContractsForExpiry(AllSymbols, minDaysTillExpiry);
            var contracts = contractsForExpiry.Where(x => x.ID.OptionRight == right).ToList();
            if (contracts.Count == 0)
            {
                return Empty();
            }

            // Select strike price
            var strike = GetStrike(contracts, strikeFromAtm);
            var selected = contracts.Single(x => x.ID.StrikePrice == strike);

            return SymbolList(new List<Symbol> { selected });
        }

        /// <summary>
        /// Sets universe of 2 call contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Call Spread and Bull Call Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Spread(OptionRight.Call, minDaysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
        }

        /// <summary>
        /// Sets universe of 2 put contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Put Spread and Bull Put Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Spread(OptionRight.Put, minDaysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm);
        }

        private OptionFilterUniverse Spread(OptionRight right, int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal? lowerStrikeFromAtm = null)
        {
            if (!lowerStrikeFromAtm.HasValue)
            {
                lowerStrikeFromAtm = -higherStrikeFromAtm;
            }

            if (higherStrikeFromAtm <= lowerStrikeFromAtm)
            {
                throw new ArgumentException("Spread(): strike price arguments must be in descending order, "
                    + $"{nameof(higherStrikeFromAtm)}, {nameof(lowerStrikeFromAtm)}");
            }

            // Select the expiry as the nearest to set days later
            var contractsForExpiry = GetContractsForExpiry(AllSymbols, minDaysTillExpiry);
            var contracts = contractsForExpiry.Where(x => x.ID.OptionRight == right).ToList();
            if (contracts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var lowerStrike = GetStrike(contracts, (decimal)lowerStrikeFromAtm);
            var lowerStrikeContract = contracts.Single(x => x.ID.StrikePrice == lowerStrike);
            var higherStrikeContracts = contracts.Where(x => x.ID.StrikePrice > lowerStrike).ToList();
            if (higherStrikeContracts.Count == 0)
            {
                return Empty();
            }

            var higherStrike = GetStrike(higherStrikeContracts, higherStrikeFromAtm);
            var higherStrikeContract = higherStrikeContracts.Single(x => x.ID.StrikePrice == higherStrike);

            return SymbolList(new List<Symbol> { lowerStrikeContract, higherStrikeContract });
        }

        /// <summary>
        /// Sets universe of 2 call contracts with the same strike price and different expiration dates, with closest match to the criteria given
        /// </summary>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The mininum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The mininum days till expiry of the further conrtact from the current time, closest expiry will be selected</param>
        /// <remarks>Applicable to Long and Short Call Calendar Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            return CalendarSpread(OptionRight.Call, strikeFromAtm, minNearDaysTillExpiry, minFarDaysTillExpiry);
        }

        /// <summary>
        /// Sets universe of 2 put contracts with the same strike price and different expiration dates, with closest match to the criteria given
        /// </summary>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The mininum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The mininum days till expiry of the further conrtact from the current time, closest expiry will be selected</param>
        /// <remarks>Applicable to Long and Short Put Calendar Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            return CalendarSpread(OptionRight.Put, strikeFromAtm, minNearDaysTillExpiry, minFarDaysTillExpiry);
        }

        private OptionFilterUniverse CalendarSpread(OptionRight right, decimal strikeFromAtm, int minNearDaysTillExpiry, int minFarDaysTillExpiry)
        {
            if (minFarDaysTillExpiry <= minNearDaysTillExpiry)
            {
                throw new ArgumentException("CalendarSpread(): expiry arguments must be in ascending order, "
                    + $"{nameof(minNearDaysTillExpiry)}, {nameof(minFarDaysTillExpiry)}");
            }

            if (minNearDaysTillExpiry < 0)
            {
                throw new ArgumentException("CalendarSpread(): near expiry argument must be positive.");
            }

            // Select the set strike
            var strike = GetStrike(AllSymbols, strikeFromAtm);
            var contracts = AllSymbols.Where(x => x.ID.StrikePrice == strike && x.ID.OptionRight == right).ToList();

            // Select the expiries
            var nearExpiryContract = GetContractsForExpiry(contracts, minNearDaysTillExpiry).SingleOrDefault();
            if (nearExpiryContract == null)
            {
                return Empty();
            }

            var furtherContracts = contracts.Where(x => x.ID.Date > nearExpiryContract.ID.Date).ToList();
            var farExpiryContract = GetContractsForExpiry(furtherContracts, minFarDaysTillExpiry).SingleOrDefault();
            if (farExpiryContract == null)
            {
                return Empty();
            }

            return SymbolList(new List<Symbol> { nearExpiryContract, farExpiryContract });
        }

        /// <summary>
        /// Sets universe of an OTM call contract and an OTM put contract with the same expiry, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="callStrikeFromAtm">The desire strike price distance from the current underlying price of the OTM call. It must be positive.</param>
        /// <param name="putStrikeFromAtm">The desire strike price distance from the current underlying price of the OTM put. It must be negative.</param>
        /// <remarks>Applicable to Long and Short Strangle Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Strangle(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            if (callStrikeFromAtm <= 0)
            {
                throw new ArgumentException($"Strangle(): {nameof(callStrikeFromAtm)} must be positive");
            }

            if (putStrikeFromAtm >= 0)
            {
                throw new ArgumentException($"Strangle(): {nameof(putStrikeFromAtm)} must be negative");
            }

            return CallPutSpread(minDaysTillExpiry, callStrikeFromAtm, putStrikeFromAtm, true);
        }

        /// <summary>
        /// Sets universe of an ATM call contract and an ATM put contract with the same expiry, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <remarks>Applicable to Long and Short Straddle Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Straddle(int minDaysTillExpiry = 30)
        {
            return CallPutSpread(minDaysTillExpiry, 0, 0);
        }

        /// <summary>
        /// Sets universe of a call contract and a put contract with the same expiry but lower strike price, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="callStrikeFromAtm">The desire strike price distance from the current underlying price of the call.</param>
        /// <param name="putStrikeFromAtm">The desire strike price distance from the current underlying price of the put.</param>
        /// <remarks>Applicable to Protective Collar Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse ProtectiveCollar(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            if (callStrikeFromAtm <= putStrikeFromAtm)
            {
                throw new ArgumentException("ProtectiveCollar(): strike price arguments must be in descending order, "
                    + $"{nameof(callStrikeFromAtm)}, {nameof(putStrikeFromAtm)}");
            }

            var filtered = CallPutSpread(minDaysTillExpiry, callStrikeFromAtm, putStrikeFromAtm);

            var callStrike = filtered.Single(x => x.ID.OptionRight == OptionRight.Call).ID.StrikePrice;
            var putStrike = filtered.Single(x => x.ID.OptionRight == OptionRight.Put).ID.StrikePrice;
            if (callStrike <= putStrike)
            {
                return Empty();
            }

            return filtered;
        }

        /// <summary>
        /// Sets universe of a call contract and a put contract with the same expiry and strike price, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <remarks>Applicable to Conversion and Reverse Conversion Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Conversion(int minDaysTillExpiry = 30, decimal strikeFromAtm = 5)
        {
            return CallPutSpread(minDaysTillExpiry, strikeFromAtm, strikeFromAtm);
        }

        private OptionFilterUniverse CallPutSpread(int minDaysTillExpiry, decimal callStrikeFromAtm, decimal putStrikeFromAtm, bool otm = false)
        {
            // Select the expiry as the nearest to set days later
            var contracts = GetContractsForExpiry(AllSymbols, minDaysTillExpiry).ToList();

            var calls = contracts.Where(x => x.ID.OptionRight == OptionRight.Call).ToList();
            var puts = contracts.Where(x => x.ID.OptionRight == OptionRight.Put).ToList();

            if (otm)
            {
                calls = calls.Where(x => x.ID.StrikePrice > Underlying.Price).ToList();
                puts = puts.Where(x => x.ID.StrikePrice < Underlying.Price).ToList();
            }

            if (calls.Count == 0 || puts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var callStrike = GetStrike(calls, callStrikeFromAtm);
            var call = calls.Single(x => x.ID.StrikePrice == callStrike);
            var putStrike = GetStrike(puts, putStrikeFromAtm);
            var put = puts.Single(x => x.ID.StrikePrice == putStrike);

            // Select the contracts
            return SymbolList(new List<Symbol> { call, put });
        }

        /// <summary>
        /// Sets universe of an ITM call, an ATM call, and an OTM call with the same expiry and equal strike price distance, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desire strike price distance of the ITM call and the OTM call from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Call Butterfly Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Butterfly(OptionRight.Call, minDaysTillExpiry, strikeSpread);
        }

        /// <summary>
        /// Sets universe of an ITM put, an ATM put, and an OTM put with the same expiry and equal strike price distance, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desire strike price distance of the ITM put and the OTM put from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Put Butterfly Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Butterfly(OptionRight.Put, minDaysTillExpiry, strikeSpread);
        }

        private OptionFilterUniverse Butterfly(OptionRight right, int minDaysTillExpiry, decimal strikeSpread)
        {
            if (strikeSpread <= 0)
            {
                throw new ArgumentException("ProtectiveCollar(): strikeSpread arguments must be positive");
            }

            // Select the expiry as the nearest to set days later
            var contractsForExpiry = GetContractsForExpiry(AllSymbols, minDaysTillExpiry);
            var contracts = contractsForExpiry.Where(x => x.ID.OptionRight == right).ToList();
            if (contracts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var atmStrike = GetStrike(contracts, 0m);
            var lowerStrike = GetStrike(contracts.Where(x => x.ID.StrikePrice < Underlying.Price && x.ID.StrikePrice < atmStrike), -strikeSpread);
            var upperStrike = -1m;
            if (lowerStrike != decimal.MaxValue)
            {
                upperStrike = atmStrike * 2 - lowerStrike;
            }

            // Select the contracts
            var filtered = this.Where(x =>
                x.ID.Date == contracts[0].ID.Date && x.ID.OptionRight == right &&
                (x.ID.StrikePrice == atmStrike || x.ID.StrikePrice == lowerStrike || x.ID.StrikePrice == upperStrike));
            if (filtered.Count() != 3)
            {
                return Empty();
            }
            return filtered;
        }

        /// <summary>
        /// Sets universe of an OTM call, an ATM call, an ATM put, and an OTM put with the same expiry and equal strike price distance, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desire strike price distance of the OTM call and the OTM put from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Iron Butterfly Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse IronButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            if (strikeSpread <= 0)
            {
                throw new ArgumentException("IronButterfly(): strikeSpread arguments must be positive");
            }

            // Select the expiry as the nearest to set days later
            var contracts = GetContractsForExpiry(AllSymbols, minDaysTillExpiry).ToList();
            var calls = contracts.Where(x => x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice > Underlying.Price).ToList();
            var puts = contracts.Where(x => x.ID.OptionRight == OptionRight.Put && x.ID.StrikePrice < Underlying.Price).ToList();

            if (calls.Count == 0 || puts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var atmStrike = GetStrike(contracts, 0);
            var otmCallStrike = GetStrike(calls.Where(x => x.ID.StrikePrice > atmStrike), strikeSpread);
            var otmPutStrike = -1m;
            if (otmCallStrike != decimal.MaxValue)
            {
                otmPutStrike = atmStrike * 2 - otmCallStrike;
            }

            var filtered = this.Where(x =>
                x.ID.Date == contracts[0].ID.Date && (
                x.ID.StrikePrice == atmStrike ||
                (x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice == otmCallStrike) ||
                (x.ID.OptionRight == OptionRight.Put && x.ID.StrikePrice == otmPutStrike)
            ));
            if (filtered.Count() != 4)
            {
                return Empty();
            }
            return filtered;
        }

        /// <summary>
        /// Sets universe of a far-OTM call, a near-OTM call, a near-OTM put, and a far-OTM put with the same expiry
        /// and equal strike price distance between both calls and both puts, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="nearStrikeSpread">The desire strike price distance of the near-to-expiry call and the near-to-expiry put from the current underlying price</param>
        /// <param name="farStrikeSpread">The desire strike price distance of the further-to-expiry call and the further-to-expiry put from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Iron Condor Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse IronCondor(int minDaysTillExpiry = 30, decimal nearStrikeSpread = 5, decimal farStrikeSpread = 10)
        {
            if (nearStrikeSpread <= 0 || farStrikeSpread <= 0)
            {
                throw new ArgumentException("IronCondor(): strike arguments must be positive, "
                    + $"{nameof(nearStrikeSpread)}, {nameof(farStrikeSpread)}");
            }

            if (nearStrikeSpread >= farStrikeSpread)
            {
                throw new ArgumentException("IronCondor(): strike arguments must be in ascending orders, "
                    + $"{nameof(nearStrikeSpread)}, {nameof(farStrikeSpread)}");
            }

            // Select the expiry as the nearest to set days later
            var contracts = GetContractsForExpiry(AllSymbols, minDaysTillExpiry).ToList();
            var calls = contracts.Where(x => x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice > Underlying.Price).ToList();
            var puts = contracts.Where(x => x.ID.OptionRight == OptionRight.Put && x.ID.StrikePrice < Underlying.Price).ToList();

            if (calls.Count == 0 || puts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var nearCallStrike = GetStrike(calls, nearStrikeSpread);
            var nearPutStrike = GetStrike(puts, -nearStrikeSpread);
            var farCallStrike = GetStrike(calls.Where(x => x.ID.StrikePrice > nearCallStrike), farStrikeSpread);
            var farPutStrike = -1m;
            if (farCallStrike != decimal.MaxValue)
            {
                farPutStrike = nearPutStrike - farCallStrike + nearCallStrike;
            }

            // Select the contracts
            var filtered = this.Where(x =>
                x.ID.Date == contracts[0].ID.Date && (
                (x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice == nearCallStrike) ||
                (x.ID.OptionRight == OptionRight.Put && x.ID.StrikePrice == nearPutStrike) ||
                (x.ID.OptionRight == OptionRight.Call && x.ID.StrikePrice == farCallStrike) ||
                (x.ID.OptionRight == OptionRight.Put && x.ID.StrikePrice == farPutStrike)
            ));
            if (filtered.Count() != 4)
            {
                return Empty();
            }
            return filtered;
        }

        /// <summary>
        /// Sets universe of an OTM call, an ITM call, an OTM put, and an ITM put with the same expiry with closest match to the criteria given.
        /// The OTM call has the same strike as the ITM put, while the same holds for the ITM call and the OTM put
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desire strike price distance of the OTM call and the OTM put from the current underlying price</param>
        /// <remarks>Applicable to Long and Short Box Spread Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse BoxSpread(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            if (strikeSpread <= 0)
            {
                throw new ArgumentException($"BoxSpread(): strike arguments must be positive, {nameof(strikeSpread)}");
            }

            // Select the expiry as the nearest to set days later
            var contracts = GetContractsForExpiry(AllSymbols, minDaysTillExpiry).ToList();
            if (contracts.Count == 0)
            {
                return Empty();
            }

            // Select the strike prices with the set spread range
            var higherStrike = GetStrike(contracts.Where(x => x.ID.StrikePrice > Underlying.Price), strikeSpread);
            var lowerStrike = GetStrike(contracts.Where(x => x.ID.StrikePrice < higherStrike && x.ID.StrikePrice < Underlying.Price), -strikeSpread);

            // Select the contracts
            var filtered = this.Where(x =>
                (x.ID.StrikePrice == higherStrike || x.ID.StrikePrice == lowerStrike) &&
                x.ID.Date == contracts[0].ID.Date);
            if (filtered.Count() != 4)
            {
                return Empty();
            }
            return filtered;
        }

        /// <summary>
        /// Sets universe of 2 call and 2 put contracts with the same strike price and 2 expiration dates, with closest match to the criteria given
        /// </summary>
        /// <param name="strikeFromAtm">The desire strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The mininum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The mininum days till expiry of the further conrtact from the current time, closest expiry will be selected</param>
        /// <remarks>Applicable to Long and Short Jelly Roll Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse JellyRoll(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            if (minFarDaysTillExpiry <= minNearDaysTillExpiry)
            {
                throw new ArgumentException("JellyRoll(): expiry arguments must be in ascending order, "
                    + $"{nameof(minNearDaysTillExpiry)}, {nameof(minFarDaysTillExpiry)}");
            }

            if (minNearDaysTillExpiry < 0)
            {
                throw new ArgumentException("JellyRoll(): near expiry argument must be positive.");
            }

            // Select the set strike
            var strike = AllSymbols.OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + strikeFromAtm))
                .First().ID.StrikePrice;
            var contracts = AllSymbols.Where(x => x.ID.StrikePrice == strike && x.ID.OptionRight == OptionRight.Call).ToList();

            // Select the expiries
            var nearExpiryContract = GetContractsForExpiry(contracts, minNearDaysTillExpiry).SingleOrDefault();
            if (nearExpiryContract == null)
            {
                return Empty();
            }
            var nearExpiry = nearExpiryContract.ID.Date;

            var furtherContracts = contracts.Where(x => x.ID.Date > nearExpiryContract.ID.Date).ToList();
            var farExpiryContract = GetContractsForExpiry(furtherContracts, minFarDaysTillExpiry).SingleOrDefault();
            if (farExpiryContract == null)
            {
                return Empty();
            }
            var farExpiry = farExpiryContract.ID.Date;

            var filtered = this.Where(x => x.ID.StrikePrice == strike && (x.ID.Date == nearExpiry || x.ID.Date == farExpiry));
            if (filtered.Count() != 4)
            {
                return Empty();
            }
            return filtered;
        }

        /// <summary>
        /// Sets universe of 3 call contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="middleStrikeFromAtm">The desire strike price distance from the current underlying price of the middle strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Call Ladder and Bull Call Ladder Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse CallLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm)
        {
            return Ladder(OptionRight.Call, minDaysTillExpiry, higherStrikeFromAtm, middleStrikeFromAtm, lowerStrikeFromAtm);
        }

        /// <summary>
        /// Sets universe of 3 put contracts with the same expiry and different strike prices, with closest match to the criteria given
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desire strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="middleStrikeFromAtm">The desire strike price distance from the current underlying price of the middle strike price</param>
        /// <param name="lowerStrikeFromAtm">The desire strike price distance from the current underlying price of the lower strike price</param>
        /// <remarks>Applicable to Bear Put Ladder and Bull Put Ladder Option Strategy</remarks>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse PutLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm)
        {
            return Ladder(OptionRight.Put, minDaysTillExpiry, higherStrikeFromAtm, middleStrikeFromAtm, lowerStrikeFromAtm);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Delta between the given range
        /// </summary>
        /// <param name="min">The minimum Delta value</param>
        /// <param name="max">The maximum Delta value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Delta(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(Delta));
            return this.Where(contractData => contractData.Greeks.Delta >= min && contractData.Greeks.Delta <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Delta between the given range.
        /// Alias for <see cref="Delta(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum Delta value</param>
        /// <param name="max">The maximum Delta value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse D(decimal min, decimal max)
        {
            return Delta(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Gamma between the given range
        /// </summary>
        /// <param name="min">The minimum Gamma value</param>
        /// <param name="max">The maximum Gamma value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Gamma(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(Gamma));
            return this.Where(contractData => contractData.Greeks.Gamma >= min && contractData.Greeks.Gamma <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Gamma between the given range.
        /// Alias for <see cref="Gamma(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum Gamma value</param>
        /// <param name="max">The maximum Gamma value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse G(decimal min, decimal max)
        {
            return Gamma(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Theta between the given range
        /// </summary>
        /// <param name="min">The minimum Theta value</param>
        /// <param name="max">The maximum Theta value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Theta(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(Theta));
            return this.Where(contractData => contractData.Greeks.Theta >= min && contractData.Greeks.Theta <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Theta between the given range.
        /// Alias for <see cref="Theta(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum Theta value</param>
        /// <param name="max">The maximum Theta value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse T(decimal min, decimal max)
        {
            return Theta(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Vega between the given range
        /// </summary>
        /// <param name="min">The minimum Vega value</param>
        /// <param name="max">The maximum Vega value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Vega(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(Vega));
            return this.Where(contractData => contractData.Greeks.Vega >= min && contractData.Greeks.Vega <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Vega between the given range.
        /// Alias for <see cref="Vega(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum Vega value</param>
        /// <param name="max">The maximum Vega value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse V(decimal min, decimal max)
        {
            return Vega(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Rho between the given range
        /// </summary>
        /// <param name="min">The minimum Rho value</param>
        /// <param name="max">The maximum Rho value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse Rho(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(Rho));
            return this.Where(contractData => contractData.Greeks.Rho >= min && contractData.Greeks.Rho <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with Rho between the given range.
        /// Alias for <see cref="Rho(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum Rho value</param>
        /// <param name="max">The maximum Rho value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse R(decimal min, decimal max)
        {
            return Rho(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with implied volatility between the given range
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse ImpliedVolatility(decimal min, decimal max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(ImpliedVolatility));
            return this.Where(contractData => contractData.ImpliedVolatility >= min && contractData.ImpliedVolatility <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with implied volatility between the given range.
        /// Alias for <see cref="ImpliedVolatility(decimal, decimal)"/>
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse IV(decimal min, decimal max)
        {
            return ImpliedVolatility(min, max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with open interest between the given range
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse OpenInterest(long min, long max)
        {
            ValidateSecurityTypeForSupportedFilters(nameof(OpenInterest));
            return this.Where(contractData => contractData.OpenInterest >= min && contractData.OpenInterest <= max);
        }

        /// <summary>
        /// Applies the filter to the universe selecting the contracts with open interest between the given range.
        /// Alias for <see cref="OpenInterest(long, long)"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>Universe with filter applied</returns>
        public OptionFilterUniverse OI(long min, long max)
        {
            return OpenInterest(min, max);
        }

        /// <summary>
        /// Implicitly convert the universe to a list of symbols
        /// </summary>
        /// <param name="universe"></param>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2225 // Operator overloads have named alternates
        public static implicit operator List<Symbol>(OptionFilterUniverse universe)
        {
            return universe.AllSymbols.ToList();
        }
#pragma warning restore CA2225 // Operator overloads have named alternates
#pragma warning restore CA1002 // Do not expose generic lists

        private OptionFilterUniverse Ladder(OptionRight right, int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm)
        {
            if (higherStrikeFromAtm <= lowerStrikeFromAtm || higherStrikeFromAtm <= middleStrikeFromAtm || middleStrikeFromAtm <= lowerStrikeFromAtm)
            {
                throw new ArgumentException("Ladder(): strike price arguments must be in descending order, "
                    + $"{nameof(higherStrikeFromAtm)}, {nameof(middleStrikeFromAtm)}, {nameof(lowerStrikeFromAtm)}");
            }

            // Select the expiry as the nearest to set days later
            var contracts = GetContractsForExpiry(AllSymbols.Where(x => x.ID.OptionRight == right).ToList(), minDaysTillExpiry);

            // Select the strike prices with the set ladder range
            var lowerStrikeContract = contracts.OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + lowerStrikeFromAtm)).First();
            var middleStrikeContract = contracts.Where(x => x.ID.StrikePrice > lowerStrikeContract.ID.StrikePrice)
                .OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + middleStrikeFromAtm)).FirstOrDefault();
            if (middleStrikeContract == default)
            {
                return Empty();
            }
            var higherStrikeContract = contracts.Where(x => x.ID.StrikePrice > middleStrikeContract.ID.StrikePrice)
                .OrderBy(x => Math.Abs(Underlying.Price - x.ID.StrikePrice + higherStrikeFromAtm)).FirstOrDefault();
            if (higherStrikeContract == default)
            {
                return Empty();
            }

            return this.WhereContains(new List<Symbol> { lowerStrikeContract, middleStrikeContract, higherStrikeContract });
        }

        /// <summary>
        /// Will provide all contracts that respect a specific expiration filter
        /// </summary>
        /// <param name="symbols">Symbols source to use</param>
        /// <param name="minDaysTillExpiry">The desired minimum days till expiry</param>
        /// <returns>All symbols that respect a single expiration date</returns>
        private IEnumerable<Symbol> GetContractsForExpiry(IEnumerable<Symbol> symbols, int minDaysTillExpiry)
        {
            var leastExpiryAccepted = _lastExchangeDate.AddDays(minDaysTillExpiry);
            return symbols.Where(x => x.ID.Date >= leastExpiryAccepted)
                .GroupBy(x => x.ID.Date)
                .OrderBy(x => x.Key)
                .FirstOrDefault()
                // let's order the symbols too, to guarantee determinism
                ?.OrderBy(x => x.ID) ?? Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Helper method that will select no contract
        /// </summary>
        private OptionFilterUniverse Empty()
        {
            Data = Enumerable.Empty<OptionUniverse>();
            return this;
        }

        /// <summary>
        /// Helper method that will select the given contract list
        /// </summary>
        private OptionFilterUniverse SymbolList(List<Symbol> contracts)
        {
            AllSymbols = contracts;
            return this;
        }

        private decimal GetStrike(IEnumerable<Symbol> symbols, decimal strikeFromAtm)
        {
            return symbols.OrderBy(x => Math.Abs(Underlying.Price + strikeFromAtm - x.ID.StrikePrice))
                .Select(x => x.ID.StrikePrice)
                .DefaultIfEmpty(decimal.MaxValue)
                .First();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateSecurityTypeForSupportedFilters(string filterName)
        {
            if (_option.Symbol.SecurityType == SecurityType.FutureOption)
            {
                throw new InvalidOperationException($"{filterName} filter is not supported for future options.");
            }
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
        public static OptionFilterUniverse Where(this OptionFilterUniverse universe, Func<OptionUniverse, bool> predicate)
        {
            universe.Data = universe.Data.Where(predicate).ToList();
            return universe;
        }

        /// <summary>
        /// Filters universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="predicate">Bool function to determine which Symbol are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse Where(this OptionFilterUniverse universe, PyObject predicate)
        {
            universe.Data = universe.Data.Where(predicate.ConvertToDelegate<Func<OptionUniverse, bool>>()).ToList();
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse Select(this OptionFilterUniverse universe, Func<OptionUniverse, Symbol> mapFunc)
        {
            universe.AllSymbols = universe.Data.Select(mapFunc).ToList();
            return universe;
        }

        /// <summary>
        /// Maps universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse Select(this OptionFilterUniverse universe, PyObject mapFunc)
        {
            return universe.Select(mapFunc.ConvertToDelegate<Func<OptionUniverse, Symbol>>());
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse SelectMany(this OptionFilterUniverse universe, Func<OptionUniverse, IEnumerable<Symbol>> mapFunc)
        {
            universe.AllSymbols = universe.Data.SelectMany(mapFunc).ToList();
            return universe;
        }

        /// <summary>
        /// Binds universe
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="mapFunc">Symbol function to determine which Symbols are filtered</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse SelectMany(this OptionFilterUniverse universe, PyObject mapFunc)
        {
            return universe.SelectMany(mapFunc.ConvertToDelegate<Func<OptionUniverse, IEnumerable<Symbol>>>());
        }

        /// <summary>
        /// Updates universe to only contain the symbols in the list
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="filterList">List of Symbols to keep in the Universe</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse WhereContains(this OptionFilterUniverse universe, List<Symbol> filterList)
        {
            universe.Data = universe.Data.Where(x => filterList.Contains(x)).ToList();
            return universe;
        }

        /// <summary>
        /// Updates universe to only contain the symbols in the list
        /// </summary>
        /// <param name="universe">Universe to apply the filter too</param>
        /// <param name="filterList">List of Symbols to keep in the Universe</param>
        /// <returns>Universe with filter applied</returns>
        public static OptionFilterUniverse WhereContains(this OptionFilterUniverse universe, PyObject filterList)
        {
            return universe.WhereContains(filterList.ConvertToSymbolEnumerable().ToList());
        }
    }
}
