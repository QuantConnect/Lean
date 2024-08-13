using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Indicators
{
    public class Vortex : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly AverageTrueRange _atr;
        private readonly Sum _atrSum;
        private readonly Sum _plusVMSum;
        private readonly Sum _minusVMSum;
        private IBaseDataBar _previousInput;

        public Identity PlusVortex { get; private set; }
        public Identity MinusVortex { get; private set; }

        public override bool IsReady => Samples >= _period;
        public int WarmUpPeriod => _period;

        public Vortex(int period)
            : this($"VTX({period})", period)
        {
        }

        public Vortex(string name, int period)
            : base(name)
        {
            _period = period;
            _atr = new AverageTrueRange("VTX_ATR_1", 1, MovingAverageType.Simple);
            _atrSum = new Sum("ATR_SUM", period);
            _plusVMSum = new Sum("PlusVM_SUM", period);
            _minusVMSum = new Sum("MinusVM_SUM", period);

            PlusVortex = new Identity("PlusVortex");
            MinusVortex = new Identity("MinusVortex");
        }

        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_previousInput != null)
            {
                var plusVMValue = Math.Abs(input.High - _previousInput.Low);
                var minusVMValue = Math.Abs(input.Low - _previousInput.High);

                _plusVMSum.Update(input.Time, plusVMValue);
                _minusVMSum.Update(input.Time, minusVMValue);
            }

            _previousInput = input;
            _atr.Update(input);
            _atrSum.Update(input.Time, _atr.Current.Value);

            if (IsReady)
            {
                PlusVI.Update(input.Time, _plusVMSum / _atrSum);
                MinusVI.Update(input.Time, _minusVMSum / _atrSum);
            }

            return (PlusVI.Current.Value + MinusVI.Current.Value) / 2;
        }

        public override void Reset()
        {
            base.Reset();
            _atr.Reset();
            _plusVMSum.Reset();
            _minusVMSum.Reset();
            _atrSum.Reset();
            PlusVI.Reset();
            MinusVI.Reset();
            _previousInput = null;
        }
    }
}
