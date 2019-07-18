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
using Python.Runtime;
using QuantConnect.Python;

namespace QuantConnect.Tests.Python
{
    public static class PythonWrapperTests
    {
        [TestFixture]
        public class ValidateImplementationOf
        {
            [Test]
            public void ThrowsOnMissingMember()
            {
                using (Py.GIL())
                {
                    var module = PythonEngine.ModuleFromString(nameof(ValidateImplementationOf), MissingMethod1);
                    var model = module.GetAttr("ModelMissingMethod1");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws
                        .Exception.InstanceOf<NotImplementedException>().With.Message.Contains("Method1"));
                }
            }

            [Test]
            public void DoesNotThrowWhenInterfaceFullyImplemented()
            {
                using (Py.GIL())
                {
                    var module = PythonEngine.ModuleFromString(nameof(ValidateImplementationOf), FullyImplemented);
                    var model = module.GetAttr("FullyImplementedModel");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws.Nothing);
                }
            }

            [Test]
            public void DoesNotThrowWhenDerivedFromCSharpModel()
            {
                using (Py.GIL())
                {
                    var module = PythonEngine.ModuleFromString(nameof(ValidateImplementationOf), DerivedFromCsharp);
                    var model = module.GetAttr("DerivedFromCSharpModel");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws.Nothing);
                }
            }

            private const string FullyImplemented =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class FullyImplementedModel:
    def Method1():
        pass
    def Method2():
        pass

";

            private const string DerivedFromCsharp =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class DerivedFromCSharpModel(PythonWrapperTests.ValidateImplementationOf.Model):
    def Method1():
        pass

";

            private const string MissingMethod1 =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class ModelMissingMethod1:
    def Method2():
        pass

";

            interface IModel
            {
                void Method1();
                void Method2();
            }

            public class Model : IModel
            {
                public void Method1()
                {
                }

                public void Method2()
                {
                }
            }
        }
    }
}
