namespace QuantConnect.Indicators
{
    /// <summary>
    /// Wraps an indicator to allow for storing a rolling window of output results from the indicator
    /// </summary>
    public class RollingIndicatorHistoryWindow : RollingWindow<decimal>
    {
        /// <summary>
        /// Indicator the window wraps 
        /// </summary>
        public IIndicator WrappedIndicator { get; }

        /// <summary>
        /// Initializes a new instance of the RollingIndicatorHistoryWindow class
        /// </summary>
        /// <param name="wrappedIndicator">The indicator to wrap</param>
        /// <param name="size">The number of results to keep in the window</param>
        public RollingIndicatorHistoryWindow(IIndicator wrappedIndicator, int size)
            : base(size)
        {
            WrappedIndicator = wrappedIndicator;
            wrappedIndicator.Updated += (sender, indicatorDataPoint) =>
            {
                if (wrappedIndicator.IsReady)
                {
                    Add(indicatorDataPoint.Value);
                }
            };
        }
    }
}
