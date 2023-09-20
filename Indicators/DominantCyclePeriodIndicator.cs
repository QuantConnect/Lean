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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Dominant Short-Cycle Indicator is excellent for tracking a regular occurring
    /// cycle of between 10 and 20 bars in length. The indicator works about as well for
    /// intraday timeframes as it does for the daily and weekly charts. I like it because
    /// it is fairly smooth, but yet also turns quickly after cycle bottoms or tops - about
    /// as much as you could ask for in any such indicator.  
    /// </summary>
    //public class DominantCyclePeriodIndicator : BarIndicator, IIndicatorWarmUpPeriodProvider
    public class DominantCyclePeriodIndicator : WindowIndicator<TradeBar>, IIndicatorWarmUpPeriodProvider
    {
      
        public override bool IsReady => true;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }


        /// <summary>
        /// Creates a new AverageTrueRange indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The warm up period</param>
        public DominantCyclePeriodIndicator(string name, int period) : base(name, period)
        {
            WarmUpPeriod = period;
        }

        /// <summary>
        /// 
        /// </summary>
        public DominantCyclePeriodIndicator() : this("HT_DCPERIOD()", 30)
        {
        }

        /// <summary>
        /// Computes the Dominant Cycle Period from the last n period of trade bars
        /// </summary>
        /// <param name="window">The trade bar window</param>
        /// <param name="input">The current trade bar</param>
        /// <returns>The Dominant Cycle Period Value</returns>
        public static decimal ComputeIndicator(
            IReadOnlyWindow<TradeBar> window,
            TradeBar input)
        {
            System.Diagnostics.Debug.WriteLine("window 0: " + window[0].Time +
                " window count: " + window[window.Count - 1].Time);
            System.Diagnostics.Debug.WriteLine("input " + input.Time);

            double[] inReal = new double[window.Count];
            double[] outReal = new double[window.Count];

            for (int wc = 0; wc < window.Count; wc++)
            {
                inReal[wc] = (double)window[wc].Close;
            }

            int startIdx = 15;
            int endIdx = window.Count - 1;
            int outBegIdx = 0;
            int outNBElement = 0;

            int outIdx, i;
            int lookbackTotal, today;
            double tempReal, tempReal2;

            double adjustedPrevPeriod, period;

            /* Variable used for the price smoother (a weighted moving average). */
            int trailingWMAIdx;
            double periodWMASum, periodWMASub, trailingWMAValue;
            double smoothedValue;

            /* Variables used for the Hilbert Transormation */
            double a = 0.0962;
            double b = 0.5769;
            double hilbertTempReal;
            int hilbertIdx;

            // HILBERT_VARIABLES(detrender);
            double[] detrender_Odd = new double[3];
            double[] detrender_Even = new double[3];
            double detrender;
            double prev_detrender_Odd;
            double prev_detrender_Even;
            double prev_detrender_input_Odd;
            double prev_detrender_input_Even;

            // HILBERT_VARIABLES(Q1);
            double[] Q1_Odd = new double[3];
            double[] Q1_Even = new double[3];
            double Q1;
            double prev_Q1_Odd;
            double prev_Q1_Even;
            double prev_Q1_input_Odd;
            double prev_Q1_input_Even;

            // HILBERT_VARIABLES(jI);
            double[] jI_Odd = new double[3];
            double[] jI_Even = new double[3];
            double jI;
            double prev_jI_Odd;
            double prev_jI_Even;
            double prev_jI_input_Odd;
            double prev_jI_input_Even;

            // HILBERT_VARIABLES(jQ);
            double[] jQ_Odd = new double[3];
            double[] jQ_Even = new double[3];
            double jQ;
            double prev_jQ_Odd;
            double prev_jQ_Even;
            double prev_jQ_input_Odd;
            double prev_jQ_input_Even;

            double Q2, I2, prevQ2, prevI2, Re, Im;

            double I1ForOddPrev2, I1ForOddPrev3;
            double I1ForEvenPrev2, I1ForEvenPrev3;

            double rad2Deg;

            double todayValue, smoothPeriod;

            /* Insert TA function code here. */
            /* Constant */
            rad2Deg = 180.0 / (4.0 * Math.Atan(1));

            /* Identify the minimum number of price bar needed
             * to calculate at least one output.
             */
            // lookbackTotal = 32 + TA_GLOBALS_UNSTABLE_PERIOD(TA_FUNC_UNST_HT_DCPERIOD, HtDcPeriod);
            lookbackTotal = 10;

            System.Diagnostics.Debug.WriteLine("startIdx: " + startIdx + " endIdx: " + endIdx);

            /* Move up the start index if there is not
             * enough initial data.
             */
            //if (startIdx < lookbackTotal)
            //    startIdx = lookbackTotal;

            /* Make sure there is still something to evaluate. */
            if (startIdx > endIdx)
            {
                // VALUE_HANDLE_DEREF_TO_ZERO(outBegIdx);
                outBegIdx = 0;
                // VALUE_HANDLE_DEREF_TO_ZERO(outNBElement);
                outNBElement = 0;
                // return ENUM_VALUE(RetCode, TA_SUCCESS, Success);
                return 0;
            }

            //VALUE_HANDLE_DEREF(outBegIdx) = startIdx;
            outBegIdx = startIdx;

            /* Initialize the price smoother, which is simply a weighted
             * moving average of the price.
             * To understand this algorithm, I strongly suggest to understand
             * first how TA_WMA is done.
             */
            trailingWMAIdx = startIdx - lookbackTotal;
            today = trailingWMAIdx;

            /* Initialization is same as WMA, except loop is unrolled
             * for speed optimization.
             */
            tempReal = inReal[today++];
            periodWMASub = tempReal;
            periodWMASum = tempReal;
            tempReal = inReal[today++];
            periodWMASub += tempReal;
            periodWMASum += tempReal * 2.0;
            tempReal = inReal[today++];
            periodWMASub += tempReal;
            periodWMASum += tempReal * 3.0;

            trailingWMAValue = 0.0;

            /* Subsequent WMA value are evaluated by using
             * the DO_PRICE_WMA macro.
             */
            // #define DO_PRICE_WMA(varNewPrice,varToStoreSmoothedValue) { \
            //             periodWMASub += varNewPrice; \
            //             periodWMASub -= trailingWMAValue; \
            //             periodWMASum += varNewPrice * 4.0; \
            //             trailingWMAValue = inReal[trailingWMAIdx++]; \
            //             varToStoreSmoothedValue = periodWMASum * 0.1; \
            //             periodWMASum -= periodWMASub; \
            //         }

            i = 9;
            do
            {
                tempReal = inReal[today++];

                // DO_PRICE_WMA(tempReal, smoothedValue);
                periodWMASub += tempReal;
                periodWMASub -= trailingWMAValue;
                periodWMASum += tempReal * 4.0;
                trailingWMAValue = inReal[trailingWMAIdx++];
                smoothedValue = periodWMASum * 0.1;
                periodWMASum -= periodWMASub;
            }

            while (--i != 0);

            /* Initialize the circular buffers used by the hilbert
             * transform logic.
             * A buffer is used for odd day and another for even days.
             * This minimize the number of memory access and floating point
             * operations needed (note also that by using static circular buffer,
             * no large dynamic memory allocation is needed for storing
             * intermediate calculation!).
             */
            hilbertIdx = 0;

            // INIT_HILBERT_VARIABLES(detrender);
            detrender_Odd[0] = 0.0;
            detrender_Odd[1] = 0.0;
            detrender_Odd[2] = 0.0;
            detrender_Even[0] = 0.0;
            detrender_Even[1] = 0.0;
            detrender_Even[2] = 0.0;
            detrender = 0.0;
            prev_detrender_Odd = 0.0;
            prev_detrender_Even = 0.0;
            prev_detrender_input_Odd = 0.0;
            prev_detrender_input_Even = 0.0;

            // INIT_HILBERT_VARIABLES(Q1);
            Q1_Odd[0] = 0.0;
            Q1_Odd[1] = 0.0;
            Q1_Odd[2] = 0.0;
            Q1_Even[0] = 0.0;
            Q1_Even[1] = 0.0;
            Q1_Even[2] = 0.0;
            Q1 = 0.0;
            prev_Q1_Odd = 0.0;
            prev_Q1_Even = 0.0;
            prev_Q1_input_Odd = 0.0;
            prev_Q1_input_Even = 0.0;

            // INIT_HILBERT_VARIABLES(jI);
            jI_Odd[0] = 0.0;
            jI_Odd[1] = 0.0;
            jI_Odd[2] = 0.0;
            jI_Even[0] = 0.0;
            jI_Even[1] = 0.0;
            jI_Even[2] = 0.0;
            jI = 0.0;
            prev_jI_Odd = 0.0;
            prev_jI_Even = 0.0;
            prev_jI_input_Odd = 0.0;
            prev_jI_input_Even = 0.0;

            // INIT_HILBERT_VARIABLES(jQ);
            jQ_Odd[0] = 0.0;
            jQ_Odd[1] = 0.0;
            jQ_Odd[2] = 0.0;
            jQ_Even[0] = 0.0;
            jQ_Even[1] = 0.0;
            jQ_Even[2] = 0.0;
            jQ = 0.0;
            prev_jQ_Odd = 0.0;
            prev_jQ_Even = 0.0;
            prev_jQ_input_Odd = 0.0;
            prev_jQ_input_Even = 0.0;

            period = 0.0;
            outIdx = 0;

            prevI2 = prevQ2 = 0.0;
            Re = Im = 0.0;
            I1ForOddPrev3 = I1ForEvenPrev3 = 0.0;
            I1ForOddPrev2 = I1ForEvenPrev2 = 0.0;
            smoothPeriod = 0.0;

            /* The code is speed optimized and is most likely very
             * hard to follow if you do not already know well the
             * original algorithm.
             * To understadn better, it is strongly suggested to look
             * first at the Excel implementation in "test_MAMA.xls" included
             * in this package.
             */
            while (today <= endIdx)
            {
                adjustedPrevPeriod = (0.075 * period) + 0.54;

                todayValue = inReal[today];

                // DO_PRICE_WMA(todayValue, smoothedValue);
                periodWMASub += todayValue;
                periodWMASub -= trailingWMAValue;
                periodWMASum += todayValue * 4.0;
                trailingWMAValue = inReal[trailingWMAIdx++];
                smoothedValue = periodWMASum * 0.1;
                periodWMASum -= periodWMASub;

                if ((today % 2) == 0)
                {
                    /* Do the Hilbert Transforms for even price bar */

                    // DO_HILBERT_EVEN(detrender, smoothedValue);
                    hilbertTempReal = a * smoothedValue;
                    detrender = -detrender_Even[hilbertIdx];
                    detrender_Even[hilbertIdx] = hilbertTempReal;
                    detrender += hilbertTempReal;
                    detrender -= prev_detrender_Even;
                    prev_detrender_Even = b * prev_detrender_input_Even;
                    detrender += prev_detrender_Even;
                    prev_detrender_input_Even = smoothedValue;
                    detrender *= adjustedPrevPeriod;

                    // DO_HILBERT_EVEN(Q1, detrender);
                    hilbertTempReal = a * detrender;
                    Q1 = -Q1_Even[hilbertIdx];
                    Q1_Even[hilbertIdx] = hilbertTempReal;
                    Q1 += hilbertTempReal;
                    Q1 -= prev_Q1_Even;
                    prev_Q1_Even = b * prev_Q1_input_Even;
                    Q1 += prev_Q1_Even;
                    prev_Q1_input_Even = detrender;
                    Q1 *= adjustedPrevPeriod;

                    // DO_HILBERT_EVEN(jI, I1ForEvenPrev3);
                    hilbertTempReal = a * I1ForEvenPrev3;
                    jI = -jI_Even[hilbertIdx];
                    jI_Even[hilbertIdx] = hilbertTempReal;
                    jI += hilbertTempReal;
                    jI -= prev_jI_Even;
                    prev_jI_Even = b * prev_jI_input_Even;
                    jI += prev_jI_Even;
                    prev_jI_input_Even = I1ForEvenPrev3;
                    jI *= adjustedPrevPeriod;

                    // DO_HILBERT_EVEN(jQ, Q1);
                    hilbertTempReal = a * Q1;
                    jQ = -jQ_Even[hilbertIdx];
                    jQ_Even[hilbertIdx] = hilbertTempReal;
                    jQ += hilbertTempReal;
                    jQ -= prev_jQ_Even;
                    prev_jQ_Even = b * prev_jQ_input_Even;
                    jQ += prev_jQ_Even;
                    prev_jQ_input_Even = Q1;
                    jQ *= adjustedPrevPeriod;

                    if (++hilbertIdx == 3)
                        hilbertIdx = 0;

                    Q2 = (0.2 * (Q1 + jI)) + (0.8 * prevQ2);
                    I2 = (0.2 * (I1ForEvenPrev3 - jQ)) + (0.8 * prevI2);

                    /* The variable I1 is the detrender delayed for
                     * 3 price bars.
                     *
                     * Save the current detrender value for being
                     * used by the "odd" logic later.
                     */
                    I1ForOddPrev3 = I1ForOddPrev2;
                    I1ForOddPrev2 = detrender;
                }
                else
                {
                    /* Do the Hilbert Transforms for odd price bar */

                    // DO_HILBERT_ODD(detrender, smoothedValue);
                    hilbertTempReal = a * smoothedValue;
                    detrender = -detrender_Odd[hilbertIdx];
                    detrender_Odd[hilbertIdx] = hilbertTempReal;
                    detrender += hilbertTempReal;
                    detrender -= prev_detrender_Odd;
                    prev_detrender_Odd = b * prev_detrender_input_Odd;
                    detrender += prev_detrender_Odd;
                    prev_detrender_input_Odd = smoothedValue;
                    detrender *= adjustedPrevPeriod;

                    // DO_HILBERT_ODD(Q1, detrender);
                    hilbertTempReal = a * detrender;
                    Q1 = -Q1_Odd[hilbertIdx];
                    Q1_Odd[hilbertIdx] = hilbertTempReal;
                    Q1 += hilbertTempReal;
                    Q1 -= prev_Q1_Odd;
                    prev_Q1_Odd = b * prev_Q1_input_Odd;
                    Q1 += prev_Q1_Odd;
                    prev_Q1_input_Odd = detrender;
                    Q1 *= adjustedPrevPeriod;

                    // DO_HILBERT_ODD(jI, I1ForOddPrev3);
                    hilbertTempReal = a * I1ForOddPrev3;
                    jI = -jI_Odd[hilbertIdx];
                    jI_Odd[hilbertIdx] = hilbertTempReal;
                    jI += hilbertTempReal;
                    jI -= prev_jI_Odd;
                    prev_jI_Odd = b * prev_jI_input_Odd;
                    jI += prev_jI_Odd;
                    prev_jI_input_Odd = I1ForOddPrev3;
                    jI *= adjustedPrevPeriod;

                    // DO_HILBERT_ODD(jQ, Q1);
                    hilbertTempReal = a * Q1;
                    jQ = -jQ_Odd[hilbertIdx];
                    jQ_Odd[hilbertIdx] = hilbertTempReal;
                    jQ += hilbertTempReal;
                    jQ -= prev_jQ_Odd;
                    prev_jQ_Odd = b * prev_jQ_input_Odd;
                    jQ += prev_jQ_Odd;
                    prev_jQ_input_Odd = Q1;
                    jQ *= adjustedPrevPeriod;

                    Q2 = (0.2 * (Q1 + jI)) + (0.8 * prevQ2);
                    I2 = (0.2 * (I1ForOddPrev3 - jQ)) + (0.8 * prevI2);

                    /* The varaiable I1 is the detrender delayed for
                     * 3 price bars.
                     *
                     * Save the current detrender value for being
                     * used by the "even" logic later.
                     */
                    I1ForEvenPrev3 = I1ForEvenPrev2;
                    I1ForEvenPrev2 = detrender;
                }

                /* Adjust the period for next price bar */
                Re = (0.2 * ((I2 * prevI2) + (Q2 * prevQ2))) + (0.8 * Re);
                Im = (0.2 * ((I2 * prevQ2) - (Q2 * prevI2))) + (0.8 * Im);
                prevQ2 = Q2;
                prevI2 = I2;
                tempReal = period;
                if ((Im != 0.0) && (Re != 0.0))
                    period = 360.0 / (Math.Atan(Im / Re) * rad2Deg);
                tempReal2 = 1.5 * tempReal;
                if (period > tempReal2)
                    period = tempReal2;
                tempReal2 = 0.67 * tempReal;
                if (period < tempReal2)
                    period = tempReal2;
                if (period < 6)
                    period = 6;
                else if (period > 50)
                    period = 50;
                period = (0.2 * period) + (0.8 * tempReal);

                smoothPeriod = (0.33 * period) + (0.67 * smoothPeriod);

                if (today >= startIdx)
                {
                    outReal[outIdx++] = smoothPeriod;
                }

                /* Ooof... let's do the next price bar now! */
                today++;
            }

            // VALUE_HANDLE_DEREF(outNBElement) = outIdx;
            outNBElement = outIdx;

            // return ENUM_VALUE(RetCode, TA_SUCCESS, Success);
            //return 0;

            for (int k = 0; k < outReal.Length; k++)
            {
                System.Diagnostics.Debug.WriteLine(k + " th outReal is " + outReal[k]);
            }

            return (decimal)outReal[outReal.Length - 1];
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window">The trade bar window</param>
        /// <param name="input">The current trade bar</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(
            IReadOnlyWindow<TradeBar> window,
            TradeBar input)
        {
            System.Diagnostics.Debug.WriteLine("DominantCyclePeriodIndicator ComputeNextValue Window: " + window.Count);

            if (window.Count > 20)
            {
                decimal decResult = ComputeIndicator(window, input);

                System.Diagnostics.Debug.WriteLine("Indicator Result for " + window.Count + " is " + decResult);
            }

            return 0;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }
    }
}
