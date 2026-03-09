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
using System.Globalization;
using QuantConnect.Interfaces;
using QuantConnect.Data.Fundamental;
using System.Runtime.CompilerServices;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Base fundamental data provider
    /// </summary>
    public class BaseFundamentalDataProvider : IFundamentalDataProvider
    {
        /// <summary>
        /// True if live trading
        /// </summary>
        public bool LiveMode { get; set; }

        /// <summary>
        /// THe data provider instance to use
        /// </summary>
        protected IDataProvider DataProvider { get; set; }

        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <param name="liveMode">True if running in live mode</param>
        public virtual void Initialize(IDataProvider dataProvider, bool liveMode)
        {
            LiveMode = liveMode;
            DataProvider = dataProvider;
        }

        /// <summary>
        /// Will fetch the requested fundamental information for the requested time and symbol
        /// </summary>
        /// <typeparam name="T">The expected data type</typeparam>
        /// <param name="time">The time to request this data for</param>
        /// <param name="securityIdentifier">The security identifier</param>
        /// <param name="name">The name of the fundamental property</param>
        /// <returns>The fundamental information</returns>
        public virtual T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get's the default value for the given T type
        /// </summary>
        /// <typeparam name="T">The expected T type</typeparam>
        /// <returns>The default value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetDefault<T>()
        {
            if (typeof(T) == typeof(double))
            {
                return (T)Convert.ChangeType(double.NaN, typeof(T), CultureInfo.InvariantCulture);
            }
            else if (typeof(T) == typeof(decimal))
            {
                return (T)Convert.ChangeType(decimal.Zero, typeof(T), CultureInfo.InvariantCulture);
            }
            return default;
        }

        /// <summary>
        /// True if the given value is none
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone(object value) => IsNone(value?.GetType(), value);

        /// <summary>
        /// True if the given value is none
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone(Type type, object value)
        {
            if (type == null || value == null)
            {
                return true;
            }
            else if(type == typeof(double))
            {
                return ((double)value).IsNaNOrInfinity();
            }
            else if (type == typeof(decimal))
            {
                return default(decimal) == (decimal)value;
            }
            else if (type == typeof(DateTime))
            {
                return default(DateTime) == (DateTime)value;
            }
            return false;
        }
    }
}
