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

using System;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a functional implementation of the <see cref="IParallelRunnerWorkItem"/> interface
    /// </summary>
    public sealed class FuncParallelRunnerWorkItem : IParallelRunnerWorkItem
    {
        private readonly Func<bool> _isReady;
        private readonly Action _execute;

        /// <summary>
        /// Determines if this work item is ready to be processed
        /// </summary>
        public bool IsReady
        {
            get { return _isReady(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncParallelRunnerWorkItem"/> class
        /// </summary>
        /// <param name="isReady">The IsReady function implementation</param>
        /// <param name="execute">The Execute function implementation</param>
        public FuncParallelRunnerWorkItem(Func<bool> isReady, Action execute)
        {
            _isReady = isReady;
            _execute = execute;
        }

        /// <summary>
        /// Executes this work item
        /// </summary>
        /// <returns>The result of execution</returns>
        public void Execute()
        {
            _execute();
        }
    }
}