using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public interface IPriceGenerator
    {
        public decimal NextReferencePrice(
            DateTime dateTime,
            decimal referencePrice,
            decimal maximumPercentDeviation
            );

        public decimal NextValue(decimal referencePrice, DateTime referenceDate);
    }
}
