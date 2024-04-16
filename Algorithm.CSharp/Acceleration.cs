using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class Acceleration : QCAlgorithm
    {
        //Algorithm Parameters------------------------------------------
        public int HullPeriod { get; set; } = 30; 
        public int VelocityPeriod { get; set; } = 5; 
        public int AccelerationPeriod { get; set; } = 1; 
        public int RateOfChangePeriod { get; set; } = 2; 
        public decimal VelocityBuyThreshold { get; set; } = -.25m;
        public decimal AccelerationBuyThreshold { get; set; } = -.80m;
        public decimal AccelerationSellThreshold { get; set; } = 1.6m;
        public decimal FixedStopMultiple { get; set; } = .99m;
        public decimal DisasterStopMultiple { get; set; } = .82m;
        //--------------------------------------------------------------


        //string fileName = @"D:\StockTrading\acceleration.csv";
        //StreamWriter writer;

        private string _symbol = "TQQQ_2_20_24"; 

        //Short-term velocity estimate
        RateOfChange rateOfChange;

        //Price filter -used in the estimation of velocity and acceleration
        HullMovingAverage hullMovingAverage;

        // Medium-term velocity estimate
        MomentumPercent velocity;

        //Medium-term acceleration estimate
        Momentum acceleration;
 
        //Buy Price
        decimal buyPrice;

        public override void Initialize()
        {
            //Set transaction fees to 0 
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));


            // Set backtest time range
            SetStartDate(2011, 1, 1);
            SetEndDate(2024, 2, 20);
            SetCash(100000);  //# Set Strategy Cash
             
            AddEquity(_symbol, Resolution.Daily);

            //Warmup the indicators
            SetWarmUp(TimeSpan.FromDays(250), Resolution.Daily);

            //Set benchmark comparison asset (comparing against the buy and hold strategy on the same asset
            SetBenchmark(x => Securities[_symbol].Price);

            //Initialize indicators
            hullMovingAverage = HMA(_symbol, HullPeriod, Resolution.Daily, x => ((TradeBar)x).Close);  
            velocity = MOMP(_symbol, VelocityPeriod, Resolution.Daily, x => hullMovingAverage.Current.Value);
            acceleration = MOM(_symbol, AccelerationPeriod, Resolution.Daily, x => velocity.Current.Value);
            rateOfChange = ROC(_symbol, RateOfChangePeriod, Resolution.Daily, x => ((TradeBar)x).Close);
        }

        public override void OnData(Slice slice)
        {

            // Check Warmup
            if (IsWarmingUp) return;

            Log(acceleration + " " + velocity.Current.Value + " " + slice.Time + " " + hullMovingAverage.Current.Value);


            if (!Portfolio.Invested)  //Buy Signals
            {
                // Buy on trend change - velocity up, acceleration up
                if (acceleration > AccelerationBuyThreshold && velocity > VelocityBuyThreshold)
                {
                    SetHoldings(_symbol, 1);
                }
            }
            else if (Portfolio.Invested)  //Sell signals
            {

                //Sell on strength (strong medium-term acceleration and positive short-term velocity)
                // Other options for this are the RSI and ADX indicators
                if (acceleration > AccelerationSellThreshold && rateOfChange > 0)
                {
                    Liquidate(_symbol);
                }

                //fixed stop loss  
                else if (Securities[_symbol].Close < FixedStopMultiple * buyPrice && rateOfChange > 0)  
                {
                    Liquidate(_symbol);
                }

                //Disaster stop loss
                else if (Securities[_symbol].Close < DisasterStopMultiple * buyPrice)  
                {
                    //Log("Disaster stop:" + buyPrice);
                    Liquidate(_symbol);
                }

                // Other stop loss possibilities------------------------------------------------------------

                //Regime change stop loss - Try to detect a transition to a neutral regime (3-bar overlap???)

                //Temporal stop loss - sell if nothing happens within a certain number of days

                //Trailing stop loss - Use volatility to determine the adaptive stop level

                //------------------------------------------------------------------------------------------

            }

        }
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                if (orderEvent.Direction == OrderDirection.Buy)
                    buyPrice = orderEvent.FillPrice;
                Log($"{orderEvent}");
            }
        }
        public override void OnEndOfAlgorithm()
        {
            //writer.Close();
        }

    }
}
