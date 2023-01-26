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

using QuantConnect.Data;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders.Slippage"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Slippage.VolumeShareSlippageModel"/> class and its consumers or related classes
        /// </summary>
        public static class VolumeShareSlippageModel
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidMarketDataType(BaseData data)
            {
                return $"VolumeShareSlippageModel.GetSlippageApproximation(): Cannot use this model with market data type {data.GetType()}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string VolumeNotReportedForMarketDataType(SecurityType securityType)
            {
                return Invariant($"VolumeShareSlippageModel.GetSlippageApproximation(): {securityType} security type often ") +
                    "does not report volume. If you intend to model slippage beyond the spread, please consider another model.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NegativeOrZeroBarVolume(decimal barVolume, decimal slippagePercent)
            {
                return Invariant($@"VolumeShareSlippageModel.GetSlippageApproximation: Bar volume cannot be zero or negative. Volume: {
                    barVolume}. Using maximum slippage percentage of {slippagePercent}");
            }
        }
    }
}
