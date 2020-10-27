using System;
using Newtonsoft.Json;

namespace QuantConnect.Optimizer
{
    public class OptimizationEstimate
    {
        /// <summary>
        /// Total number of backtests, approximately
        /// </summary>
        [JsonProperty("totalBacktest")]
        public int TotalBacktest { get; set; }

        /// <summary>
        /// Number of completed backtests
        /// </summary>
        [JsonProperty("completedBacktest")]
        public int CompletedBacktest { get; set; }

        /// <summary>
        /// Number of failed backtests
        /// </summary>
        [JsonProperty("failedBacktest")]
        public int FailedBacktest { get; set; }

        /// <summary>
        /// Number of running backtests
        /// </summary>
        [JsonProperty("runningBacktest")]
        public int RunningBacktest { get; set; }

        /// <summary>
        /// Number of backtests in queue
        /// </summary>
        [JsonProperty("inQueueBacktest")]
        public int InQueueBacktest { get; set; }

        /// <summary>
        /// Indicates backtest average duration; (start - now) / CompletedBacktest
        /// </summary>
        [JsonProperty("averageBacktest")]
        public TimeSpan AverageBacktest { get; set; }

        public override string ToString()
        {
            return $"TotalBacktest: {TotalBacktest} \r\n" +
                $"CompletedBacktest: {CompletedBacktest} \r\n" +
                $"FailedBacktest: {FailedBacktest} \r\n" +
                $"RunningBacktest: {RunningBacktest} \r\n" +
                $"InQueueBacktest: {InQueueBacktest} \r\n" +
                $"AverageBacktest: {AverageBacktest}";
        }
    }
}
