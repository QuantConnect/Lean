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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Seed a security price from a history function
    /// </summary>
    public class FuncSecuritySeeder : ISecuritySeeder
    {
        private readonly Func<Security, IEnumerable<BaseData>> _seedFunction;


        /// <summary>
        /// Constructor that takes as a parameter the security used to seed the price
        /// </summary>
        /// <param name="seedFunction">The seed function to use</param>
        public FuncSecuritySeeder(PyObject seedFunction)
        {
            var result = seedFunction.ConvertToDelegate<Func<Security, object>>();
            _seedFunction = security =>
            {
                var dataObject = result(security);
                var dataPoint = dataObject as BaseData;
                if (dataPoint != null)
                {
                    return new[] { dataPoint };
                }

                return (IEnumerable<BaseData>)dataObject;
            };
        }

        /// <summary>
        /// Constructor that takes as a parameter the security used to seed the price
        /// </summary>
        /// <param name="seedFunction">The seed function to use</param>
        public FuncSecuritySeeder(Func<Security, BaseData> seedFunction)
         : this(security => { return new []{ seedFunction(security) }; })
        {
        }

        /// <summary>
        /// Constructor that takes as a parameter the security used to seed the price
        /// </summary>
        /// <param name="seedFunction">The seed function to use</param>
        public FuncSecuritySeeder(Func<Security, IEnumerable<BaseData>> seedFunction)
        {
            _seedFunction = seedFunction;
        }

        /// <summary>
        /// Seed the security
        /// </summary>
        /// <param name="security"><see cref="Security"/> being seeded</param>
        /// <returns>true if the security was seeded, false otherwise</returns>
        public bool SeedSecurity(Security security)
        {
            try
            {
                // Do not seed canonical symbols
                if (!security.Symbol.IsCanonical())
                {
                    var gotData = false;
                    foreach (var seedData in _seedFunction(security))
                    {
                        gotData = true;
                        security.SetMarketPrice(seedData);
                        Log.Debug("FuncSecuritySeeder.SeedSecurity(): " + Messages.FuncSecuritySeeder.SeededSecurityInfo(seedData));
                    }

                    if (!gotData)
                    {
                        Log.Trace("FuncSecuritySeeder.SeedSecurity(): " + Messages.FuncSecuritySeeder.UnableToSeedSecurity(security));
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Trace("FuncSecuritySeeder.SeedSecurity(): " + Messages.FuncSecuritySeeder.UnableToSecurityPrice(security) + $": {exception}");
                return false;
            }

            return true;
        }
    }
}
