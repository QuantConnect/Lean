/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Interfaces;
using System;
using System.Linq;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class ToolBoxTests
    {
        [Test]
        public void ComposeDataQueueHandlerInstances()
        {
            var type = typeof(IDataQueueHandler);

            var types = AppDomain.CurrentDomain.Load("QuantConnect.ToolBox")
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToList();

            Assert.Zero(types.Count);       
        }

        [TestCase("--app=RDG --tickers=AAPL --resolution=Daily --from-date=20200820-00:00:00 --to-date=20200830-00:00:00", 1)]
        [TestCase("--app=RDG --resolution=Daily --from-date=20200820-00:00:00 --to-date=20200830-00:00:00", 0)]
        [TestCase("--app=RDG --tickers=AAPL,SPY,TSLA --resolution=Daily --from-date=20200820-00:00:00 --to-date=20200830-00:00:00", 3)]
        [TestCase("--app=RDG --tickers=ES --security-type=Future --resolution=Minute --destination-dir=/Lean/Data", 1)]
        public void CanParseTickersCorrectly(string args, int expectedTcikerCount)
        {
            var options = ToolboxArgumentParser.ParseArguments(args.Split(' '));
            var tickers = ToolboxArgumentParser.GetTickers(options);

            Assert.AreEqual(expectedTcikerCount, tickers.Count);
        }
    }
}
