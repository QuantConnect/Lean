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
    class ParameterTests
    {
        #region Helpers
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
            public bool slowParam = true;

            [BoolParameter]
            public bool fastParam = true;
        }

        class DateTimeAlgo : QCAlgorithm
        {
            [DateTimeParameter("2015-01-01", "2015-01-05")]
            public DateTime slowParam = new DateTime();

            [DateTimeParameter("2015-02-07", "2015-02-10")]
            public DateTime fastParam = new DateTime();
        }

        class DecimalAlgo : QCAlgorithm
        {
            [DecimalParameter(5.3d, 10.77d)]
            public decimal slowParam = 2;

            [DecimalParameter(13.5d, 20.4d, 2.1d)]
            public decimal fastParam = 10;
        }
        #endregion

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void ReadIntParameter()
        {
            var algo = new IntAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            // 5, 6, 7, 8, 9, 10 = 6 combinations
            // 12, 14, 16, 18, 20 = 5 combinations
            // total: 6 * 4 = 30 permutations

            int x = 0;
            foreach(var permutation in permutationsList)
            {
                Console.WriteLine("PERMUTATION # {0}", ++x);
                foreach(var parameter in permutation)
                    Console.WriteLine("  -- {0}: {1}", parameter.Key, parameter.Value);
            }
            Assert.AreEqual(30, permutationsList.Count);
        }

        [Test]
        public void ReadDecimalParameter()
        {
            var algo = new DecimalAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            // six and four = 24 permutations

            int x = 0;
            foreach (var permutation in permutationsList)
            {
                Console.WriteLine("PERMUTATION # {0}", ++x);
                foreach (var parameter in permutation)
                    Console.WriteLine("  -- {0}: {1}", parameter.Key, parameter.Value);
            }
            Assert.AreEqual(24, permutationsList.Count);
        }

        [Test]
        public void ReadDateTimeParameter()
        {
            var algo = new DateTimeAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            // five and four = 20 permutations

            int x = 0;
            foreach (var permutation in permutationsList)
            {
                Console.WriteLine("PERMUTATION # {0}", ++x);
                foreach (var parameter in permutation)
                    Console.WriteLine("  -- {0}: {1}", parameter.Key, parameter.Value);
            }
            Assert.AreEqual(20, permutationsList.Count);
        }

        [Test]
        public void ReadBoolParameter()
        {
            var algo = new BoolAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            // 2 and 2 = four permutations

            int x = 0;
            foreach (var permutation in permutationsList)
            {
                Console.WriteLine("PERMUTATION # {0}", ++x);
                foreach (var parameter in permutation)
                    Console.WriteLine("  -- {0}: {1}", parameter.Key, parameter.Value);
            }
            Assert.AreEqual(4, permutationsList.Count);
        }

        [Test]
        public void AssignIntParameters()
        {
            var algo = new IntAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            QCAlgorithm.AssignParameters(permutationsList[0], algo);
            Assert.AreEqual(5, algo.slowParam);
            Assert.AreEqual(12, algo.fastParam);
        }

        [Test]
        public void AssignDecimalParameters()
        {
            var algo = new DecimalAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            QCAlgorithm.AssignParameters(permutationsList[0], algo);
            Assert.AreEqual(5.3m, algo.slowParam);
            Assert.AreEqual(13.5m, algo.fastParam);
        }

        [Test]
        public void AssignDateTimeParameters()
        {
            var algo = new DateTimeAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            QCAlgorithm.AssignParameters(permutationsList[0], algo);
            Assert.AreEqual(new DateTime(2015, 1, 1), algo.slowParam);
            Assert.AreEqual(new DateTime(2015, 2, 7), algo.fastParam);
        }

        [Test]
        public void AssignBoolParameters()
        {
            var algo = new BoolAlgo();
            var permutationsList = QCAlgorithm.ExtractPermutations(algo);
            QCAlgorithm.AssignParameters(permutationsList[0], algo);
            Assert.AreEqual(false, algo.slowParam);
            Assert.AreEqual(false, algo.fastParam);
        }
    }
}
