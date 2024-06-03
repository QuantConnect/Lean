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
 *
*/

using System;
using System.IO;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Python;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonVirtualEnvironmentTests
    {
        [TestCase(null)]
        [TestCase("/something")]
        [TestCase("non existing env")]
        public void InvalidVirtualEnvironment(string venv)
        {
            Assert.IsFalse(PythonInitializer.ActivatePythonVirtualEnvironment(venv));
        }

        [Test]
        public void VirtualEnvironment()
        {
            if (Directory.Exists("testenv"))
            {
                Directory.Delete("testenv", true);
            }

            using (Py.GIL())
            {
                PythonEngine.Exec("import venv;venv.create(\"testenv\", system_site_packages=True)");
            }

            Assert.IsTrue(PythonInitializer.ActivatePythonVirtualEnvironment("testenv"));
        }

        [Test, Explicit("Requires a virtual env setup")]
        public void AssertVirtualEnvironment()
        {
            Assert.IsTrue(PythonInitializer.ActivatePythonVirtualEnvironment("/lean-testenv"));

            using (Py.GIL())
            {
                var code = @"import lean

def assertLeanVersion():
    return lean.__version__";
                var module = PyModule.FromString(Guid.NewGuid().ToString(), code);
                dynamic assertVersion = module.GetAttr("assertLeanVersion");

                Assert.AreEqual("1.0.185", (string)assertVersion());
            }
        }
    }
}
