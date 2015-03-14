using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Type capable of consolidating trade bars from any base data instance
    /// </summary>
    public class BaseDataConsolidator : TradeBarConsolidatorBase<BaseData>
    {
        /// <summary>
        /// Create a new TickConsolidator for the desired resolution
        /// </summary>
        /// <param name="resolution">The resoluton desired</param>
        /// <returns>A consolidator that produces data on the resolution interval</returns>
        public static BaseDataConsolidator FromResolution(Resolution resolution)
        {
            return new BaseDataConsolidator(resolution.ToTimeSpan());
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public BaseDataConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public BaseDataConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public BaseDataConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        protected override void AggregateBar(ref TradeBar workingBar, BaseData data)
        {
            if (workingBar == null)
            {
                workingBar = new TradeBar
                {
                    Symbol = data.Symbol,
                    Time = data.Time,
                    Close = data.Value,
                    High = data.Value,
                    Low = data.Value,
                    Open = data.Value,
                    DataType = data.DataType,
                    Value = data.Value
                };
            }
            else
            {
                //Aggregate the working bar
                workingBar.Close = data.Value;
                if (data.Value < workingBar.Low) workingBar.Low = data.Value;
                if (data.Value > workingBar.High) workingBar.High = data.Value;
            }
        }
    }
}