using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Class that uses consolidators to aggregate tick data data
    /// </summary>
    public abstract class TickAggregatingConsolidator
    {
        /// <summary>
        /// The consolidator used to aggregate data from
        /// higher resolutions to data in lower resolutions
        /// </summary>
        public abstract IDataConsolidator Consolidator { get; }

        /// <summary>
        /// The consolidated data
        /// </summary>
        public List<BaseData> Consolidated { get; protected set; }

        /// <summary>
        /// The resolution that the data is being aggregated into
        /// </summary>
        public Resolution Resolution { get; protected set; }

        /// <summary>
        /// This method is intended to return all of the
        /// aggregated data and the bar that the consolidator is working on
        /// </summary>
        public abstract List<BaseData> Flush();
    }

    /// <summary>
    /// Use <see cref="TickQuoteBarConsolidator"/> to consolidate quote ticks into a specified resolution
    /// </summary>
    public class QuoteTickAggregatingConsolidator : TickAggregatingConsolidator
    {
        public override IDataConsolidator Consolidator => _consolidator;

        private readonly TickQuoteBarConsolidator _consolidator;

        public QuoteTickAggregatingConsolidator(Resolution resolution)
        {
            Resolution = resolution;
            Consolidated = new List<BaseData>();
            _consolidator = new TickQuoteBarConsolidator(resolution.ToTimeSpan());
            _consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated);
            };
        }

        /// <summary>
        /// Return all the consolidated data as well as the
        /// bar the consolidator is currently working on
        /// </summary>
        public override List<BaseData> Flush()
        {
            // Add the last bar
            Consolidated.Add(Consolidator.WorkingData as QuoteBar);
            return Consolidated;
        }
    }

    /// <summary>
    /// Use <see cref="TickQuoteBarConsolidator"/> to consolidate trade ticks into a specified resolution
    /// </summary>
    internal class TradeTickAggregatingConsolidator : TickAggregatingConsolidator
    {
        public override IDataConsolidator Consolidator => _consolidator;

        private readonly TickConsolidator _consolidator;

        public TradeTickAggregatingConsolidator(Resolution resolution)
        {
            Resolution = resolution;
            Consolidated = new List<BaseData>();
            _consolidator = new TickConsolidator(resolution.ToTimeSpan());
            _consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated);
            };
        }

        /// <summary>
        /// Return all the consolidated data as well as the
        /// bar the consolidator is currently working on
        /// </summary>
        public override List<BaseData> Flush()
        {
            // Add the last bar
            Consolidated.Add(Consolidator.WorkingData as TradeBar);
            return Consolidated;
        }
    }
}
