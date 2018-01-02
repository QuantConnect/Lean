using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Class that uses consolidators to aggregate tick data data
    /// </summary>
    public abstract class TickAggregator
    {
        protected TickAggregator(Resolution resolution)
        {
            Resolution = resolution;
        }

        /// <summary>
        /// The consolidator used to aggregate data from
        /// higher resolutions to data in lower resolutions
        /// </summary>
        public IDataConsolidator Consolidator { get; protected set; }

        /// <summary>
        /// The consolidated data
        /// </summary>
        public List<BaseData> Consolidated { get; protected set; }

        /// <summary>
        /// The resolution that the data is being aggregated into
        /// </summary>
        public Resolution Resolution { get; }

        /// <summary>
        /// Return all the consolidated data as well as the
        /// bar the consolidator is currently working on
        /// </summary>
        public List<BaseData> Flush()
        {
            return new List<BaseData>(Consolidated) { Consolidator.WorkingData as BaseData };
        }
    }

    /// <summary>
    /// Use <see cref="TickQuoteBarConsolidator"/> to consolidate quote ticks into a specified resolution
    /// </summary>
    public class QuoteTickAggregator : TickAggregator
    {
        public QuoteTickAggregator(Resolution resolution)
            : base(resolution)
        {
            Consolidated = new List<BaseData>();
            Consolidator = new TickQuoteBarConsolidator(resolution.ToTimeSpan());
            Consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated as QuoteBar);
            };
        }
    }

    /// <summary>
    /// Use <see cref="TickQuoteBarConsolidator"/> to consolidate trade ticks into a specified resolution
    /// </summary>
    public class TradeTickAggregator : TickAggregator
    {
        public TradeTickAggregator(Resolution resolution)
            : base(resolution)
        {
            Consolidated = new List<BaseData>();
            Consolidator = new TickConsolidator(resolution.ToTimeSpan());
            Consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated as TradeBar);
            };
        }
    }
}
