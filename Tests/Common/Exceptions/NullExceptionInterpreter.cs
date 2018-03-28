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
using QuantConnect.Exceptions;

namespace QuantConnect.Tests.Common.Exceptions
{
    /// <summary>
    /// Provides a null implementation of <see cref="IExceptionInterpreter"/>
    /// </summary>
    public class NullExceptionInterpreter : IExceptionInterpreter
    {
        public static readonly IExceptionInterpreter Instance = new NullExceptionInterpreter();

        public int Order => int.MaxValue;

        private NullExceptionInterpreter()
        {
        }

        public bool CanInterpret(Exception exception)
        {
            return false;
        }

        public Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            return exception;
        }
    }
}