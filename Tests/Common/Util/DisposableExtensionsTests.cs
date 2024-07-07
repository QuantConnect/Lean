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
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DisposableExtensionsTests
    {
#pragma warning disable CA2000
        [Test]
        public void ReturnsFalseForNullDisposable()
        {
            IDisposable disposable = null;
            var result = disposable.DisposeSafely();
            Assert.IsFalse(result);
        }

        [Test]
        public void ReturnsTrueOnSuccessfulDisposal()
        {
            var disposable = new Disposable();
            var result = disposable.DisposeSafely();
            Assert.IsTrue(result);
            Assert.IsTrue(disposable.DisposeWasCalled);
        }

        [Test]
        public void InvokesErrorHandlerOnExceptionDuringDispose()
        {
            var errorHandlerWasInvoked = false;
            var disposable = new Disposable(throwException: true);
            var result = disposable.DisposeSafely(error => errorHandlerWasInvoked = true);
            Assert.IsFalse(result);
            Assert.IsTrue(errorHandlerWasInvoked);
            Assert.IsTrue(disposable.DisposeWasCalled);
        }

        [Test]
        public void SwallowsObjectDisposedException()
        {
            var errorHandlerWasInvoked = false;
            var disposable = new Disposable();
            disposable.Dispose();
            Assert.IsTrue(disposable.DisposeWasCalled);

            var result = disposable.DisposeSafely(error => errorHandlerWasInvoked = true);
            Assert.IsTrue(result);
            Assert.IsFalse(errorHandlerWasInvoked);
        }

        private sealed class Disposable : IDisposable
        {
            private readonly bool _throwException;
            public bool DisposeWasCalled { get; private set; }

            public Disposable(bool throwException = false)
            {
                _throwException = throwException;
            }

            public void Dispose()
            {
                if (DisposeWasCalled)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                DisposeWasCalled = true;

                if (_throwException)
                {
                    throw new RegressionTestException();
                }
            }
        }
    }
}
