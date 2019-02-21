using System;
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
        protected TickAggregator(Resolution resolution, TickType tickType)
        {
            TickType = tickType;
            Resolution = resolution;
        }

        /// <summary>
        /// Gets the tick type of the consolidator
        /// </summary>
        public TickType TickType { get; protected set; }

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
        /// Updates the consolidator with the specified bar.
        /// </summary>
        /// <param name="data">The latest data observation.</param>
        public virtual void Update(BaseData data)
        {
            Consolidator.Update(data);
        }

        /// <summary>
        /// Return all the consolidated data as well as the
        /// bar the consolidator is currently working on
        /// </summary>
        public List<BaseData> Flush()
        {
            var data = new List<BaseData>(Consolidated);
            if (Consolidator.WorkingData != null)
            {
                data.Add(Consolidator.WorkingData as BaseData);
            }

            return data;
        }

        /// <summary>
        /// Creates the correct <see cref="TickAggregator"/> instances for the specified tick types and resolution.
        /// <see cref="QuantConnect.TickType.OpenInterest"/> will ignore <paramref name="resolution"/> and use <see cref="QuantConnect.Resolution.Daily"/>
        /// </summary>
        public static IEnumerable<TickAggregator> ForTickTypes(Resolution resolution, params TickType[] tickTypes)
        {
            if (resolution == Resolution.Tick)
            {
                foreach (var tickType in tickTypes)
                {
                    // OI is special
                    if (tickType == TickType.OpenInterest)
                    {
                        yield return new OpenInterestTickAggregator();
                        continue;
                    }

                    yield return new IdentityTickAggregator(tickType);
                }

                yield break;
            }

            foreach (var tickType in tickTypes)
            {
                switch (tickType)
                {
                    case TickType.Trade:
                        yield return new TradeTickAggregator(resolution);
                        break;

                    case TickType.Quote:
                        yield return new QuoteTickAggregator(resolution);
                        break;

                    case TickType.OpenInterest:
                        yield return new OpenInterestTickAggregator();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(tickType), tickType, null);
                }
            }
        }
    }

    /// <summary>
    /// Use <see cref="TickQuoteBarConsolidator"/> to consolidate quote ticks into a specified resolution
    /// </summary>
    public class QuoteTickAggregator : TickAggregator
    {
        public QuoteTickAggregator(Resolution resolution)
            : base(resolution, TickType.Quote)
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
            : base(resolution, TickType.Trade)
        {
            Consolidated = new List<BaseData>();
            Consolidator = new TickConsolidator(resolution.ToTimeSpan());
            Consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated as TradeBar);
            };
        }
    }

    /// <summary>
    /// Use <see cref="OpenInterestConsolidator"/> to consolidate open interest ticks daily.
    /// </summary>
    public class OpenInterestTickAggregator : TickAggregator
    {
        public OpenInterestTickAggregator()
            : base(Resolution.Daily, TickType.OpenInterest)
        {
            Consolidated = new List<BaseData>();
            Consolidator = new OpenInterestConsolidator(Time.OneDay);
            Consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated as OpenInterest);
            };
        }
    }

    /// <summary>
    /// Use <see cref="IdentityDataConsolidator{T}"/> to yield ticks unmodified into the consolidated data collection
    /// </summary>
    public class IdentityTickAggregator : TickAggregator
    {
        public IdentityTickAggregator(TickType tickType)
            : base(Resolution.Tick, tickType)
        {
            Consolidated = new List<BaseData>();
            Consolidator = FilteredIdentityDataConsolidator.ForTickType(tickType);
            Consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Consolidated.Add(consolidated as Tick);
            };
        }
    }
}
