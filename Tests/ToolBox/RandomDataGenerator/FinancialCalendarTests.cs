using NUnit.Framework;
using QuantConnect.ToolBox.RandomDataGenerator;
using System;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class FinancialCalendarTests
    {
        [Test]
        [TestCase(1, 2)]
        [TestCase(3, 4)]
        [TestCase(12, 1)]
        public void NextMonthTest(int currentMonth, int nextMonth)
        {
            Assert.AreEqual(currentMonth.NextMonth(), nextMonth);
        }

        [Test]
        [TestCase(1, 12)]
        [TestCase(12, 11)]
        [TestCase(7, 6)]
        public void PreviousMonthTest(int currentMonth, int previousMonth)
        {
            Assert.AreEqual(currentMonth.PreviousMonth(), previousMonth);
        }

        [Test]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 6)]
        [TestCase(4, 6)]
        [TestCase(5, 6)]
        [TestCase(6, 9)]
        [TestCase(7, 9)]
        [TestCase(8, 9)]
        [TestCase(9, 12)]
        [TestCase(10, 12)]
        [TestCase(11, 12)]
        [TestCase(12, 3)]
        public void NextQuarterTest(int currentMonth, int nextQuarter)
        {
            Assert.AreEqual(currentMonth.NextQuarter(), nextQuarter);
        }

        [Test]
        [TestCase(1, 12)]
        [TestCase(2, 12)]
        [TestCase(3, 12)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        [TestCase(7, 6)]
        [TestCase(8, 6)]
        [TestCase(9, 6)]
        [TestCase(10, 9)]
        [TestCase(11, 9)]
        [TestCase(12, 9)]
        public void PreviousQuarterTest(int currentMonth, int previousQuarter)
        {
            Assert.AreEqual(currentMonth.PreviousQuarter(), previousQuarter);
        }
        
        [Test]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 7)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        [TestCase(7, 10)]
        [TestCase(8, 10)]
        [TestCase(9, 10)]
        [TestCase(10, 1)]
        [TestCase(11, 1)]
        [TestCase(12, 1)]
        public void NextFinancialStatementTest(int currentMonth, int nextQuarterReport)
        {
            Assert.AreEqual(currentMonth.NextFinancialStatement(), nextQuarterReport);
        }
        
        [Test]
        [TestCase(1, 10)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 4)]
        [TestCase(6, 4)]
        [TestCase(7, 4)]
        [TestCase(8, 7)]
        [TestCase(9, 7)]
        [TestCase(10, 7)]
        [TestCase(11, 10)]
        [TestCase(12, 10)]
        public void PreviousFinancialStatementTest(int currentMonth, int previousQuarterReport)
        {
            Assert.AreEqual(currentMonth.PreviousFinancialStatement(), previousQuarterReport);
        }

        [Test]
        public void IsFinancialQuarter()
        {
            Assert.False(1.IsFinancialQuarter());
            Assert.False(2.IsFinancialQuarter());
            Assert.True(3.IsFinancialQuarter());
            Assert.False(4.IsFinancialQuarter());
            Assert.False(5.IsFinancialQuarter());
            Assert.True(6.IsFinancialQuarter());
            Assert.False(7.IsFinancialQuarter());
            Assert.False(8.IsFinancialQuarter());
            Assert.True(9.IsFinancialQuarter());
            Assert.False(10.IsFinancialQuarter());
            Assert.False(11.IsFinancialQuarter());
            Assert.True(12.IsFinancialQuarter());
        }
        
        [Test]
        public void IsFinancialStatementMonth()
        {
            Assert.True(1.IsFinancialStatementMonth());
            Assert.False(2.IsFinancialStatementMonth());
            Assert.False(3.IsFinancialStatementMonth());
            Assert.True(4.IsFinancialStatementMonth());
            Assert.False(5.IsFinancialStatementMonth());
            Assert.False(6.IsFinancialStatementMonth());
            Assert.True(7.IsFinancialStatementMonth());
            Assert.False(8.IsFinancialStatementMonth());
            Assert.False(9.IsFinancialStatementMonth());
            Assert.True(10.IsFinancialStatementMonth());
            Assert.False(11.IsFinancialStatementMonth());
            Assert.False(12.IsFinancialStatementMonth());
        }
    }
}
