using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    public class IchimokuKinkoHyo : TradeBarIndicator
    {
        public int DelayedSenkouAPeriod = 26;
        public int DelayedSenkouBPeriod = 26;

        public IndicatorBase<TradeBar> Tenkan { get; private set; }
        public IndicatorBase<TradeBar> Kijun { get; private set; }
        public IndicatorBase<TradeBar> SenkouA { get; private set; }
        public IndicatorBase<TradeBar> SenkouB { get; private set; }
        public IndicatorBase<IndicatorDataPoint> TenkanMaximum { get; private set; }
        public IndicatorBase<IndicatorDataPoint> TenkanMinimum { get; private set; }
        public IndicatorBase<IndicatorDataPoint> KijunMaximum { get; private set; }
        public IndicatorBase<IndicatorDataPoint> KijunMinimum { get; private set; }
        public IndicatorBase<IndicatorDataPoint> SenkouBMaximum { get; private set; }
        public IndicatorBase<IndicatorDataPoint> SenkouBMinimum { get; private set; }
        public WindowIndicator<IndicatorDataPoint> DelayedTenkanSenkouA { get; private set; }
        public WindowIndicator<IndicatorDataPoint> DelayedKijunSenkouA { get; private set; }
        public WindowIndicator<IndicatorDataPoint> DelayedMaximumSenkouB { get; private set; }
        public WindowIndicator<IndicatorDataPoint> DelayedMinimumSenkouB { get; private set; }


        public IchimokuKinkoHyo(string name, int tenkanPeriod = 9, int kijunPeriod = 26, int senkouAPeriod = 26, int senkouBPeriod = 52, int senkouADelayPeriod = 26, int senkouBDelayPeriod = 26)
            : base(name)
        {
            TenkanMaximum = new Maximum(name + "_TenkanMax", tenkanPeriod);
            TenkanMinimum = new Minimum(name + "_TenkanMin", tenkanPeriod);
            KijunMaximum = new Maximum(name + "_KijunMax", kijunPeriod);
            KijunMinimum = new Minimum(name + "_KijunMin", kijunPeriod);
            SenkouBMaximum = new Maximum(name + "_SenkouBMaximum", senkouBPeriod);
            SenkouBMinimum = new Minimum(name + "_SenkouBMinimum", senkouBPeriod);
            DelayedTenkanSenkouA = new Delay(name + "DelayedTenkan", senkouADelayPeriod);
            DelayedKijunSenkouA = new Delay(name + "DelayedKijun", senkouADelayPeriod);
            DelayedMaximumSenkouB = new Delay(name + "DelayedMax", senkouBDelayPeriod);
            DelayedMinimumSenkouB = new Delay(name + "DelayedMin", senkouBDelayPeriod);


            SenkouA = new FunctionalIndicator<TradeBar>(
                name + "_SenkouA",
                input => computeSenkouA(senkouAPeriod, input),
                senkouA => DelayedTenkanSenkouA.IsReady && DelayedKijunSenkouA.IsReady,
                () =>
                {
                    Tenkan.Reset();
                    Kijun.Reset();
                });

            SenkouB = new FunctionalIndicator<TradeBar>(
                name + "_SenkouB",
                input => computeSenkouB(senkouBPeriod, input),
                senkouA => DelayedMaximumSenkouB.IsReady && DelayedMinimumSenkouB.IsReady,
                () =>
                {
                    Tenkan.Reset();
                    Kijun.Reset();
                });


            Tenkan = new FunctionalIndicator<TradeBar>(
                name + "_Tenkan",
                input => ComputeTenkan(tenkanPeriod, input),
                tenkan => TenkanMaximum.IsReady && TenkanMinimum.IsReady,
                () =>
                {
                    TenkanMaximum.Reset();
                    TenkanMinimum.Reset();
                });

            Kijun = new FunctionalIndicator<TradeBar>(
                name + "_Kijun",
                input => ComputeKijun(kijunPeriod, input),
                kijun => KijunMaximum.IsReady && KijunMinimum.IsReady,
                () =>
                {
                    KijunMaximum.Reset();
                    KijunMinimum.Reset();
                });
        }

        private decimal computeSenkouB(int period, TradeBar input)
        {
            var senkouB = DelayedMaximumSenkouB.Samples >= period ? (DelayedMaximumSenkouB + DelayedMinimumSenkouB) / 2 : new decimal(0.0);
            return senkouB;
        }

        private decimal computeSenkouA(int period, TradeBar input)
        {
            var senkouA = DelayedKijunSenkouA.Samples >= period ? (DelayedTenkanSenkouA + DelayedKijunSenkouA) / 2 : new decimal(0.0);
            return senkouA;
        }

        private decimal ComputeTenkan(int period, TradeBar input)
        {
            var tenkan = TenkanMaximum.Samples >= period ? (TenkanMaximum.Current.Value + TenkanMinimum.Current.Value) / 2 : new decimal(0.0);

            return tenkan;
        }

        private decimal ComputeKijun(int period, TradeBar input)
        {
            var kijun = KijunMaximum.Samples >= period ? (KijunMaximum + KijunMinimum) / 2 : new decimal(0.0);
            return kijun;
        }
        public override bool IsReady
        {

            get { return Tenkan.IsReady && Kijun.IsReady && SenkouA.IsReady && SenkouB.IsReady; }
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {


            TenkanMaximum.Update(input.Time, input.High);
            TenkanMinimum.Update(input.Time, input.Low);
            Tenkan.Update(input);
            KijunMaximum.Update(input.Time, input.High);
            KijunMinimum.Update(input.Time, input.Low);
            Kijun.Update(input);
            DelayedTenkanSenkouA.Update(input.Time, Tenkan.Current.Value);
            DelayedKijunSenkouA.Update(input.Time, Kijun.Current.Value);
            SenkouA.Update(input);
            SenkouBMaximum.Update(input.Time, input.High);
            SenkouBMinimum.Update(input.Time, input.Low);
            DelayedMaximumSenkouB.Update(input.Time, SenkouBMaximum.Current.Value);
            DelayedMinimumSenkouB.Update(input.Time, SenkouBMinimum.Current.Value);
            SenkouB.Update(input);
            return input.Close;
        }

        public override void Reset()
        {
            base.Reset();
            TenkanMaximum.Reset();
            TenkanMinimum.Reset();
            Tenkan.Reset();
            KijunMaximum.Reset();
            KijunMinimum.Reset();
            Kijun.Reset();
            DelayedTenkanSenkouA.Reset();
            DelayedKijunSenkouA.Reset();
            SenkouA.Reset();
            SenkouBMaximum.Reset();
            SenkouBMinimum.Reset();
            DelayedMaximumSenkouB.Reset();
            DelayedMinimumSenkouB.Reset();
            SenkouB.Reset();
        }
    }
}
