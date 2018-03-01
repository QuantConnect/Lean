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
    /// Provids a fake implementation that can be utilized in tests
    /// </summary>
    public class FakeExceptionInterpreter : IExceptionInterpreter
    {
        private readonly Func<Exception, bool> _canInterpret;
        private readonly Func<Exception, Exception> _interpret;

        public FakeExceptionInterpreter()
        {
            _canInterpret = e => true;

            var count = 0;
            _interpret = e =>
            {
                if (e == null)
                {
                    return null;
                }
                return new Exception($"Projected {++count}: " + e.Message, _interpret(e.InnerException));
            };
        }

        public FakeExceptionInterpreter(Func<Exception, bool> canInterpret, Func<Exception, Exception> interpret)
        {
            _canInterpret = canInterpret;
            _interpret = interpret;
        }

        public bool CanInterpret(Exception exception)
        {
            return _canInterpret(exception);
        }

        public Exception Interpret(Exception exception, IExceptionInterpreter innerInterpreter)
        {
            return _interpret(exception);
        }
    }
}