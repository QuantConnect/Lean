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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The option contract filters shared by the option universe selection (<see cref="OptionFilterUniverse"/>)
    /// and the option chain (<see cref="Data.Market.OptionChain"/>), so both offer the same filters with the same semantics.
    /// <c>OptionChainTests.ChainExposesEveryUniverseFilter</c> checks that every universe filter is declared here
    /// </summary>
    /// <typeparam name="TSelf">The implementing type, returned by every filter for chaining</typeparam>
    public interface IOptionContractFilters<TSelf>
    {
        /// <summary>
        /// Selects the contracts with strikes in the given range relative to the underlying price, in number of strikes
        /// </summary>
        TSelf Strikes(int minStrike, int maxStrike);

        /// <summary>
        /// Selects the contracts expiring in the given range relative to the current date
        /// </summary>
        TSelf Expiration(TimeSpan minExpiry, TimeSpan maxExpiry);

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the current date
        /// </summary>
        TSelf Expiration(int minExpiryDays, int maxExpiryDays);

        /// <summary>
        /// Selects the contracts expiring on any of the given dates, ignoring the time of day
        /// </summary>
        TSelf Expiration(IEnumerable<DateTime> expiries);

        /// <summary>
        /// Selects the contracts expiring after the given date, excluding it
        /// </summary>
        TSelf ExpiringAfter(DateTime date);

        /// <summary>
        /// Selects the contracts expiring before the given date, excluding it
        /// </summary>
        TSelf ExpiringBefore(DateTime date);

        /// <summary>
        /// Selects the contracts with any of the given strike prices
        /// </summary>
        TSelf Strikes(IEnumerable<decimal> strikes);

        /// <summary>
        /// Selects the contracts with strikes above the given price, excluding it
        /// </summary>
        TSelf StrikesAbove(decimal price);

        /// <summary>
        /// Selects the contracts with strikes below the given price, excluding it
        /// </summary>
        TSelf StrikesBelow(decimal price);

        /// <summary>
        /// Selects the contracts expiring today
        /// </summary>
        TSelf ZeroDte();

        /// <summary>
        /// Selects the out of the money contracts: calls above and puts below the underlying price
        /// </summary>
        TSelf OutOfTheMoney();

        /// <summary>
        /// Selects the out of the money contracts. Alias for <see cref="OutOfTheMoney"/>
        /// </summary>
        TSelf OTM();

        /// <summary>
        /// Selects the in the money contracts: calls below and puts above the underlying price
        /// </summary>
        TSelf InTheMoney();

        /// <summary>
        /// Selects the in the money contracts. Alias for <see cref="InTheMoney"/>
        /// </summary>
        TSelf ITM();

        /// <summary>
        /// Selects the contracts at the strike closest to the underlying price, the lower strike on ties, when that strike
        /// is within the tolerance, in units of the underlying price. Zero requires a strike equal to the price
        /// </summary>
        TSelf AtTheMoney(decimal tolerance = 0);

        /// <summary>
        /// Selects the contracts at the money. Alias for <see cref="AtTheMoney"/>
        /// </summary>
        TSelf ATM(decimal tolerance = 0);

        /// <summary>
        /// Selects the call contracts
        /// </summary>
        TSelf CallsOnly();

        /// <summary>
        /// Selects the put contracts
        /// </summary>
        TSelf PutsOnly();

        /// <summary>
        /// Selects the standard contracts, excluding weeklys
        /// </summary>
        TSelf StandardsOnly();

        /// <summary>
        /// Selects the non standard weekly contracts
        /// </summary>
        TSelf WeeklysOnly();

        /// <summary>
        /// Selects the contracts of the nearest expiration
        /// </summary>
        TSelf FrontMonth();

        /// <summary>
        /// Selects the contracts of the farthest expiration
        /// </summary>
        TSelf FarthestExpiration();

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one
        /// </summary>
        TSelf BackMonths();

        /// <summary>
        /// Selects the contracts of the second nearest expiration
        /// </summary>
        TSelf BackMonth();

        /// <summary>
        /// Selects the contracts with delta in the given range
        /// </summary>
        TSelf Delta(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with delta in the given range. Alias for <see cref="Delta"/>
        /// </summary>
        TSelf D(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with gamma in the given range
        /// </summary>
        TSelf Gamma(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with gamma in the given range. Alias for <see cref="Gamma"/>
        /// </summary>
        TSelf G(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with theta in the given range
        /// </summary>
        TSelf Theta(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with theta in the given range. Alias for <see cref="Theta"/>
        /// </summary>
        TSelf T(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with vega in the given range
        /// </summary>
        TSelf Vega(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with vega in the given range. Alias for <see cref="Vega"/>
        /// </summary>
        TSelf V(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with rho in the given range
        /// </summary>
        TSelf Rho(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with rho in the given range. Alias for <see cref="Rho"/>
        /// </summary>
        TSelf R(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with implied volatility in the given range
        /// </summary>
        TSelf ImpliedVolatility(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Alias for <see cref="ImpliedVolatility"/>
        /// </summary>
        TSelf IV(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with open interest in the given range
        /// </summary>
        TSelf OpenInterest(long min, long max);

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        TSelf OI(long min, long max);

        /// <summary>
        /// Selects the single call contract with the closest match to the criteria given
        /// </summary>
        TSelf NakedCall(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0);

        /// <summary>
        /// Selects the single put contract with the closest match to the criteria given
        /// </summary>
        TSelf NakedPut(int minDaysTillExpiry = 30, decimal strikeFromAtm = 0);

        /// <summary>
        /// Selects the 2 call contracts with the same expiry and different strikes closest to the criteria given
        /// </summary>
        TSelf CallSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null);

        /// <summary>
        /// Selects the 2 put contracts with the same expiry and different strikes closest to the criteria given
        /// </summary>
        TSelf PutSpread(int minDaysTillExpiry = 30, decimal higherStrikeFromAtm = 5, decimal? lowerStrikeFromAtm = null);

        /// <summary>
        /// Selects the 2 call contracts with the same strike and different expiries closest to the criteria given
        /// </summary>
        TSelf CallCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60);

        /// <summary>
        /// Selects the 2 put contracts with the same strike and different expiries closest to the criteria given
        /// </summary>
        TSelf PutCalendarSpread(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60);

        /// <summary>
        /// Selects an OTM call and an OTM put with the same expiry closest to the criteria given
        /// </summary>
        TSelf Strangle(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5);

        /// <summary>
        /// Selects the ATM call and the ATM put with the same expiry closest to the criteria given
        /// </summary>
        TSelf Straddle(int minDaysTillExpiry = 30);

        /// <summary>
        /// Selects a call and a put with the same expiry and a lower put strike closest to the criteria given
        /// </summary>
        TSelf ProtectiveCollar(int minDaysTillExpiry = 30, decimal callStrikeFromAtm = 5, decimal putStrikeFromAtm = -5);

        /// <summary>
        /// Selects a call and a put with the same expiry and strike closest to the criteria given
        /// </summary>
        TSelf Conversion(int minDaysTillExpiry = 30, decimal strikeFromAtm = 5);

        /// <summary>
        /// Selects an ITM, an ATM and an OTM call with the same expiry and equal strike distance closest to the criteria given
        /// </summary>
        TSelf CallButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5);

        /// <summary>
        /// Selects an ITM, an ATM and an OTM put with the same expiry and equal strike distance closest to the criteria given
        /// </summary>
        TSelf PutButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5);

        /// <summary>
        /// Selects an OTM call, an ATM call, an ATM put and an OTM put with the same expiry and equal strike distance closest to the criteria given
        /// </summary>
        TSelf IronButterfly(int minDaysTillExpiry = 30, decimal strikeSpread = 5);

        /// <summary>
        /// Selects a far OTM call, a near OTM call, a near OTM put and a far OTM put with the same expiry closest to the criteria given
        /// </summary>
        TSelf IronCondor(int minDaysTillExpiry = 30, decimal nearStrikeSpread = 5, decimal farStrikeSpread = 10);

        /// <summary>
        /// Selects an OTM call, an ITM call, an OTM put and an ITM put with the same expiry closest to the criteria given
        /// </summary>
        TSelf BoxSpread(int minDaysTillExpiry = 30, decimal strikeSpread = 5);

        /// <summary>
        /// Selects 2 calls and 2 puts with the same strike and 2 expiries closest to the criteria given
        /// </summary>
        TSelf JellyRoll(decimal strikeFromAtm = 0, int minNearDaysTillExpiry = 30, int minFarDaysTillExpiry = 60);

        /// <summary>
        /// Selects 3 calls with the same expiry and different strikes closest to the criteria given
        /// </summary>
        TSelf CallLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm);

        /// <summary>
        /// Selects 3 puts with the same expiry and different strikes closest to the criteria given
        /// </summary>
        TSelf PutLadder(int minDaysTillExpiry, decimal higherStrikeFromAtm, decimal middleStrikeFromAtm, decimal lowerStrikeFromAtm);
    }
}
