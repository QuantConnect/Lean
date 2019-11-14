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

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Exception raised when an algorithm framework model has dependencies on other framework components
    /// and those dependencies have not been met. The provided error message should detail the dependency
    /// and how a user should go about fixing the issue
    /// </summary>
    public class IncompatibleFrameworkModelsException : Exception
    {
        /// <summary>
        /// Gets the name of the model that raised the error
        /// </summary>
        public string Model { get; }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="IncompatibleFrameworkModelsException"/> class
        /// </summary>
        /// <param name="model">The model that raised the error</param>
        /// <param name="message">An error message describing the dependency and how to fix the error</param>
        /// <param name="inner">An optional inner exception</param>
        public IncompatibleFrameworkModelsException(string model, string message, Exception inner = null)
            : base(message, inner)
        {
            Model = model;
        }
    }
}
