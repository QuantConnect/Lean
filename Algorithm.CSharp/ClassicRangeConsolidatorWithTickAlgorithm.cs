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
using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of how to use ClassicRangeConsolidator with Tick resolution
    /// </summary>
    public class ClassicRangeConsolidatorWithTickAlgorithm : RangeConsolidatorWithTickAlgorithm
    {
        protected override RangeConsolidator CreateRangeConsolidator()
        {
            return new ClassicRangeConsolidator(Range);
        }

        protected override void OnDataConsolidated(Object sender, RangeBar rangeBar)
        {
            base.OnDataConsolidated(sender, rangeBar);

            if (rangeBar.Volume == 0)
            {
                throw new Exception($"All RangeBar's should have non-zero volume, but this doesn't");
            }
        }
    }
}
