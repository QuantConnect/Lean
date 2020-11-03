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
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Enumerates all possible values for specific optimization parameter
    /// </summary>
    public abstract class OptimizationParameterEnumerator<T> : IEnumerator<string>  where T: OptimizationParameter
    {
        protected T OptimizationParameter;
        protected int Index = -1;

        protected OptimizationParameterEnumerator(T optimizationParameter)
        {
            OptimizationParameter = optimizationParameter;
        }

        public abstract string Current { get; }
    
        object IEnumerator.Current => Current;

        public void Dispose() { }

        public abstract bool MoveNext();

        public void Reset()
        {
            Index = -1;
        }
    }
}
