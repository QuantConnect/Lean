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
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Benchmarks
{
    /// <summary>
    /// Creates a benchmark defined by the closing price of a <see cref="Security"/> instance
    /// </summary>
    public class SecurityBenchmark : IBenchmark
    {
        /// <summary>
        /// The benchmark security
        /// </summary>
        public Security Security { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityBenchmark"/> class
        /// </summary>
        public SecurityBenchmark(Security security)
        {
            Security = security;
        }

        /// <summary>
        /// Evaluates this benchmark at the specified time in units of the account's currency.
        /// </summary>
        /// <param name="time">The time to evaluate the benchmark at</param>
        /// <returns>The value of the benchmark at the specified time
        /// in units of the account's currency.</returns>
        public decimal Evaluate(DateTime time)
        {
            return Security.Price * Security.QuoteCurrency.ConversionRate;
        }

        /// <summary>
        /// Helper function that will create a security with the given SecurityManager
        /// for a specific symbol and then create a SecurityBenchmark for it
        /// </summary>
        /// <param name="securities">SecurityService to create the security</param>
        /// <param name="symbol">The symbol to create a security benchmark with</param>
        /// <returns>The new SecurityBenchmark</returns>
        public static SecurityBenchmark CreateInstance(SecurityManager securities, Symbol symbol)
        {
            return new SecurityBenchmark(securities.CreateBenchmarkSecurity(symbol));
        }
    }
}
