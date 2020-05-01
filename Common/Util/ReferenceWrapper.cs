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

namespace QuantConnect.Util
{
    /// <summary>
    /// We wrap a T instance, a value type, with a class, a reference type, to achieve thread safety when assigning new values
    /// and reading from multiple threads. This is possible because assignments are atomic operations in C# for reference types (among others).
    /// </summary>
    /// <remarks>This is a simpler, performance oriented version of <see cref="Ref"/></remarks>
    public class ReferenceWrapper<T> 
        where T : struct
    {
        /// <summary>
        /// The current value
        /// </summary>
        public T Value;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="value">The value to use</param>
        public ReferenceWrapper(T value)
        {
            Value = value;
        }
    }
}
