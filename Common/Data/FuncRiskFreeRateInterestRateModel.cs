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
using System;

namespace QuantConnect.Data
{
    /// <summary>
    /// Constant risk free rate interest rate model
    /// </summary>
    public class FuncRiskFreeRateInterestRateModel : IRiskFreeInterestRateModel
    {
        private readonly Func<DateTime, decimal> _getInterestRateFunc;

        /// <summary>
        /// Create class instance of interest rate provider
        /// </summary>
        public FuncRiskFreeRateInterestRateModel(Func<DateTime, decimal> getInterestRateFunc)
        {
            _getInterestRateFunc = getInterestRateFunc;
        }

        /// <summary>
        /// Create class instance of interest rate provider with given PyObject
        /// </summary>
        public FuncRiskFreeRateInterestRateModel(PyObject getInterestRateFunc)
        {
            using (Py.GIL())
            {
                _getInterestRateFunc = getInterestRateFunc.ConvertToDelegate<Func<DateTime, decimal>>();
            }
        }

        /// <summary>
        /// Get interest rate by a given date
        /// </summary>
        /// <param name="date">The date</param>
        /// <returns>Interest rate on the given date</returns>
        public decimal GetInterestRate(DateTime date)
        {
            return _getInterestRateFunc(date);
        }
    }
}
