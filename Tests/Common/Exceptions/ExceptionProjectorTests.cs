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
using NUnit.Framework;
using QuantConnect.Exceptions;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class ExceptionProjectorTests
    {
        [Test]
        public void CreatesFromAssemblies()
        {
            var assembly = typeof(MockExceptionProjection).Assembly;
            var projector = ExceptionProjector.CreateFromAssemblies(new[] {assembly});
            Assert.AreEqual(1, projector.Projections.Count(p => p.GetType() == typeof(MockExceptionProjection)));
        }

        [Test]
        public void CallsProjectOnFirstProjectionThatCanProject()
        {
            var canProjectCalled = new List<int>();
            var projectCalled = new List<int>();
            var projections = new[]
            {
                new MockExceptionProjection(e =>
                {
                    canProjectCalled.Add(0);
                    return false;
                }, e =>
                {
                    projectCalled.Add(0);
                    return e;
                }),
                new MockExceptionProjection(e =>
                {
                    canProjectCalled.Add(1);
                    return true;
                }, e =>
                {
                    projectCalled.Add(1);
                    return e;
                }),
                new MockExceptionProjection(e =>
                {
                    canProjectCalled.Add(2);
                    return false;
                }, e =>
                {
                    projectCalled.Add(2);
                    return e;
                })
            };

            var projector = new ExceptionProjector(projections);
            projector.Project(new Exception());

            // can project called for 1st and second entry
            Assert.Contains(0, canProjectCalled);
            Assert.Contains(1, canProjectCalled);
            Assert.That(canProjectCalled, Is.Not.Contains(2));

            // project only called on second entry
            Assert.That(canProjectCalled, Is.Not.Contains(0));
            Assert.Contains(1, projectCalled);
            Assert.That(canProjectCalled, Is.Not.Contains(2));

            // third entry never touched
            Assert.That(canProjectCalled, Is.Not.Contains(2));
            Assert.That(projectCalled, Is.Not.Contains(2));
        }

        class MockExceptionProjection : IExceptionProjection
        {
            private readonly Func<Exception, bool> _canProject;
            private readonly Func<Exception, Exception> _project;

            public MockExceptionProjection()
            {
                _canProject = e => true;

                var count = 0;
                _project = e => new Exception($"Projected {++count}", e);
            }

            public MockExceptionProjection(Func<Exception, bool> canProject, Func<Exception, Exception> project)
            {
                _canProject = canProject;
                _project = project;
            }

            public bool CanProject(Exception exception)
            {
                return _canProject(exception);
            }

            public Exception Project(Exception exception)
            {
                return _project(exception);
            }
        }
    }
}
