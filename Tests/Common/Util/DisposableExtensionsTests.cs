using System;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class DisposableExtensionsTests
    {
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
                    throw new Exception();
                }
            }
        }
    }
}
