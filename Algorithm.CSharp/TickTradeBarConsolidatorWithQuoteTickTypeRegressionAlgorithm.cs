using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests the functionality of the TickConsolidator with tick data.
    /// The SubscriptionManager.AddConsolidator method uses a Quote TickType
    /// It checks if data consolidation does not occur when the algorithm is running. If consolidation happens, a RegressionTestException is thrown.
    /// </summary>
    public class TickTradeBarConsolidatorWithQuoteTickTypeRegressionAlgorithm : TickTradeBarConsolidatorWithDefaultTickTypeRegressionAlgorithm
    {
        protected override void AddConsolidator(TickConsolidator consolidator)
        {
            SubscriptionManager.AddConsolidator(GoldFuture.Mapped, consolidator, TickType.Quote);
        }

        public override void OnEndOfAlgorithm()
        {
            if (ItWasConsolidated)
            {
                throw new RegressionTestException("TickConsolidator should not have consolidated Quote ticks.");
            }
        }
    }
}