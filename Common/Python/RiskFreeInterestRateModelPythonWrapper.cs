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

namespace QuantConnect.Python
{
    /// <summary>
    /// Wraps a <see cref="PyObject"/> object that represents a risk-free interest rate model
    /// </summary>
    public class RiskFreeInterestRateModelPythonWrapper : BasePythonWrapper<IRiskFreeInterestRateModel>, IRiskFreeInterestRateModel
    {
        /// <summary>
        /// Constructor for initializing the <see cref="RiskFreeInterestRateModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Represents a security's model of buying power</param>
        public RiskFreeInterestRateModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Get interest rate by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Interest rate on the given date</returns>
        public decimal GetInterestRate(DateTime date)
        {
            return InvokeMethod<decimal>(nameof(GetInterestRate), date);
        }

        /// <summary>
        /// Converts a <see cref="PyObject"/> object into a <see cref="IRiskFreeInterestRateModel"/> object, wrapping it if necessary
        /// </summary>
        /// <param name="model">The Python model</param>
        /// <returns>The converted <see cref="IRiskFreeInterestRateModel"/> instance</returns>
        public static IRiskFreeInterestRateModel FromPyObject(PyObject model)
        {
            if (!model.TryConvert(out IRiskFreeInterestRateModel riskFreeInterestRateModel))
            {
                riskFreeInterestRateModel = new RiskFreeInterestRateModelPythonWrapper(model);
            }

            return riskFreeInterestRateModel;
        }
    }
}
