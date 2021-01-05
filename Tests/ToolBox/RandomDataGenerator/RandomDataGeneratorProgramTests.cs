using System;
//using System.Linq;
using NUnit.Framework;
//using QuantConnect.Brokerages;
//using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomDataGeneratorProgramTests
    {
        [Test]
        [TestCase("2020, 1, 1", "2020, 1, 1")]
        [TestCase("2020, 1, 1", "2020, 2, 1")]
        [TestCase("2020, 1, 1", "2020, 6, 1")]
        [TestCase("2019, 1, 1", "2020, 6, 1")]
        public void NextRandomGeneratedData(DateTime start, DateTime end)
        {
            var delistIsLessThanEnd = true;
            var startIsLessThanEnd = true;
            var randomValueGenerator = new RandomValueGenerator();
            var delistDate = RandomDataGeneratorProgram.GetDelistDate(start, end, randomValueGenerator);

            //start must be less than or equal to end
            if(start > end)
            {
                startIsLessThanEnd = false;
            }

            // delistDate must be less than or equal to end
            if(delistDate > end)
            {
                delistIsLessThanEnd = false;
            }

            Assert.IsTrue(startIsLessThanEnd);
            Assert.IsTrue(delistIsLessThanEnd);
        }
    }
}
