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

using System.Runtime.CompilerServices;

using QuantConnect.Securities;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders.Fees"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fees.FeeModel"/> class and its consumers or related classes
        /// </summary>
        public static class FeeModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedSecurityType(Securities.Security security)
            {
                return Invariant($"Unsupported security type: {security.Type}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fees.AlphaStreamsFeeModel"/> class and its consumers or related classes
        /// </summary>
        public static class AlphaStreamsFeeModel
        {
            /// <summary>
            /// Returns a string message saying the given market is unexpected
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedEquityMarket(string market)
            {
                return Invariant($"AlphaStreamsFeeModel(): unexpected equity Market {market}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fees.ExanteFeeModel"/> class and its consumers or related classes
        /// </summary>
        public static class ExanteFeeModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedExchange(Orders.Order order)
            {
                return Invariant($"Unsupported exchange: ${order.Symbol.ID.Market}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fees.InteractiveBrokersFeeModel"/> class and its consumers or related classes
        /// </summary>
        public static class InteractiveBrokersFeeModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedOptionMarket(string market)
            {
                return Invariant($"InteractiveBrokersFeeModel(): unexpected option Market {market}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedFutureMarket(string market)
            {
                return Invariant($"InteractiveBrokersFeeModel(): unexpected future Market {market}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnexpectedEquityMarket(string market)
            {
                return Invariant($"InteractiveBrokersFeeModel(): unexpected equity Market {market}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnitedStatesFutureFeesUnsupportedSecurityType(Securities.Security security)
            {
                return Invariant($"InteractiveBrokersFeeModel.UnitedStatesFutureFees(): Unsupported security type: {security.Type}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string HongKongFutureFeesUnexpectedQuoteCurrency(Securities.Security security)
            {
                return Invariant($"Unexpected quote currency {security.QuoteCurrency.Symbol} for Hong Kong futures exchange");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Fees.TDAmeritradeFeeModel"/> class and its consumers or related classes
        /// </summary>
        public static class TDAmeritradeFeeModel
        {
            /// <summary>
            /// Returns a string message for unsupported security types in TDAmeritradeFeeModel
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedSecurityType(SecurityType securityType)
            {
                return $"TDAmeritradeFeeModel doesn't return correct fee model for SecurityType = {nameof(securityType)}";
            }
        }
    }
}
