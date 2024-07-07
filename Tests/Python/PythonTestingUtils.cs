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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Python
{
    public static class PythonTestingUtils
    {
        public static dynamic GetSlices(Symbol symbol)
        {
            var slices = Enumerable
                .Range(0, 100)
                .Select(i =>
                {
                    var time = new DateTime(2013, 10, 7).AddMilliseconds(14400000 + i * 10000);
                    return new Slice(
                        time,
                        new List<BaseData>
                        {
                            new Tick
                            {
                                Time = time,
                                Symbol = symbol,
                                Value = 167 + i / 10,
                                Quantity = 1 + i * 10,
                                Exchange = "T"
                            }
                        },
                        time
                    );
                });
            return slices;
        }
    }
}
