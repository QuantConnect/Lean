using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;
using System;
using System.Linq;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomValueGeneratorTests
    {
        private const int Seed = 123456789;
        private RandomValueGenerator randomValueGenerator;

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            randomValueGenerator = new RandomValueGenerator(Seed);
        }

        [Test]
        public void NextDateTime_CreatesDateTime_WithinSpecifiedMinMax()
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek: null);

            Assert.LessOrEqual(min, dateTime);
            Assert.GreaterOrEqual(max, dateTime);
        }

        [Test]
        [TestCase(DayOfWeek.Sunday)]
        [TestCase(DayOfWeek.Monday)]
        [TestCase(DayOfWeek.Tuesday)]
        [TestCase(DayOfWeek.Wednesday)]
        [TestCase(DayOfWeek.Thursday)]
        [TestCase(DayOfWeek.Friday)]
        [TestCase(DayOfWeek.Saturday)]
        public void NextDateTime_CreatesDateTime_OnSpecifiedDayOfWeek(DayOfWeek dayOfWeek)
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek);

            Assert.AreEqual(dayOfWeek, dateTime.DayOfWeek);
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenMaxIsLessThanMin()
        {
            var min = new DateTime(2000, 01, 01);
            var max = min.AddDays(-1);
            Assert.Throws<ArgumentException>(() =>
                randomValueGenerator.NextDate(min, max, dayOfWeek: null)
            );
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenRangeIsTooSmallToProduceDateTimeOnRequestedDayOfWeek()
        {
            var min = new DateTime(2019, 01, 15);
            var max = new DateTime(2019, 01, 20);
            Assert.Throws<ArgumentException>(() =>
                // no monday between these dates, so impossible to fulfill request
                randomValueGenerator.NextDate(min, max, DayOfWeek.Monday)
            );
        }
    }
}
