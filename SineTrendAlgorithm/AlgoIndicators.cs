using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Indicators;

namespace QuantConnect
{
    public class AlgoIndicators
    {
        public BollingerBands BB;
        public SimpleMovingAverage SMA;
        public ExponentialMovingAverage EMA;
        public RelativeStrengthIndex RSI;
        public AverageTrueRange ATR;
        public StandardDeviation STD;
        public AroonOscillator AROON;
        public Momentum MOM;
        public MomentumPercent MOMP;
        public MovingAverageConvergenceDivergence MACD;
        public Minimum MIN;
        public Maximum MAX;
        public WeightedMovingAverage WMA;
    }

}
