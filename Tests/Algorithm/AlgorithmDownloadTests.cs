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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuantConnect.Tests.Algorithm
{
    // For now these tests are excluded from the Travis build because of occasional web server errors.
    [TestFixture, Category("TravisExclude")]
    public class AlgorithmDownloadTests
    {
        [Test]
        public void Download_Without_Parameters_Successfully()
        {
            var algo = new QCAlgorithm();
            algo.SetApi(new Api.Api());
            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/"));
            Assert.IsNotEmpty(content);
        }

        [Test]
        public void Download_With_CSharp_Parameter_Successfully()
        {
            var algo = new QCAlgorithm();
            algo.SetApi(new Api.Api());

            var byteKey = Encoding.ASCII.GetBytes($"UserName:Password");
            var headers = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Authorization", $"Basic ({Convert.ToBase64String(byteKey)})")
            };

            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/", headers));
            Assert.IsNotEmpty(content);
        }

        [Test]
        public void Download_With_Python_Parameter_Successfully()
        {
            var algo = new QCAlgorithm();
            algo.SetApi(new Api.Api());

            var byteKey = Encoding.ASCII.GetBytes($"UserName:Password");
            var value = $"Basic ({Convert.ToBase64String(byteKey)})";

            var headers = new PyDict();
            using (Py.GIL())
            {
                headers.SetItem("Authorization".ToPython(), value.ToPython());
            }

            var content = string.Empty;
            Assert.DoesNotThrow(() => content = algo.Download("https://www.quantconnect.com/", headers));
            Assert.IsNotEmpty(content);
        }
    }
}
