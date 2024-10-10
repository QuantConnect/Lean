using NUnit.Framework;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class NHNLIndicatorTests
    {
        // 1. Test for proper initialization
        [Test]
        public void InitializesProperly()
        {
            var NhNl = new NHNLIndicator("NHNL", 52);
            Assert.AreEqual(52, NhNl.Period);
            Assert.IsFalse(NhNl.IsReady);
        }

        // 2. Test indicator values using a small data set
        [Test]
        public void ComputesCorrectly()
        {
            var NhNl = new NHNLIndicator("NHNL", 10);

            // Add some sample data. You'll need to add the right MarketData types that your indicator consumes.
            NhNl.Update(new IndicatorDataPoint(DateTime.Now, 5.0m));
            NhNl.Update(new IndicatorDataPoint(DateTime.Now.AddSeconds(1), 10.0m));
            NhNl.Update(new IndicatorDataPoint(DateTime.Now.AddSeconds(2), 8.0m));
            
            Assert.IsTrue(NhNl.IsReady);
            Assert.AreEqual(1, NhNl.Current.Value); // Expected Value
        }

    // 3. Test edge cases, such as no data or very few data points
    [Test]
    public void HandlesNoData()
    {
        var NhNl = new NHNLIndicator("NHNL", 10);

        Assert.AreEqual(0, NhNl.Current.Value);
    }

    // 4. Test for correct resetting behavior
    [Test]
    public void ResetsProperly()
    {
        var NhNl.Update(new Indicator("NHNL", 10);

        NhNl.Update(new IndicatorDataPoint(DateTime.Now, 5.0m));
        Assert.IsTrue(NhNl.IsReady);

        NhNl.Reset();
        Assert.AreEqual(0, NhNl.Current.Value);
        Assert.IsFalse(NhNl.IsReady);
    }
}
