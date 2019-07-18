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

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// TradingEconomics static class contains shortcut definitions of major Trading Economics Indicators available
    /// </summary>
    public static class TradingEconomics
    {
        /// <summary>
        /// Calendar group
        /// </summary>
        public static class Calendar
        {
            /// <summary>
            /// Country group
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// Interest Rate
                /// </summary>
                /// <returns>The symbol</returns>
                public const string InterestRate = "FDTR.C";
            }
        }

        /// <summary>
        /// Indicator group
        /// </summary>
        public static class Indicator
        {
            /// <summary>
            /// Country group
            /// </summary>
            public static class UnitedStates
            {
                /// <summary>
                /// Interest Rate
                /// </summary>
                /// <returns>The symbol</returns>
                public const string InterestRate = "FDTR.I";
            }
        }
    }
}