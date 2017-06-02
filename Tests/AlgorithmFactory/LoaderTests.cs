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
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.AlgorithmFactory
{
    [TestFixture]
    public class LoaderTests
    {
        [Test]
        public void LoadsSamePythonAlgorithmTwice()
        {
            var assemblyPath = "../../../Algorithm.Python/BasicTemplateAlgorithm.py";

            string error1;
            IAlgorithm algorithm1;
            var one = new Loader(Language.Python, TimeSpan.FromMinutes(1), names => names.SingleOrDefault())
                .TryCreateAlgorithmInstanceWithIsolator(assemblyPath, 512, out algorithm1, out error1);

            string error2;
            IAlgorithm algorithm2;
            var two = new Loader(Language.Python, TimeSpan.FromMinutes(1), names => names.SingleOrDefault())
                .TryCreateAlgorithmInstanceWithIsolator(assemblyPath, 512, out algorithm2, out error2);

            Assert.AreNotEqual(algorithm1.ToString(), algorithm2.ToString());
        }

        [Test]
        public void LoadsTwoDifferentPythonAlgorithm()
        {
            var assemblyPath1 = "../../../Algorithm.Python/BasicTemplateAlgorithm.py";
            var assemblyPath2 = "../../../Algorithm.Python/AddRemoveSecurityRegressionAlgorithm.py";

            string error1;
            IAlgorithm algorithm1;
            var one = new Loader(Language.Python, TimeSpan.FromMinutes(1), names => names.SingleOrDefault())
                .TryCreateAlgorithmInstanceWithIsolator(assemblyPath1, 512, out algorithm1, out error1);

            string error2;
            IAlgorithm algorithm2;
            var two = new Loader(Language.Python, TimeSpan.FromMinutes(1), names => names.SingleOrDefault())
                .TryCreateAlgorithmInstanceWithIsolator(assemblyPath2, 512, out algorithm2, out error2);

            Assert.AreNotEqual(algorithm1.ToString(), algorithm2.ToString());
        }
    }
}