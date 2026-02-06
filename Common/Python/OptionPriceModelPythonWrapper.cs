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

using Python.Runtime;
using QuantConnect.Python;
using System;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionPriceModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class OptionPriceModelPythonWrapper : BasePythonWrapper<IOptionPriceModel>, IOptionPriceModel
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="model">The python model to wrap</param>
        public OptionPriceModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Evaluates the specified option contract to compute a theoretical price, IV and greeks
        /// </summary>
        /// <param name="parameters">A <see cref="OptionPriceModelParameters"/> object
        /// containing the security, slice and contract</param>
        /// <returns>An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract</returns>
        public OptionPriceModelResult Evaluate(OptionPriceModelParameters parameters)
        {
            return InvokeMethod<OptionPriceModelResult>(nameof(Evaluate), parameters);
        }
    }
}