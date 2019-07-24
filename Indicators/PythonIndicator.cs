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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides a wrapper for <see cref="IndicatorBase{IBaseDataBar}"/> implementations written in python
    /// </summary>
    public class PythonIndicator : IndicatorBase<IBaseDataBar>
    {
        private readonly dynamic _indicator;

        /// <summary>
        /// Get the indicator Name. If not defined, use the class name
        /// </summary>
        /// <param name="indicator">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        /// <returns>The indicator Name.</returns>
        private static string GetIndicatorName(PyObject indicator)
        {
            using (Py.GIL())
            {
                var name = indicator.HasAttr("Name")
                    ? indicator.GetAttr("Name")
                    : indicator.GetAttr("__class__").GetAttr("__name__");

                return name.GetAndDispose<string>();
            }
        }

        /// <summary>
        /// Initializes a new instance of the PythonIndicator class using the specified name.
        /// </summary>
        /// <param name="indicator">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        public PythonIndicator(PyObject indicator)
            : base(GetIndicatorName(indicator))
        {
            using (Py.GIL())
            {
                foreach (var attributeName in new[] {"Update", "IsReady", "Value"})
                {
                    if (!indicator.HasAttr(attributeName))
                    {
                        throw new NotImplementedException(
                            $"Indicator.{attributeName} must be implemented. Please implement this missing method on {indicator.GetPythonType()}"
                        );
                    }
                }
            }

            _indicator = indicator;
        }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public new bool Update(IBaseData input)
        {
            using (Py.GIL())
            {
                _indicator.Update(input);
            }

            return IsReady;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        /// <remarks>If IsReady is not defined in the Python indicator, always returns false</remarks>
        public override bool IsReady
        {
            get
            {
                using (Py.GIL())
                {
                    return _indicator.IsReady;
                }
            }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            Update(input);

            using (Py.GIL())
            {
                return _indicator.Value;
            }
        }
    }
}