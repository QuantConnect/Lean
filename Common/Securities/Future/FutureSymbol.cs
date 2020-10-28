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

using QuantConnect.Logging;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Static class contains common utility methods specific to symbols representing the future contracts
    /// </summary>
    public static class FutureSymbol
    {
        /// <summary>
        /// Returns true if the option is a standard contract that expires 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Future symbol</param>
        /// <returns></returns>
        public static bool IsStandard(Symbol symbol)
        {
            var date = symbol.ID.Date;
            var symbolToCheck = symbol.HasUnderlying ? symbol.Underlying : symbol;

            try
            {
                // Use our FutureExpiryFunctions to determine standard contracts dates.
                var expiryFunction = FuturesExpiryFunctions.FuturesExpiryFunction(symbolToCheck);
                var standardDate = expiryFunction(date);

                // If the date on this symbol and the nearest standard date are equal then it is a standard contract
                if (date == standardDate)
                {
                    return true;
                }

                // Date is non-standard, return false
                return false;
            }
            catch
            {
                Log.Error($"Could not find standard date for {symbolToCheck}, will be classified as weekly (non-standard)");
                return false;
            }
        }

        /// <summary>
        /// Returns true if the future contract is a weekly contract
        /// </summary>
        /// <param name="symbol">Future symbol</param>
        /// <returns></returns>
        public static bool IsWeekly(Symbol symbol)
        {
            return !IsStandard(symbol);
        }
    }
}
