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

 using QuantConnect.Data;
 using RDotNet;
 using System.Linq;

 namespace QuantConnect.Algorithm.CSharp
 {
    /// <summary>
    /// Demonstration of the R-integration for calling external statistics operations in QuantConnect.
    /// </summary>
    /// <meta name="tag" content="using r" />
    /// <meta name="tag" content="statistics libraries" />
    public class CallingRFromCSharp : QCAlgorithm
     {
         private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

         /// <summary>
         /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
         /// </summary>
         public override void Initialize()
         {
             SetStartDate(2013, 10, 07);  //Set Start Date
             SetEndDate(2013, 10, 11);    //Set End Date
             SetCash(100000);             //Set Strategy Cash
             // Find more symbols here: http://quantconnect.com/data
             AddEquity("SPY", Resolution.Second);

             var engine = REngine.GetInstance();
             engine.Evaluate("print('This is from R command.')");
             // .NET Framework array to R vector.
             var group1 = engine.CreateNumericVector(new double[] { 30.02, 29.99, 30.11, 29.97, 30.01, 29.99 });
             engine.SetSymbol("group1", group1);
             // Direct parsing from R script.
             var group2 = engine.Evaluate("group2 <- c(29.89, 29.93, 29.72, 29.98, 30.02, 29.98)").AsNumeric();
             // Test difference of mean and get the P-value.
             var testResult = engine.Evaluate("t.test(group1, group2)").AsList();
             var p = testResult["p.value"].AsNumeric().First();
             // you should always dispose of the REngine properly.
             // After disposing of the engine, you cannot reinitialize nor reuse it
             engine.Dispose();

         }

         /// <summary>
         /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
         /// </summary>
         /// <param name="data">Slice object keyed by symbol containing the stock data</param>
         public override void OnData(Slice data)
         {
             if (!Portfolio.Invested)
             {
                 SetHoldings(_spy, 1);
             }
         }
     }
 }
