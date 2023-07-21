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

        [Test]
        public void CleanupStacktrace()
        {
            var interpreter = new SystemExceptionInterpreter();

            var message = "The ticker AAPL was not found in the SymbolCache. Use the Symbol object as key instead. Accessing the securities collection/slice object by string ticker is only available for" +
                " securities added with the AddSecurity-family methods. For more details, please check out the documentation.";
            var stackTrace = "   at QuantConnect.ExtendedDictionary`1.get_Item(String ticker) in D:\\QuantConnect\\MyLean\\Lean\\Common\\ExtendedDictionary.cs:line 121\r\n   at QuantConnect.Algorithm.CSh" +
                "arp.BasicTemplateAlgorithm.OnData(Slice data) in D:\\QuantConnect\\MyLean\\Lean\\Algorithm.CSharp\\BasicTemplateAlgorithm.cs:line 58\r\n   at QuantConnect.Lean.Engine.AlgorithmManager.R" +
                "un(AlgorithmNodePacket job, IAlgorithm algorithm, ISynchronizer synchronizer, ITransactionHandler transactions, IResultHandler results, IRealTimeHandler realtime, ILeanManager leanMana" +
                "ger, CancellationToken token) in D:\\QuantConnect\\MyLean\\Lean\\Engine\\AlgorithmManager.cs:line 525";
            var result = interpreter.Interpret(new TestException(message, stackTrace), null);

            Assert.AreEqual("   at QuantConnect.ExtendedDictionary`1.get_Item(String ticker) in Common\\ExtendedDictionary.cs:line 121\r\n   at QuantConnect.Algorithm.CSharp.BasicTemplateAlgorithm.OnData(" +
                "Slice data) in Algorithm.CSharp\\BasicTemplateAlgorithm.cs:line 58\r\n   at QuantConnect.Lean.Engine.AlgorithmManager.Run(AlgorithmNodePacket job, IAlgorithm algorithm, ISynchronizer sync" +
                "hronizer, ITransactionHandler transactions, IResultHandler results, IRealTimeHandler realtime, ILeanManager leanManager, CancellationToken token) in Engine\\AlgorithmManager.cs:line 525", result.InnerException.StackTrace);
        }

        [TestCase("")]
        [TestCase(null)]
        public void CleanupStackTraceHandles(string stackTrace)
        {
            var interpreter = new SystemExceptionInterpreter();
            var result = interpreter.Interpret(new TestException("Message", stackTrace), null);

            Assert.IsNull(result.InnerException);
        }

        private class TestException : Exception
        {
            private readonly string _message;
            private readonly string _stackTrace;

            public override string Message => _message;
            public override string StackTrace => _stackTrace;

            public TestException(string message, string stackTrace)
            {
                _message = message;
                _stackTrace = Extensions.ClearLeanPaths(stackTrace);
            }
        }
    }
}
