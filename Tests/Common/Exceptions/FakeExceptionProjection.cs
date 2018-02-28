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
    public class FakeExceptionProjection : IExceptionProjection
    {
        private readonly Func<Exception, bool> _canProject;
        private readonly Func<Exception, Exception> _project;

        public FakeExceptionProjection()
        {
            _canProject = e => true;

            var count = 0;
            _project = e =>
            {
                if (e == null)
                {
                    return null;
                }
                return new Exception($"Projected {++count}: " + e.Message, _project(e.InnerException));
            };
        }

        public FakeExceptionProjection(Func<Exception, bool> canProject, Func<Exception, Exception> project)
        {
            _canProject = canProject;
            _project = project;
        }

        public bool CanProject(Exception exception)
        {
            return _canProject(exception);
        }

        public Exception Project(Exception exception, IExceptionProjection innerProjection)
        {
            return _project(exception);
        }
    }
}