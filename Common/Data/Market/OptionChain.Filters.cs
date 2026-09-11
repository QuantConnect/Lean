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
using System.Linq;
using Python.Runtime;
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The option chain filters, the same ones the option universe selection offers, see <see cref="IOptionContractFilters{TSelf}"/>.
    /// Each filter returns a new chain, leaving this one untouched
    /// </summary>
    public partial class OptionChain
    {
        #region Filters

        /// <summary>
        /// Selects the contracts with strikes in the given range relative to the underlying price, in number of strikes.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Strikes"/>
        /// </summary>
        /// <param name="minStrike">The minimum strike relative to the underlying price, for example, -1 would filter out contracts further than 1 strike below market price</param>
        /// <param name="maxStrike">The maximum strike relative to the underlying price, for example, +1 would filter out contracts further than 1 strike above market price</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Strikes(int minStrike, int maxStrike)
        {
            return Filter(universe => universe.Strikes(minStrike, maxStrike));
        }

        /// <summary>
        /// Selects the contracts expiring in the given range relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(TimeSpan, TimeSpan)"/>
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            return Filter(universe => universe.Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(int, int)"/>
        /// </summary>
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Expiration(int minExpiryDays, int maxExpiryDays)
        {
            return Filter(universe => universe.Expiration(minExpiryDays, maxExpiryDays));
        }

        /// <summary>
        /// Selects the contracts expiring on any of the given dates. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(IEnumerable{DateTime})"/>
        /// </summary>
        /// <param name="expiries">The expiration dates</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Expiration(IEnumerable<DateTime> expiries)
        {
            return Filter(universe => universe.Expiration(expiries));
        }

        /// <summary>
        /// Selects the contracts expiring after the given date, excluding it. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.ExpiringAfter"/>
        /// </summary>
        /// <param name="date">The date the expirations must be after</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ExpiringAfter(DateTime date)
        {
            return Filter(universe => universe.ExpiringAfter(date));
        }

        /// <summary>
        /// Selects the contracts expiring before the given date, excluding it. Time of day is ignored.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.ExpiringBefore"/>
        /// </summary>
        /// <param name="date">The date the expirations must be before</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ExpiringBefore(DateTime date)
        {
            return Filter(universe => universe.ExpiringBefore(date));
        }

        /// <summary>
        /// Selects the contracts with any of the given strike prices.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Strikes(IEnumerable{decimal})"/>
        /// </summary>
        /// <param name="strikes">The strike prices</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Strikes(IEnumerable<decimal> strikes)
        {
            return Filter(universe => universe.Strikes(strikes));
        }

        /// <summary>
        /// Selects the contracts with strikes above the given price, excluding it.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.StrikesAbove"/>
        /// </summary>
        /// <param name="price">The price the strikes must be above</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain StrikesAbove(decimal price)
        {
            return Filter(universe => universe.StrikesAbove(price));
        }

        /// <summary>
        /// Selects the contracts with strikes below the given price, excluding it.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.StrikesBelow"/>
        /// </summary>
        /// <param name="price">The price the strikes must be below</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain StrikesBelow(decimal price)
        {
            return Filter(universe => universe.StrikesBelow(price));
        }

        /// <summary>
        /// Selects the contracts expiring today. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.ZeroDte"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ZeroDte()
        {
            return Filter(universe => universe.ZeroDte());
        }

        /// <summary>
        /// Selects the call contracts. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallsOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain CallsOnly()
        {
            return Filter(universe => universe.CallsOnly());
        }

        /// <summary>
        /// Selects the put contracts. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutsOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain PutsOnly()
        {
            return Filter(universe => universe.PutsOnly());
        }

        /// <summary>
        /// Selects the out of the money contracts: calls with strikes above the underlying price and puts with strikes below it.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.OutOfTheMoney"/>
        /// </summary>
        /// <returns>A new chain with the filter applied, empty when the underlying price is unknown</returns>
        public OptionChain OutOfTheMoney()
        {
            return Filter(universe => universe.OutOfTheMoney());
        }

        /// <summary>
        /// Selects the out of the money contracts. Alias for <see cref="OutOfTheMoney"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain OTM()
        {
            return OutOfTheMoney();
        }

        /// <summary>
        /// Selects the in the money contracts: calls with strikes below the underlying price and puts with strikes above it.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.InTheMoney"/>
        /// </summary>
        /// <returns>A new chain with the filter applied, empty when the underlying price is unknown</returns>
        public OptionChain InTheMoney()
        {
            return Filter(universe => universe.InTheMoney());
        }

        /// <summary>
        /// Selects the in the money contracts. Alias for <see cref="InTheMoney"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ITM()
        {
            return InTheMoney();
        }

        /// <summary>
        /// Selects the contracts at the money: the ones at the strike closest to the underlying price, the lower strike on ties,
        /// when that strike is within the tolerance. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.AtTheMoney"/>
        /// </summary>
        /// <param name="tolerance">The largest distance between the closest strike and the underlying price for the strike to be
        /// at the money, in units of the underlying price. Zero requires a strike equal to the underlying price. Null, the default,
        /// allows <see cref="BaseOptionFilterUniverse{TUniverse, TData}.DefaultAtTheMoneyTolerance"/> of the underlying price</param>
        /// <returns>A new chain with the filter applied, empty when the underlying price is unknown</returns>
        public OptionChain AtTheMoney(decimal? tolerance = null)
        {
            return Filter(universe => universe.AtTheMoney(tolerance));
        }

        /// <summary>
        /// Selects the contracts at the money. Alias for <see cref="AtTheMoney"/>
        /// </summary>
        /// <param name="tolerance">The largest distance between the closest strike and the underlying price for the strike to be
        /// at the money, in units of the underlying price. Zero requires a strike equal to the underlying price. Null, the default,
        /// allows <see cref="BaseOptionFilterUniverse{TUniverse, TData}.DefaultAtTheMoneyTolerance"/> of the underlying price</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ATM(decimal? tolerance = null)
        {
            return AtTheMoney(tolerance);
        }

        /// <summary>
        /// Selects the standard contracts in the chain, excluding weeklys. Unlike <see cref="ContractSecurityFilterUniverse{T, TData}.StandardsOnly"/>,
        /// it applies to the contracts already selected, so it can be combined with the expiry filters in any order
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain StandardsOnly()
        {
            return Filter(universe => universe.StandardsOnly());
        }

        /// <summary>
        /// Selects the non standard weekly contracts in the chain. Unlike <see cref="ContractSecurityFilterUniverse{T, TData}.WeeklysOnly"/>,
        /// it applies to the contracts already selected, so it can be combined with the expiry filters in any order
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain WeeklysOnly()
        {
            return Filter(universe => universe.WeeklysOnly());
        }

        /// <summary>
        /// Selects the contracts of the nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.FrontMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain FrontMonth()
        {
            return Filter(universe => universe.FrontMonth());
        }

        /// <summary>
        /// Selects the contracts of the farthest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.FarthestExpiration"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain FarthestExpiration()
        {
            return Filter(universe => universe.FarthestExpiration());
        }

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonths"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain BackMonths()
        {
            return Filter(universe => universe.BackMonths());
        }

        /// <summary>
        /// Selects the contracts of the second nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain BackMonth()
        {
            return Filter(universe => universe.BackMonth());
        }

        /// <summary>
        /// Selects the contracts with delta in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Delta"/>
        /// </summary>
        /// <param name="min">The minimum delta value</param>
        /// <param name="max">The maximum delta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Delta(decimal min, decimal max)
        {
            return Filter(universe => universe.Delta(min, max));
        }

        /// <summary>
        /// Selects the contracts with delta in the given range. Alias for <see cref="Delta"/>
        /// </summary>
        /// <param name="min">The minimum delta value</param>
        /// <param name="max">The maximum delta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain D(decimal min, decimal max)
        {
            return Delta(min, max);
        }

        /// <summary>
        /// Selects the contracts with gamma in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Gamma"/>
        /// </summary>
        /// <param name="min">The minimum gamma value</param>
        /// <param name="max">The maximum gamma value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Gamma(decimal min, decimal max)
        {
            return Filter(universe => universe.Gamma(min, max));
        }

        /// <summary>
        /// Selects the contracts with gamma in the given range. Alias for <see cref="Gamma"/>
        /// </summary>
        /// <param name="min">The minimum gamma value</param>
        /// <param name="max">The maximum gamma value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain G(decimal min, decimal max)
        {
            return Gamma(min, max);
        }

        /// <summary>
        /// Selects the contracts with theta in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Theta"/>
        /// </summary>
        /// <param name="min">The minimum theta value</param>
        /// <param name="max">The maximum theta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Theta(decimal min, decimal max)
        {
            return Filter(universe => universe.Theta(min, max));
        }

        /// <summary>
        /// Selects the contracts with theta in the given range. Alias for <see cref="Theta"/>
        /// </summary>
        /// <param name="min">The minimum theta value</param>
        /// <param name="max">The maximum theta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain T(decimal min, decimal max)
        {
            return Theta(min, max);
        }

        /// <summary>
        /// Selects the contracts with vega in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Vega"/>
        /// </summary>
        /// <param name="min">The minimum vega value</param>
        /// <param name="max">The maximum vega value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Vega(decimal min, decimal max)
        {
            return Filter(universe => universe.Vega(min, max));
        }

        /// <summary>
        /// Selects the contracts with vega in the given range. Alias for <see cref="Vega"/>
        /// </summary>
        /// <param name="min">The minimum vega value</param>
        /// <param name="max">The maximum vega value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain V(decimal min, decimal max)
        {
            return Vega(min, max);
        }

        /// <summary>
        /// Selects the contracts with rho in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Rho"/>
        /// </summary>
        /// <param name="min">The minimum rho value</param>
        /// <param name="max">The maximum rho value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Rho(decimal min, decimal max)
        {
            return Filter(universe => universe.Rho(min, max));
        }

        /// <summary>
        /// Selects the contracts with rho in the given range. Alias for <see cref="Rho"/>
        /// </summary>
        /// <param name="min">The minimum rho value</param>
        /// <param name="max">The maximum rho value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain R(decimal min, decimal max)
        {
            return Rho(min, max);
        }

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.ImpliedVolatility"/>
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ImpliedVolatility(decimal min, decimal max)
        {
            return Filter(universe => universe.ImpliedVolatility(min, max));
        }

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Alias for <see cref="ImpliedVolatility"/>
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain IV(decimal min, decimal max)
        {
            return ImpliedVolatility(min, max);
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain OpenInterest(long min, long max)
        {
            return Filter(universe => universe.OpenInterest(min, max));
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain OI(long min, long max)
        {
            return OpenInterest(min, max);
        }

        /// <summary>
        /// Selects the contracts matching the given predicate, e.g. <c>chain.where(lambda contract: contract.open_interest > 100)</c>.
        /// From C# use Linq's Where, which keeps this chain's type untouched
        /// </summary>
        /// <param name="predicate">Function determining which contracts are kept</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Where(PyObject predicate)
        {
            return new OptionChain(this, Contracts.Values.Where(predicate.SafeAs<Func<OptionContract, bool>>()));
        }

        #endregion

        #region Strategy filters

        /// <summary>
        /// Selects the single call contract with the closest match to the criteria given, for a naked, covered or protective call. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.NakedCall"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain NakedCall(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return Filter(universe => universe.NakedCall(minDaysTillExpiry, strikeFromAtm));
        }

        /// <summary>
        /// Selects the single put contract with the closest match to the criteria given, for a naked, covered or protective put. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.NakedPut"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain NakedPut(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0)
        {
            return Filter(universe => universe.NakedPut(minDaysTillExpiry, strikeFromAtm));
        }

        /// <summary>
        /// Selects the 2 call contracts with the same expiry and different strikes closest to the criteria given, for a bull or bear call spread. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallSpread"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desired strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desired strike price distance from the current underlying price of the lower strike price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain CallSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Filter(universe => universe.CallSpread(minDaysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm));
        }

        /// <summary>
        /// Selects the 2 put contracts with the same expiry and different strikes closest to the criteria given, for a bull or bear put spread. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutSpread"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desired strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="lowerStrikeFromAtm">The desired strike price distance from the current underlying price of the lower strike price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain PutSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null)
        {
            return Filter(universe => universe.PutSpread(minDaysTillExpiry, higherStrikeFromAtm, lowerStrikeFromAtm));
        }

        /// <summary>
        /// Selects the 2 call contracts with the same strike and different expiries closest to the criteria given, for a call calendar spread. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallCalendarSpread"/>
        /// </summary>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The minimum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The minimum days till expiry of the further contract from the current time, closest expiry will be selected</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain CallCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            return Filter(universe => universe.CallCalendarSpread(strikeFromAtm, minNearDaysTillExpiry, minFarDaysTillExpiry));
        }

        /// <summary>
        /// Selects the 2 put contracts with the same strike and different expiries closest to the criteria given, for a put calendar spread. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutCalendarSpread"/>
        /// </summary>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The minimum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The minimum days till expiry of the further contract from the current time, closest expiry will be selected</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain PutCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            return Filter(universe => universe.PutCalendarSpread(strikeFromAtm, minNearDaysTillExpiry, minFarDaysTillExpiry));
        }

        /// <summary>
        /// Selects an OTM call and an OTM put with the same expiry closest to the criteria given, for a strangle. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Strangle"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="callStrikeFromAtm">The desired strike price distance from the current underlying price of the OTM call, must be positive</param>
        /// <param name="putStrikeFromAtm">The desired strike price distance from the current underlying price of the OTM put, must be negative</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain Strangle(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            return Filter(universe => universe.Strangle(minDaysTillExpiry, callStrikeFromAtm, putStrikeFromAtm));
        }

        /// <summary>
        /// Selects the ATM call and the ATM put with the same expiry closest to the criteria given, for a straddle. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Straddle"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain Straddle(int minDaysTillExpiry = 30)
        {
            return Filter(universe => universe.Straddle(minDaysTillExpiry));
        }

        /// <summary>
        /// Selects a call and a put with the same expiry and a lower put strike closest to the criteria given, for a protective collar. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.ProtectiveCollar"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="callStrikeFromAtm">The desired strike price distance from the current underlying price of the call</param>
        /// <param name="putStrikeFromAtm">The desired strike price distance from the current underlying price of the put</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain ProtectiveCollar(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5)
        {
            return Filter(universe => universe.ProtectiveCollar(minDaysTillExpiry, callStrikeFromAtm, putStrikeFromAtm));
        }

        /// <summary>
        /// Selects a call and a put with the same expiry and strike closest to the criteria given, for a conversion or reverse conversion. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Conversion"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain Conversion(int minDaysTillExpiry = 30, decimal strikeFromAtm = 5)
        {
            return Filter(universe => universe.Conversion(minDaysTillExpiry, strikeFromAtm));
        }

        /// <summary>
        /// Selects an ITM, an ATM and an OTM call with the same expiry and equal strike distance closest to the criteria given, for a call butterfly. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallButterfly"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desired strike price distance of the ITM and OTM calls from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain CallButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Filter(universe => universe.CallButterfly(minDaysTillExpiry, strikeSpread));
        }

        /// <summary>
        /// Selects an ITM, an ATM and an OTM put with the same expiry and equal strike distance closest to the criteria given, for a put butterfly. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutButterfly"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desired strike price distance of the ITM and OTM puts from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain PutButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Filter(universe => universe.PutButterfly(minDaysTillExpiry, strikeSpread));
        }

        /// <summary>
        /// Selects an OTM call, an ATM call, an ATM put and an OTM put with the same expiry and equal strike distance closest to the criteria given, for an iron butterfly. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.IronButterfly"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desired strike price distance of the OTM call and the OTM put from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain IronButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Filter(universe => universe.IronButterfly(minDaysTillExpiry, strikeSpread));
        }

        /// <summary>
        /// Selects a far OTM call, a near OTM call, a near OTM put and a far OTM put with the same expiry closest to the criteria given, for an iron condor. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.IronCondor"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="nearStrikeSpread">The desired strike price distance of the near call and near put from the current underlying price</param>
        /// <param name="farStrikeSpread">The desired strike price distance of the far call and far put from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain IronCondor(int minDaysTillExpiry = 30, decimal nearStrikeSpread = 5, decimal farStrikeSpread = 10)
        {
            return Filter(universe => universe.IronCondor(minDaysTillExpiry, nearStrikeSpread, farStrikeSpread));
        }

        /// <summary>
        /// Selects an OTM call, an ITM call, an OTM put and an ITM put with the same expiry closest to the criteria given, for a box spread. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.BoxSpread"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="strikeSpread">The desired strike price distance of the OTM call and the OTM put from the current underlying price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain BoxSpread(int minDaysTillExpiry = 30, decimal strikeSpread = 5)
        {
            return Filter(universe => universe.BoxSpread(minDaysTillExpiry, strikeSpread));
        }

        /// <summary>
        /// Selects 2 calls and 2 puts with the same strike and 2 expiries closest to the criteria given, for a jelly roll. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.JellyRoll"/>
        /// </summary>
        /// <param name="strikeFromAtm">The desired strike price distance from the current underlying price</param>
        /// <param name="minNearDaysTillExpiry">The minimum days till expiry of the closer contract from the current time, closest expiry will be selected</param>
        /// <param name="minFarDaysTillExpiry">The minimum days till expiry of the further contract from the current time, closest expiry will be selected</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain JellyRoll(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60)
        {
            return Filter(universe => universe.JellyRoll(strikeFromAtm, minNearDaysTillExpiry, minFarDaysTillExpiry));
        }

        /// <summary>
        /// Selects 3 calls with the same expiry and different strikes closest to the criteria given, for a bull or bear call ladder. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallLadder"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desired strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="middleStrikeFromAtm">The desired strike price distance from the current underlying price of the middle strike price</param>
        /// <param name="lowerStrikeFromAtm">The desired strike price distance from the current underlying price of the lower strike price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain CallLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm)
        {
            return Filter(universe => universe.CallLadder(minDaysTillExpiry, higherStrikeFromAtm, middleStrikeFromAtm, lowerStrikeFromAtm));
        }

        /// <summary>
        /// Selects 3 puts with the same expiry and different strikes closest to the criteria given, for a bull or bear put ladder. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutLadder"/>
        /// </summary>
        /// <param name="minDaysTillExpiry">The minimum days till expiry from the current time, closest expiry will be selected</param>
        /// <param name="higherStrikeFromAtm">The desired strike price distance from the current underlying price of the higher strike price</param>
        /// <param name="middleStrikeFromAtm">The desired strike price distance from the current underlying price of the middle strike price</param>
        /// <param name="lowerStrikeFromAtm">The desired strike price distance from the current underlying price of the lower strike price</param>
        /// <returns>A new chain with the selected contracts, empty if there is no match</returns>
        public OptionChain PutLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm)
        {
            return Filter(universe => universe.PutLadder(minDaysTillExpiry, higherStrikeFromAtm, middleStrikeFromAtm, lowerStrikeFromAtm));
        }

        /// <summary>
        /// Applies the given universe filter to the contracts of this chain and returns the result as a new chain
        /// </summary>
        /// <param name="filter">The universe filter to apply</param>
        private OptionChain Filter(Func<OptionChainFilterUniverse, OptionChainFilterUniverse> filter)
        {
            var universe = new OptionChainFilterUniverse(this);
            // the type filters (standards/weeklys) are only applied on demand, like the universe selection does after the user filter
            return new OptionChain(this, filter(universe).ApplyTypesFilter());
        }

        #endregion
    }
}
