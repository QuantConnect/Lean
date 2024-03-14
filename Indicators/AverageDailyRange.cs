using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class AverageDailyRange : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly IndicatorBase<IndicatorDataPoint> _smoother;

        public IndicatorBase<IBaseDataBar> DailyRange { get; }

        public override bool IsReady => _smoother.IsReady;

        public int WarmUpPeriod { get; }

        public AverageDailyRange(string name, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            WarmUpPeriod = period;

            _smoother = movingAverageType.AsIndicator($"{name}_{movingAverageType}", period);

            DailyRange = new FunctionalIndicator<IBaseDataBar>(name + "_DailyRange", currentBar =>
            {
                var nextValue = ComputeDailyRange(currentBar);
                return nextValue;
            }
            , dailyRangeIndicator => dailyRangeIndicator.Samples >= 1
            );
        }

        public AverageDailyRange(int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"ADR({period})", period, movingAverageType)
        {
        }

        public static decimal ComputeDailyRange(IBaseDataBar current)
        {
            return current.High - current.Low;
        }

        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            DailyRange.Update(input);
            _smoother.Update(input.Time, DailyRange.Current.Value);

            // Round off the ADR value to the desired precision
            decimal roundedAverageDailyRange = Math.Round(_smoother.Current.Value, 6);

            return roundedAverageDailyRange;
        }

        public override void Reset()
        {
            _smoother.Reset();
            DailyRange.Reset();
            base.Reset();
        }
    }
}
