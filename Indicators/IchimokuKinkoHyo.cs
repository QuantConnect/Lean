/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Ichimoku Kinko Hyo indicator. It consists of the following main indicators:
    /// Tenkan-sen: (Highest High + Lowest Low) / 2 for the specific period (normally 9)
    /// Kijun-sen: (Highest High + Lowest Low) / 2 for the specific period (normally 26)
    /// Senkou A Span: (Tenkan-sen + Kijun-sen )/ 2 from a specific number of periods ago (normally 26)
    /// Senkou B Span: (Highest High + Lowest Low) / 2 for the specific period (normally 52), from a specific number of periods ago (normally 26)
    /// </summary>
    public class IchimokuKinkoHyo : TradeBarIndicator
    {
        /// <summary>
        /// The Tenkan-sen component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<TradeBar> Tenkan { get; private set; }

        /// <summary>
        /// The Kijun-sen component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<TradeBar> Kijun { get; private set; }

        /// <summary>
        /// The Senkou A Span component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<TradeBar> SenkouA { get; private set; }

        /// <summary>
        /// The Senkou B Span component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<TradeBar> SenkouB { get; private set; }

        /// <summary>
        /// The Tenkan-sen Maximum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> TenkanMaximum { get; private set; }
        /// <summary>
        /// The Tenkan-sen Minimum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> TenkanMinimum { get; private set; }
        /// <summary>
        /// The Kijun-sen Maximum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> KijunMaximum { get; private set; }
        /// <summary>
        /// The Kijun-sen Minimum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> KijunMinimum { get; private set; }
        /// <summary>
        /// The Senkou B Maximum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SenkouBMaximum { get; private set; }
        /// <summary>
        /// The Senkou B Minimum component of the Ichimoku indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SenkouBMinimum { get; private set; }
        /// <summary>
        /// The Delayed Tenkan Senkou A component of the Ichimoku indicator
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> DelayedTenkanSenkouA { get; private set; }
        /// <summary>
        /// The Delayed Kijun Senkou A component of the Ichimoku indicator
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> DelayedKijunSenkouA { get; private set; }
        /// <summary>
        /// The Delayed Maximum Senkou B component of the Ichimoku indicator
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> DelayedMaximumSenkouB { get; private set; }
        /// <summary>
        /// The Delayed Minimum Senkou B component of the Ichimoku indicator
        /// </summary>
        public WindowIndicator<IndicatorDataPoint> DelayedMinimumSenkouB { get; private set; }

        /// <summary>
        /// Creates a new IchimokuKinkoHyo indicator from the specific periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="tenkanPeriod">The Tenkan-sen period</param>
        /// <param name="kijunPeriod">The Kijun-sen period</param>
        /// <param name="senkouAPeriod">The Senkou A Span period</param>
        /// <param name="senkouBPeriod">The Senkou B Span period</param>
        /// <param name="senkouADelayPeriod">The Senkou A Span delay</param>
        /// <param name="senkouBDelayPeriod">The Senkou B Span delay</param>
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

        /// <summary>
        /// Returns true if all of the sub-components of the Ichimoku indicator is ready
        /// </summary>
        public override bool IsReady
        {

            get { return Tenkan.IsReady && Kijun.IsReady && SenkouA.IsReady && SenkouB.IsReady; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
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
        
        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
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
