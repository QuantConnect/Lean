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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides a wrapper for <see cref="IndicatorBase{IBaseData}"/> implementations written in python
    /// </summary>
    public class PythonIndicator : IndicatorBase<IBaseData>, IIndicatorWarmUpPeriodProvider
    {
        private bool _isReady;
        private BasePythonWrapper<IIndicator> _indicatorWrapper;

        /// <summary>
        /// Initializes a new instance of the PythonIndicator class using the specified name.
        /// </summary>
        /// <remarks>This overload allows inheritance for python classes with no arguments</remarks>
        public PythonIndicator()
            : base("")
        {
        }

        /// <summary>
        /// Initializes a new instance of the PythonIndicator class using the specified name.
        /// </summary>
        /// <remarks>This overload allows inheritance for python classes with multiple arguments</remarks>
        public PythonIndicator(params PyObject[] args)
            : base(GetIndicatorName(args[0]))
        {
        }

        /// <summary>
        /// Initializes a new instance of the PythonIndicator class using the specified name.
        /// </summary>
        /// <param name="indicator">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        public PythonIndicator(PyObject indicator)
            : base(GetIndicatorName(indicator))
        {
            SetIndicator(indicator);
        }

        /// <summary>
        /// Sets the python implementation of the indicator
        /// </summary>
        /// <param name="indicator">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        public void SetIndicator(PyObject indicator)
        {
            _indicatorWrapper = new BasePythonWrapper<IIndicator>(indicator, validateInterface: false);
            foreach (var attributeName in new[] { "IsReady", "Update", "Value" })
            {
                if (!_indicatorWrapper.HasAttr(attributeName))
                {
                    var name = GetIndicatorName(indicator);

                    var message = $"Indicator.{attributeName.ToSnakeCase()} must be implemented. " +
                                    $"Please implement this missing method in {name}";

                    if (attributeName == "IsReady")
                    {
                        message += " or use PythonIndicator as base:" +
                                    $"{Environment.NewLine}class {name}(PythonIndicator):";
                    }

                    throw new NotImplementedException(message);
                }
            }

            WarmUpPeriod = GetIndicatorWarmUpPeriod();
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _isReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized
        /// </summary>
        public int WarmUpPeriod { get; protected set; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseData input)
        {
            _isReady = _indicatorWrapper.InvokeMethod<bool?>(nameof(Update), input)
                ?? _indicatorWrapper.GetProperty<bool>(nameof(IsReady));
            return _indicatorWrapper.GetProperty<decimal>("Value");
        }

        /// <summary>
        /// Get the indicator WarmUpPeriod parameter. If not defined, use 0
        /// </summary>
        /// <returns>The WarmUpPeriod of the indicator.</returns>
        private int GetIndicatorWarmUpPeriod()
        {
            return _indicatorWrapper.HasAttr(nameof(WarmUpPeriod)) ? _indicatorWrapper.GetProperty<int>(nameof(WarmUpPeriod)) : 0;
        }

        /// <summary>
        /// Get the indicator Name. If not defined, use the class name
        /// </summary>
        /// <param name="indicator">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        /// <returns>The indicator Name.</returns>
        private static string GetIndicatorName(PyObject indicator)
        {
            using (Py.GIL())
            {
                PyObject name;
                if (indicator.HasAttr("Name"))
                {
                    name = indicator.GetAttr("Name");
                }
                else if (indicator.HasAttr("name"))
                {
                    name = indicator.GetAttr("name");
                }
                else
                {
                    name = indicator.GetAttr("__class__").GetAttr("__name__");
                }
                return name.GetAndDispose<string>();
            }
        }
    }
}
