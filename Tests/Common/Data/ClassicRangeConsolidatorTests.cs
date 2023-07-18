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

using QuantConnect.Data.Consolidators;

namespace QuantConnect.Tests.Common.Data
{
    public class ClassicRangeConsolidatorTests : RangeConsolidatorTests
    {
        protected override RangeConsolidator CreateConsolidator()
        {
            return new ClassicRangeConsolidator(100m, x => x.Value, x => 10m);
        }

        protected override decimal[][] GetRangeConsolidatorExpectedValues()
        {
            return new decimal[][] {
                    new decimal[]{ 90m, 90m, 91m, 91m, 10m },
                    new decimal[]{ 94.5m, 93.5m, 94.5m, 93.5m, 20m},
                    new decimal[]{ 89.5m, 89m, 90m, 90m, 20m},
                    new decimal[]{ 90.5m, 90m, 91m, 91m, 20m},
                    new decimal[]{ 91.5m, 90.5m, 91.5m, 90.5m, 10m},
                    new decimal[]{ 90m, 90m, 91m, 91m, 20m},
                };
        }
    }
}
