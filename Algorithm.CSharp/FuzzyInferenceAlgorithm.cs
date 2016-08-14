using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using AForge.Fuzzy;

namespace QuantConnect.Algorithm.CSharp
{
    public class FuzzyEngine
    {
        private InferenceSystem IS;

        public FuzzyEngine()
        {
            // Linguistic labels (fuzzy sets) for 
            FuzzySet obvLow = new FuzzySet("Low", new TrapezoidalFunction(0, 10, TrapezoidalFunction.EdgeType.Right));
            FuzzySet obvHigh = new FuzzySet("High", new TrapezoidalFunction(0, 10, TrapezoidalFunction.EdgeType.Left));

            // Linguistic labels (fuzzy sets) for 
            FuzzySet rsiLow = new FuzzySet("Low", new TrapezoidalFunction(0, 30, TrapezoidalFunction.EdgeType.Right));
            FuzzySet rsiMedium = new FuzzySet("Medium", new TrapezoidalFunction(0, 50, 50, 100));
            FuzzySet rsiHigh = new FuzzySet("high", new TrapezoidalFunction(70, 100, TrapezoidalFunction.EdgeType.Left));

            // OBV (Input)
            LinguisticVariable lvObv = new LinguisticVariable("OBV", 0, 10);
            lvObv.AddLabel(obvLow);
            lvObv.AddLabel(obvHigh);

            // RSI (Input)
            LinguisticVariable lvRsi = new LinguisticVariable("RSI", 0, 100);
            lvRsi.AddLabel(rsiLow);
            lvRsi.AddLabel(rsiMedium);
            lvRsi.AddLabel(rsiHigh);

            // Linguistic labels (fuzzy sets) that compose the Order
            FuzzySet fsShort = new FuzzySet("Sell", new TrapezoidalFunction(0, 5, 5, 10));
            FuzzySet fsHold = new FuzzySet("Hold", new TrapezoidalFunction(10, 15, 15, 20));
            FuzzySet fsLong = new FuzzySet("Buy", new TrapezoidalFunction(20, 25, 25, 30));

            // Output
            LinguisticVariable lvSignal = new LinguisticVariable("Signal", 0, 30);
            lvSignal.AddLabel(fsShort);
            lvSignal.AddLabel(fsHold);
            lvSignal.AddLabel(fsLong);

            // The database
            Database fuzzyDB = new Database();
            fuzzyDB.AddVariable(lvObv);
            fuzzyDB.AddVariable(lvRsi);
            fuzzyDB.AddVariable(lvSignal);

            // Creating the inference system
            IS = new InferenceSystem(fuzzyDB, new CentroidDefuzzifier(1000));

            // Rules
            IS.NewRule("Rule 1", "IF RSI IS Low AND OBV IS High THEN Signal IS Buy");
            IS.NewRule("Rule 2", "IF RSI IS High AND OBV IS Low THEN Signal IS Hold");
            IS.NewRule("Rule 3", "IF RSI IS Medium AND OBV IS High THEN Signal IS Buy");
            IS.NewRule("Rule 4", "IF RSI IS Medium AND OBV IS Low THEN Signal IS Hold");
            IS.NewRule("Rule 5", "IF RSI IS High AND OBV IS Low THEN Signal IS Hold");
            IS.NewRule("Rule 6", "IF RSI IS Medium AND OBV IS Low THEN Signal IS Sell");
        }

        public double DoInference(float obv, float rsi)
        {
            // Setting inputs
            IS.SetInput("OBV", obv);
            IS.SetInput("RSI", rsi);

            // Setting outputs
            double signal = IS.Evaluate("Signal");

            return signal;
        }
    }


    public class FuzzyInferenceAlgorithm : QCAlgorithm
    {
        //Indicators
        private OnBalanceVolume obv;
        private RelativeStrengthIndex rsi;
        //Fuzzy Engine
        private FuzzyEngine engine;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddEquity("SPY", Resolution.Second);

            obv = new OnBalanceVolume("OBV");
            rsi = new RelativeStrengthIndex("RSI", 14);

            engine = new FuzzyEngine();
        }

        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                double signal = engine.DoInference(float.Parse(obv.Current.Value.ToString()),
                                            float.Parse(rsi.Current.Value.ToString()));

                if (signal > 90)
                {
                    SetHoldings("SPY", 1);
                    Debug("Purchased Stock");
                }

            }
        }
    }
}