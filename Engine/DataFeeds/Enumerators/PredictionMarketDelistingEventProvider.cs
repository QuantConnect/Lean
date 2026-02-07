/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.PredictionMarket;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Delisting event provider for prediction market securities.
    /// Dynamically resolves the delisting date from the PredictionMarketSettlementRegistry.
    /// </summary>
    /// <remarks>
    /// This provider handles the timing issue where the close time may not be known when
    /// the subscription is first created but is registered later by the universe.
    /// On each tradable date, it checks the registry for the delisting date and emits
    /// Warning on the close date, Delisted the day after.
    /// </remarks>
    public class PredictionMarketDelistingEventProvider : DelistingEventProvider
    {
        private bool _delistedWarningEmitted;
        private bool _delistedEmitted;

        /// <summary>
        /// Check for delisting events for prediction markets
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>Delisting events if any</returns>
        public override IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            if (Config.Symbol != eventArgs.Symbol)
            {
                yield break;
            }

            // Try to resolve the delisting date from the registry on each call
            // This handles the case where the close time is registered after subscription creation
            if (!PredictionMarketSettlementRegistry.TryGetDelistingDate(eventArgs.Symbol, out var delistingDate))
            {
                // No delisting date registered yet, skip
                yield break;
            }

            // Update the delisting date reference if it changed
            if (DelistingDate.Value != delistingDate)
            {
                DelistingDate = new ReferenceWrapper<DateTime>(delistingDate);
            }

            // Emit warning on the delisting date
            if (!_delistedWarningEmitted && eventArgs.Date >= delistingDate.Date)
            {
                _delistedWarningEmitted = true;
                var price = eventArgs.LastBaseData?.Price ?? 0;
                yield return new Delisting(
                    eventArgs.Symbol,
                    delistingDate.Date,
                    price,
                    DelistingType.Warning);
            }

            // Emit delisted event the day after the delisting date
            if (!_delistedEmitted && eventArgs.Date > delistingDate)
            {
                _delistedEmitted = true;
                var price = eventArgs.LastBaseData?.Price ?? 0;
                yield return new Delisting(
                    eventArgs.Symbol,
                    delistingDate.AddDays(1),
                    price,
                    DelistingType.Delisted);
            }
        }
    }
}
