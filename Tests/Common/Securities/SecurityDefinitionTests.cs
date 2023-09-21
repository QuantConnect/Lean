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

using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityDefinitionTests
    {
        [TestCase("AAPL R735QTJ8XC9X,03783310,BBG000B9XRY4,2046251,US0378331005,320193", "03783310", "BBG000B9XRY4", "2046251", "US0378331005", 320193)]
        [TestCase("GOOG T1AZ164W5VTX,38259P50,BBG000BHSKN9,B020QX2,US38259P5089,", "38259P50", "BBG000BHSKN9", "B020QX2", "US38259P5089", null)]
        [TestCase("MXA R735QTJ8XC9X,,BBG000BFDB59,,US6040621095,", null, "BBG000BFDB59", null, "US6040621095", null)]
        [TestCase("MXA R735QTJ8XC9X,60406210,,,US6040621095,", "60406210", null, null, "US6040621095", null)]
        [TestCase("MXA R735QTJ8XC9X,60406210,BBG000BFDB59,,US6040621095,", "60406210", "BBG000BFDB59", null, "US6040621095", null)]
        [TestCase("MXA R735QTJ8XC9X,60406210,BBG000BFDB59,,,", "60406210", "BBG000BFDB59", null, null, null)]
        
        public void ParsesCSV(string line, string cusipExpected, string figiExpected, string sedolExpected, string isinExpected, int? cikExpected)
        {
            var securityDefinition = SecurityDefinition.FromCsvLine(line);
            
            Assert.AreEqual(cusipExpected, securityDefinition.CUSIP);
            Assert.AreEqual(figiExpected, securityDefinition.CompositeFIGI);
            Assert.AreEqual(sedolExpected, securityDefinition.SEDOL);
            Assert.AreEqual(isinExpected, securityDefinition.ISIN);
            Assert.AreEqual(cikExpected, securityDefinition.CIK);
        }

        [TestCase("MXA ABCDEFGHIJKL,60406210,BBG000BFDB59,,US6040621095")]
        [TestCase("60406210,BBG000BFDB59,,US6040621095")]
        [TestCase("MXA R735QTJ8XC9X,60406210,BBG000BFDB59,")]
        [TestCase("MXA R735QTJ8XC9X")]
        [TestCase("")]
        public void DoesNotParseCSV(string line)
        {
            try
            {
                var lines = SecurityDefinition.FromCsvLine(line);
                Assert.Fail($"Successfully read line when expecting failure: {line}");
            }
            catch
            {
            }
        }
    }
}
