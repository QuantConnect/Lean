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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Parameters;

namespace QuantConnect.Tests.Common.Parameters
{
    [TestFixture]
    public class ParameterAttributeTests
    {
        [Test]
        public void SetsParameterValues()
        {
            var instance = new Instance();
            var parameters = new Dictionary<string, string>
            {
                {"PublicField", "1"},
                {"PublicProperty", "1"},
                {"ProtectedField", "1"},
                {"ProtectedProperty", "1"},
                {"InternalField", "1"},
                {"InternalProperty", "1"},
                {"PrivateField", "1"},
                {"PrivateProperty", "1"},
            };

            ParameterAttribute.ApplyAttributes(parameters, instance);
        }

        [Test]
        public void FindsParameters()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var parameters = ParameterAttribute.GetParametersFromAssembly(assembly);
            foreach (var field in typeof(Instance).GetFields(ParameterAttribute.BindingFlags).Where(x => !x.Name.Contains(">k__")))
            {
                Assert.IsTrue(parameters.ContainsKey(field.Name), "Failed on Field: " + field.Name);
            }
            foreach (var property in typeof(Instance).GetProperties(ParameterAttribute.BindingFlags))
            {
                Assert.IsTrue(parameters.ContainsKey(property.Name), "Failed on Property: " + property.Name);
            }
        }

        [Test]
        public void FindsParametersUsingReflection()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var parameters = ParameterAttribute.GetParametersFromAssembly(assembly);
            foreach (var field in typeof(Instance).GetFields(ParameterAttribute.BindingFlags).Where(x => !x.Name.Contains(">k__")))
            {
                Assert.IsTrue(parameters.ContainsKey(field.Name), "Failed on Field: " + field.Name);
            }
            foreach (var property in typeof(Instance).GetProperties(ParameterAttribute.BindingFlags))
            {
                Assert.IsTrue(parameters.ContainsKey(property.Name), "Failed on Property: " + property.Name);
            }
        }

        class Instance
        {
            [Parameter]
            public int PublicField = 0;
            [Parameter]
            public int PublicProperty { get; set; }
            [Parameter]
            protected int ProtectedField = 0;
            [Parameter]
            protected int ProtectedProperty { get; set; }
            [Parameter]
            internal int InternalField = 0;
            [Parameter]
            internal int InternalProperty { get; set; }
            [Parameter]
            private int PrivateField = 0;
            [Parameter]
            private int PrivateProperty { get; set; }

            public void AssertValues(int expected)
            {

                Assert.AreEqual(expected, PublicField);
                Assert.AreEqual(expected, PublicProperty);
                Assert.AreEqual(expected, ProtectedField);
                Assert.AreEqual(expected, ProtectedProperty);
                Assert.AreEqual(expected, InternalField);
                Assert.AreEqual(expected, InternalProperty);
                Assert.AreEqual(expected, PrivateField);
                Assert.AreEqual(expected, PrivateProperty);
            }
        }
    }
}
