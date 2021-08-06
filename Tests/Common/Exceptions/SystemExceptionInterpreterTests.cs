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
using QuantConnect.Exceptions;

namespace QuantConnect.Tests.Common.Exceptions
{
    [TestFixture]
    public class SystemExceptionInterpreterTests
    {
        [Test]
        public void InterpreterCorrectly()
        {
            var result = SystemExceptionInterpreter.TryGetLineAndFile(@"   at QuantConnect.Algorithm.CSharp.BasicTemplateAlgorithm.Initialize() in D:\QuantConnect\MyLean\Lean\Algorithm.CSharp\BasicTemplateAlgorithm.cs:line 50
   at QuantConnect.Lean.Engine.Setup.BacktestingSetupHandler.<>c__DisplayClass27_0.<Setup>b__0() in D:\QuantConnect\MyLean\Lean\Engine\Setup\BacktestingSetupHandler.cs:line 186
", out var fileAndLine);

            Assert.IsTrue(result);
            Assert.AreEqual(" in BasicTemplateAlgorithm.cs:line 50", fileAndLine);
        }
    }
}
