using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    class BuyingPowerModelTests
    {
        // The only instance in which this function is used is when
        // (Math.Abs(currentOrderMargin) > Math.Abs(targetMargin))
        [TestCase(-1000, -900, 25)]
        [TestCase(-900, -880, 25)]
        [TestCase(1200, 1050, 25)]
        [TestCase(1225, 1212, 25)]
        public void OrderAdjustmentCalculation(decimal currentOrderMargin, decimal targetMargin, decimal perUnitMargin)
        {
            // Determine the adjustment to get us to our target margin and apply it
            var lotSize = 1m;
            var orderAdjustment =
                BuyingPowerModel.GetAmountToAdjustOrderQuantity(currentOrderMargin, targetMargin, perUnitMargin, lotSize);

            // Apply the change in margin
            var resultMargin = currentOrderMargin - (orderAdjustment * perUnitMargin);

            // Assert after our adjustment we have met our target condition
            Assert.IsTrue(Math.Abs(resultMargin) <= Math.Abs(targetMargin));
        }
    }
}
