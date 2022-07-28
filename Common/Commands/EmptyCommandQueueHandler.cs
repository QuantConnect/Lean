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

using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Provides an implementation of <see cref="ICommandHandler"/> that never
    /// returns a command. This is useful for local console backtesting when we don't
    /// really want to issue commands
    /// </summary>
    public class EmptyCommandQueueHandler : BaseCommandHandler
    {
        /// <summary>
        /// Return empty enumerable.
        /// </summary>
        /// <returns>null</returns>
        protected override IEnumerable<ICommand> GetCommands()
        {
            return Enumerable.Empty<ICommand>();
        }
    }
}
