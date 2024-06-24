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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect
{
    /// <summary>
    /// Custom exception class for regression tests
    /// </summary>
    public class RegressionTestException: Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="RegressionTestException"/>
        /// </summary>
        public RegressionTestException() { }

        /// <summary>
        /// Creates a new isntance of a <see cref="RegressionTestException"/>
        /// </summary>
        /// <param name="message">Message to be thrown by the exception</param>
        public RegressionTestException(string message): base(message) { }

        /// <summary>
        /// Creates a new instance of a <see cref="RegressionTestException"/>
        /// </summary>
        /// <param name="message">Message to be thrown by the exception</param>
        /// <param name="inner">Inner exception thrown</param>
        public RegressionTestException(string message, Exception inner) :base(message, inner) { }
    }
}
