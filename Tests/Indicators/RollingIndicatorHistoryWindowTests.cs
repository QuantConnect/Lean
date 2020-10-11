using System;
using Moq;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RollingIndicatorHistoryWindowTests
    {
        private Mock<IIndicator> _wrappedIndicatorMock;
        private RollingIndicatorHistoryWindow _underTest;
        private const int HistorySize = 3;

        [SetUp]
        public void SetUp()
        {
            _wrappedIndicatorMock = new Mock<IIndicator>();
            _underTest = new RollingIndicatorHistoryWindow(_wrappedIndicatorMock.Object, HistorySize);
        }

        [Test]
        public void OnlyAddIfWrappedIndicatorIsReady()
        {
            _wrappedIndicatorMock.Setup(x => x.IsReady).Returns(false);
            _wrappedIndicatorMock.Raise(x => x.Updated += null, _wrappedIndicatorMock.Object, new IndicatorDataPoint(DateTime.Now, 1));

            _wrappedIndicatorMock.Setup(x => x.IsReady).Returns(true);
            _wrappedIndicatorMock.Raise(x => x.Updated += null, _wrappedIndicatorMock.Object, new IndicatorDataPoint(DateTime.Now, 2));

            CollectionAssert.AreEqual(new decimal[] {2}, _underTest);
        }

        [Test]
        public void HistorySizeIsPassedCorrectly()
        {
            _wrappedIndicatorMock.Setup(x => x.IsReady).Returns(true);

            var values = new[] {1, 2, 3, 4};
            foreach(var value in values)
            {
                _wrappedIndicatorMock.Raise(x => x.Updated += null, _wrappedIndicatorMock.Object, new IndicatorDataPoint(DateTime.Now, value));
            }

            CollectionAssert.AreEqual(new decimal[] {4, 3, 2}, _underTest);
        }

        [Test]
        public void ReturnsWrappedIndicatorCorrectly()
        {
            Assert.AreSame(_wrappedIndicatorMock.Object, _underTest.WrappedIndicator);
        }
    }
}
