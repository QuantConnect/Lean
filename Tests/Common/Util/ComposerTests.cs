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

using System.ComponentModel.Composition;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ComposerTests
    {
        [Test]
        public void ComposesTypes()
        {
            var instances = Composer.Instance.GetExportedValues<IExport>().ToList();
            Assert.AreEqual(5, instances.Count);
            Assert.AreEqual(1, instances.Count(x => x.GetType() == typeof (Export1)));
            Assert.AreEqual(1, instances.Count(x => x.GetType() == typeof (Export2)));
            Assert.AreEqual(1, instances.Count(x => x.GetType() == typeof (Export3)));
            Assert.AreEqual(1, instances.Count(x => x.GetType() == typeof (Export4)));
            Assert.AreEqual(1, instances.Count(x => x.GetType() == typeof(Export5)));
        }

        [Test]
        public void GetsInstanceUsingPredicate()
        {
            var instance = Composer.Instance.Single<IExport>(x => x.Id == 3);
            Assert.IsNotNull(instance);
            Assert.IsInstanceOf(typeof (Export3), instance);
        }

        [Test]
        public void ResetsReturnsSameInstances()
        {
            var composer = Composer.Instance;
            var export1 = composer.Single<IExport>(x => x.Id == 3);
            Assert.IsNotNull(export1);
            composer.Reset();
            var export2 = composer.Single<IExport>(x => x.Id == 3);
            Assert.AreEqual(export1, export2);
        }

        [Test]
        public void GetsMultipleInterfacesInstance()
        {
            var instance1 = Composer.Instance.Single<IOneMoreExport>(x => x.Export == 5);
            Assert.IsNotNull(instance1);
            Assert.IsInstanceOf(typeof(Export5), instance1);

            var instance2 = Composer.Instance.Single<IExport>(x => x.Id == 5);
            Assert.IsNotNull(instance2);
            Assert.IsInstanceOf(typeof(Export5), instance2);

            Assert.AreEqual(instance1, instance2);
        }

        [InheritedExport(typeof(IExport))]
        interface IExport
        {
            int Id { get; }
        }

        [InheritedExport(typeof(IOneMoreExport))]
        interface IOneMoreExport
        {
            decimal Export { get; }
        }

        class Export1 : IExport
        {
            public int Id { get { return 1; } }
        }
        class Export2 : IExport
        {
            public int Id { get { return 2; } }
        }
        class Export3 : IExport
        {
            public int Id { get { return 3; } }
        }
        class Export4 : IExport
        {
            public int Id { get { return 4; } }
        }

        class Export5 : IExport, IOneMoreExport
        {
            public decimal Export { get { return 5; } }

            public int Id { get { return 5; } }
        }
    }
}
