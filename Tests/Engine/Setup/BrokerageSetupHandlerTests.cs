using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture]
    public class BrokerageSetupHandlerTests
    {
        private IAlgorithm _algorithm;
        private ITransactionHandler _transactionHandler;
        private IResultHandler _resultHanlder;
        private IBrokerage _brokerage;

        [TestFixtureSetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _transactionHandler = new BrokerageTransactionHandler();

        }

        [Test]
        public void BrokerageSetupHanlder_CanGetOpenOrders()
        {
            
        }
    }
}
