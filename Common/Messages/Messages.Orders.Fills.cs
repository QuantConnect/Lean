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
using System.Runtime.CompilerServices;

using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders.Fills"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fills.FillModel"/> class and its consumers or related classes
        /// </summary>
        public static class FillModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledAtStalePrice(Security security, Prices prices)
            {
                return Invariant($"Warning: fill at stale price ({prices.EndTime.ToStringInvariant()} {security.Exchange.TimeZone})");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MarketNeverCloses(Security security, OrderType orderType)
            {
                return Invariant($"Market never closes for this symbol {security.Symbol}, can no submit a {nameof(orderType)} order.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string SubscribedTypesToString(HashSet<Type> subscribedTypes)
            {
                return subscribedTypes == null
                    ? string.Empty
                    : Invariant($" SubscribedTypes: [{string.Join(",", subscribedTypes.Select(type => type.Name))}]");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketDataToGetAskPriceForFilling(Security security, HashSet<Type> subscribedTypes = null)
            {
                return Invariant($"Cannot get ask price to perform fill for {security.Symbol} because no market data was found.") +
                    SubscribedTypesToString(subscribedTypes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoMarketDataToGetBidPriceForFilling(Security security, HashSet<Type> subscribedTypes = null)
            {
                return Invariant($"Cannot get bid price to perform fill for {security.Symbol} because no market data was found.") +
                    SubscribedTypesToString(subscribedTypes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NoDataSubscriptionFoundForFilling(Security security)
            {
                return Invariant($"Cannot perform fill for {security.Symbol} because no data subscription were found.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fills.EquityFillModel"/> class and its consumers or related classes
        /// </summary>
        public static class EquityFillModel
        {
            public static string MarketOnOpenFillNoOfficialOpenOrOpeningPrintsWithinOneMinute =
                "No trade with the OfficialOpen or OpeningPrints flag within the 1-minute timeout.";

            public static string MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithinOneMinute =
                "No trade with the OfficialClose or ClosingPrints flag within the 1-minute timeout.";

            public static string MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithoutExtendedMarketHours =
                "No trade with the OfficialClose or ClosingPrints flag for data that does not include extended market hours.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithLastTickTypeData(Tick tick)
            {
                return Invariant($"Fill with last {tick.TickType} data.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteData(Security security)
            {
                return Invariant($@"Warning: No trade information available at {security.LocalTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using Quote data");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteTickData(Security security, Tick quoteTick)
            {
                return Invariant($@"Warning: fill at stale price ({quoteTick.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}), using Quote Tick data.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithTradeTickData(Security security, Tick tradeTick)
            {
                return Invariant($@"Warning: No quote information available at {tradeTick.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using Trade Tick data");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithQuoteBarData(Security security, QuoteBar quoteBar)
            {
                return Invariant($@"Warning: fill at stale price ({quoteBar.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}), using QuoteBar data.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FilledWithTradeBarData(Security security, TradeBar tradeBar)
            {
                return Invariant($@"Warning: No quote information available at {tradeBar.EndTime.ToStringInvariant()} {
                    security.Exchange.TimeZone}, order filled using TradeBar data");
            }
        }
    }
}
