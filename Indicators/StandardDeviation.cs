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
using System.Linq;
using System.Collections.Generic;
using MathNetStatistics = MathNet.Numerics.Statistics.Statistics;

namespace QuantConnect.Indicators
{
    public class StandardDeviation : WindowIndicator<IndicatorDataPoint>
    {
        /// <summary>
        /// Initializes a new instance of the StandardDeviation class with the specified period.
        /// 
        /// Evaluates the standard deviation from the provided full population. 
        /// On a dataset of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="period">The sample size of the standard deviation</param>
        public StandardDeviation(int period)
            : this("STD" + period, period)
        {
        }

        public StandardDeviation(string name, int period)
            : base(name, period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady {
            get { return Samples >= Period; }
        }

        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (!IsReady) {
                return input;
            }
            IEnumerable<double> doubleValues = window.Select(i => Convert.ToDouble(i.Value));
            double std = MathNetStatistics.PopulationStandardDeviation(doubleValues);
            return Convert.ToDecimal(std);
        }
    }
}
