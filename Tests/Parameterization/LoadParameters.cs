using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Parameterization
{
    [TestFixture]
    class LoadParameters
    {
        // Helper classes
        class IntAlgo : QCAlgorithm
        {
            [IntParameter(5, 10)]
            public int slowParam = 2;

            [IntParameter(12, 20, 2)]
            public int fastParam = 10;
        }

        class BoolAlgo : QCAlgorithm
        {
            [BoolParameter]
            public bool slowParam = false;

            [BoolParameter]
            public bool fastParam = true;
        }

        class DateTimeAlgo : QCAlgorithm
        {
            [DateTimeParameter("20150101", "20150105")]
            public DateTime slowParam = new DateTime();

            [DateTimeParameter("20150201", "20150205")]
            public DateTime fastParam = new DateTime();
        }

        class DoubleAlgo : QCAlgorithm
        {
            [DoubleParameter(5, 10)]
            public decimal slowParam = 2;

            [DoubleParameter(13, 20, 2)]
            public decimal fastParam = 10;
        }
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void LoadIntParameter()
        {
            //LeanEngineSystemHandlers leanEngineSystemHandlers = null;
            //LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers = null;
            //bool liveMode = false;

            //var mother = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);
            //var algoMother = mother.AlgorithmHandlers.Setup.CreateAlgorithmInstance(".", job.Language);
            //var permutationsList = QCAlgorithm.ExtractPermutations(algoMother);
        }

        [Test]
        public void LoadDecimalParameter()
        {
        }

        [Test]
        public void LoadDateTimeParameter()
        {

        }

        [Test]
        public void LoadBoolParameter()
        {

        }
    }
}
