using System;
using NUnit.Framework;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class BuyingPowerModelTests
    {
											    // Current Order Margin 
        [TestCase(-40,25, -900, 1, -36)]   	    // -1000
        [TestCase(-36, 25, -880, 1, -35)]  	    // -900
        [TestCase(-35, 25, -900,1, -36)]   	    // -875
        [TestCase(-34, 25, -880, 1, -35)]       // -850
        [TestCase(48, 25, 1050, 1,42)]    	    // 1200
        [TestCase(49, 25, 1212, 1,  48)]   	    // 1225
        [TestCase(44, 25, 1200, 1, 48)]    	    // 1100
        [TestCase(45, 25, 1250,1, 50)]    	    // 1125
        [TestCase(80, 25, -1250, 1, -50)]       // 2000
        [TestCase(45.5, 25, 1240, 0.5, 49.5)]   // 1125
        [TestCase(45.75, 25, 1285, 0.25, 51.25)]// 1125
        public void OrderAdjustmentCalculation(decimal currentOrderSize, decimal perUnitMargin, decimal targetMargin, decimal lotSize, decimal expectedOrderSize)
        {
            var currentOrderMargin = currentOrderSize * perUnitMargin;

            // Determine the adjustment to get us to our target margin and apply it
            // Use our GetAmountToOrder for determining adjustment to reach the end goal
            var orderAdjustment =
                BuyingPowerModel.GetAmountToOrder(currentOrderMargin, targetMargin, perUnitMargin, lotSize);

            // Apply the change in margin
            var resultMargin = currentOrderMargin - (orderAdjustment * perUnitMargin);

            // Assert after our adjustment we have met our target condition
            Assert.IsTrue(Math.Abs(resultMargin) <= Math.Abs(targetMargin));

            // Verify our adjustment meets our expected order size
            var adjustOrderSize = currentOrderSize - orderAdjustment;
            Assert.AreEqual(expectedOrderSize, adjustOrderSize);
        }
    }
}
