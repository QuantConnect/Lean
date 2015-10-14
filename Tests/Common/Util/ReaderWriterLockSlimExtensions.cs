using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ReaderWriterLockSlimExtensions
    {
        [Test]
        public void EntersReadLock()
        {
            var slim = new ReaderWriterLockSlim();

            var token = slim.Read();

            Assert.IsTrue(slim.IsReadLockHeld);
            slim.ExitReadLock();

            slim.Dispose();
        }
        [Test]
        public void ExitsReadLock()
        {
            var slim = new ReaderWriterLockSlim();

            var token = slim.Read();
            token.Dispose();
            Assert.IsFalse(slim.IsReadLockHeld);

            slim.Dispose();
        }

        [Test]
        public void EntersWriteLock()
        {
            var slim = new ReaderWriterLockSlim();

            var token = slim.Write();
            Assert.IsTrue(slim.IsWriteLockHeld);
            slim.ExitWriteLock();

            slim.Dispose();
        }
        [Test]
        public void ExitsWriteLock()
        {
            var slim = new ReaderWriterLockSlim();

            var token = slim.Read();
            token.Dispose();
            Assert.IsFalse(slim.IsReadLockHeld);

            slim.Dispose();
        }
    }
}
