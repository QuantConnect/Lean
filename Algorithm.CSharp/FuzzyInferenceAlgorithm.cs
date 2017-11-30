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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using Accord.Fuzzy;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the Accord Fuzzy Logic library in CSharp. Using Accord to do fuzzy inference for making decisions on indicators.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="machine learning" />
    /// <meta name="tag" content="fuzzy logic" />
    public class FuzzyInferenceAlgorithm : QCAlgorithm
    {
        //Indicators
        private RelativeStrengthIndex _rsi;
        private Momentum _mom;
        private string _symbol = "SPY";

        //
        // With Accord v3.3.0, we need Accord.Math referenced in other projects that use
        // this. By placing a hard reference to an Accord.Math type, the compiler
        // will properly copy the required dlls into other project bin directories.
        // Without this, consuming projects would need to hard reference the Accord dlls,
        // which is less than perfect. This seems to be the better of two evils
        //
        Accord.Math.Matrix3x3 _matrix = new Accord.Math.Matrix3x3();

        //Fuzzy Engine
        private FuzzyEngine _engine;

        public override void Initialize()
        {
            SetStartDate(2015, 01, 01);  //Set Start Date
            SetEndDate(2015, 06, 30);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddEquity(_symbol, Resolution.Daily);

            _rsi = RSI(_symbol, 14, MovingAverageType.Simple, Resolution.Daily);
            _mom = MOM(_symbol, 10, Resolution.Daily, Field.Close);

            _engine = new FuzzyEngine();
        }

        public void OnData(TradeBars data)
        {
            if (_rsi.IsReady && _mom.IsReady)
            {
                try
                {
                    var signal = _engine.DoInference((float)_mom.Current.Value, (float)_rsi.Current.Value);

                    if (!Portfolio.Invested)
                    {
                        if (signal > 30)
                        {
                            var quantity = decimal.ToInt32(Portfolio.MarginRemaining / data[_symbol].Price);
                            Buy(_symbol, quantity);
                            Debug("Purchased Stock: " + quantity + " shares");
                        }
                    }
                    else
                    {
                        if (signal < -10)
                        {
                            var quantity = decimal.ToInt32(Portfolio[_symbol].Quantity);
                            Sell(_symbol, quantity);
                            Debug("Sold Stock: " + quantity + " shares");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug("Ex: " + ex.Message);
                    Debug("## rsi: " + _rsi + " mom: " + _mom);
                }
            }

        }
    }

    public class FuzzyEngine
    {
        private InferenceSystem IS;

        public FuzzyEngine()
        {
            // Linguistic labels (fuzzy sets) for Momentum
            var momDown = new FuzzySet("Down", new TrapezoidalFunction(-20, 5, 5, 5));
            var momNeutral = new FuzzySet("Neutral", new TrapezoidalFunction(-20, 0, 0, 20));
            var momUp = new FuzzySet("Up", new TrapezoidalFunction(5, 20, 20, 20));


            // Linguistic labels (fuzzy sets) for RSI
            var rsiLow = new FuzzySet("Low", new TrapezoidalFunction(0, 30, 30, 30));
            var rsiMedium = new FuzzySet("Medium", new TrapezoidalFunction(0, 50, 50, 100));
            var rsiHigh = new FuzzySet("High", new TrapezoidalFunction(70, 100, 100, 100));

            // MOM (Input)
            var lvMom = new LinguisticVariable("MOM", -20, 20);
            lvMom.AddLabel(momDown);
            lvMom.AddLabel(momNeutral);
            lvMom.AddLabel(momUp);

            // RSI (Input)
            var lvRsi = new LinguisticVariable("RSI", 0, 100);
            lvRsi.AddLabel(rsiLow);
            lvRsi.AddLabel(rsiMedium);
            lvRsi.AddLabel(rsiHigh);

            // Linguistic labels (fuzzy sets) that compose the Signal
            var fsShort = new FuzzySet("Sell", new TrapezoidalFunction(-100, 0, 0, 00));
            var fsHold = new FuzzySet("Hold", new TrapezoidalFunction(-50, 0, 0, 50));
            var fsLong = new FuzzySet("Buy", new TrapezoidalFunction(0, 100, 100, 100));

            // Output
            var lvSignal = new LinguisticVariable("Signal", -100, 100);
            lvSignal.AddLabel(fsShort);
            lvSignal.AddLabel(fsHold);
            lvSignal.AddLabel(fsLong);

            // The database
            var fuzzyDB = new Database();
            fuzzyDB.AddVariable(lvMom);
            fuzzyDB.AddVariable(lvRsi);
            fuzzyDB.AddVariable(lvSignal);

            // Creating the inference system
            IS = new InferenceSystem(fuzzyDB, new CentroidDefuzzifier(1000));

            // Rules
            IS.NewRule("Rule 1", "IF RSI IS Low AND MOM IS Down THEN Signal IS Buy");
            IS.NewRule("Rule 2", "IF RSI IS Medium AND MOM IS Down THEN Signal IS Buy");
            IS.NewRule("Rule 3", "IF RSI IS High AND MOM IS Down THEN Signal IS Hold");

            IS.NewRule("Rule 4", "IF RSI IS Low AND MOM IS Neutral THEN Signal IS Buy");
            IS.NewRule("Rule 5", "IF RSI IS Medium AND MOM IS Neutral THEN Signal IS Hold");
            IS.NewRule("Rule 6", "IF RSI IS High AND MOM IS Neutral THEN Signal IS Sell");

            IS.NewRule("Rule 7", "IF RSI IS Low AND MOM IS Up THEN Signal IS Hold");
            IS.NewRule("Rule 8", "IF RSI IS Medium AND MOM IS Up THEN Signal IS Sell");
            IS.NewRule("Rule 9", "IF RSI IS High AND MOM IS Up THEN Signal IS Sell");
        }

        public double DoInference(float mom, float rsi)
        {
            // Setting inputs
            IS.SetInput("MOM", mom);
            IS.SetInput("RSI", rsi);

            // Setting outputs
            double signal = IS.Evaluate("Signal");

            return signal;
        }
    }
}