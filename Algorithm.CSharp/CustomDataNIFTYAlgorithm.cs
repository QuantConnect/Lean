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
using System.Globalization;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This demonstration imports indian NSE index "NIFTY" as a tradable security in addition to the USDINR currency pair. We move into the
    /// NSE market when the economy is performing well.s
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    public class CustomDataNiftyAlgorithm : QCAlgorithm
    {
        //Create variables for analyzing Nifty
        private CorrelationPair _today = new CorrelationPair();
        private readonly List<CorrelationPair> _prices = new List<CorrelationPair>();
        private const int _minimumCorrelationHistory = 50;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2008, 1, 8);
            SetEndDate(2014, 7, 25);

            //Set the cash for the strategy:
            SetCash(100000);

            //Define the symbol and "type" of our generic data:
            var rupee = AddData<DollarRupee>("USDINR", Resolution.Daily).Symbol;
            var nifty = AddData<Nifty>("NIFTY", Resolution.Daily).Symbol;

            EnableAutomaticIndicatorWarmUp = true;
            var rupeeSma = SMA(rupee, 20);
            var niftySma = SMA(rupee, 20);
            Log($"SMA - Is ready? USDINR: {rupeeSma.IsReady} NIFTY: {niftySma.IsReady}");
        }

        /// <summary>
        /// Event Handler for Nifty Data Events: These Nifty objects are created from our
        /// "Nifty" type below and fired into this event handler.
        /// </summary>
        /// <param name="data">One(1) Nifty Object, streamed into our algorithm synchronised in time with our other data streams</param>
        public void OnData(Slice data)
        {
            if (data.ContainsKey("USDINR"))
            {
                _today = new CorrelationPair(Time) { CurrencyPrice = Convert.ToDouble(data["USDINR"].Close) };
            }

            if (!data.ContainsKey("NIFTY"))
            {
                return;
            }

            try
            {

                _today.NiftyPrice = Convert.ToDouble(data["NIFTY"].Close);
                if (_today.Date == data["NIFTY"].EndTime)
                {
                    _prices.Add(_today);

                    if (_prices.Count > _minimumCorrelationHistory)
                    {
                        _prices.RemoveAt(0);
                    }
                }

                //Strategy
                var quantity = (int)(Portfolio.MarginRemaining * 0.9m / data["NIFTY"].Close);
                var highestNifty = (from pair in _prices select pair.NiftyPrice).Max();
                var lowestNifty = (from pair in _prices select pair.NiftyPrice).Min();
                
                if (Time.DayOfWeek == DayOfWeek.Wednesday) //prices.Count >= minimumCorrelationHistory &&
                {
                    //List<double> niftyPrices = (from pair in prices select pair.NiftyPrice).ToList();
                    //List<double> currencyPrices = (from pair in prices select pair.CurrencyPrice).ToList();
                    //double correlation = Correlation.Pearson(niftyPrices, currencyPrices);
                    //double niftyFraction = (correlation)/2;

                    if (Convert.ToDouble(data["NIFTY"].Open) >= highestNifty)
                    {
                        var code = Order("NIFTY", quantity - Portfolio["NIFTY"].Quantity);
                        Debug("LONG " + code + " Time: " + Time.ToShortDateString() + " Quantity: " + quantity + " Portfolio:" + Portfolio["NIFTY"].Quantity + " Nifty: " + data["NIFTY"].Close + " Buying Power: " + Portfolio.TotalPortfolioValue);
                    }
                    else if (Convert.ToDouble(data["NIFTY"].Open) <= lowestNifty)
                    {
                        var code = Order("NIFTY", -quantity - Portfolio["NIFTY"].Quantity);
                        Debug("SHORT " + code + " Time: " + Time.ToShortDateString() + " Quantity: " + quantity + " Portfolio:" + Portfolio["NIFTY"].Quantity + " Nifty: " + data["NIFTY"].Close + " Buying Power: " + Portfolio.TotalPortfolioValue);
                    }
                }
            }
            catch (Exception err)
            {
                Debug("Error: " + err.Message);
            }
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <remarks>Method is called 10 minutes before closing to allow user to close out position.</remarks>
        public override void OnEndOfDay()
        {
            Plot("Nifty Closing Price", _today.NiftyPrice);
        }
    }

    /// <summary>
    /// NIFTY Custom Data Class
    /// </summary>
    public class Nifty : BaseData
    {
        /// <summary>
        /// Opening Price
        /// </summary>
        public decimal Open;
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High;
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low;
        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close;

        /// <summary>
        /// Default initializer for NIFTY.
        /// </summary>
        public Nifty()
        {
            Symbol = "NIFTY";
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource("https://www.dropbox.com/s/rsmg44jr6wexn2h/CNXNIFTY.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New Nifty object
            var index = new Nifty();

            try
            {
                //Example File Format:
                //Date,       Open       High        Low       Close     Volume      Turnover
                //2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
                var data = line.Split(',');
                index.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                index.Open = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                index.High = Convert.ToDecimal(data[2], CultureInfo.InvariantCulture);
                index.Low = Convert.ToDecimal(data[3], CultureInfo.InvariantCulture);
                index.Close = Convert.ToDecimal(data[4], CultureInfo.InvariantCulture);
                index.Symbol = "NIFTY";
                index.Value = index.Close;
            }
            catch
            {
            }
            return index;
        }
    }


    /// <summary>
    /// Dollar Rupe is a custom data type we create for this algorithm
    /// </summary>
    public class DollarRupee : BaseData
    {
        /// <summary>
        /// Open Price
        /// </summary>
        public decimal Open = 0;
        /// <summary>
        /// High Price
        /// </summary>
        public decimal High = 0;
        /// <summary>
        /// Low Price
        /// </summary>
        public decimal Low = 0;
        /// <summary>
        /// Closing Price
        /// </summary>
        public decimal Close;

        /// <summary>
        /// Default constructor for the custom data class.
        /// </summary>
        public DollarRupee()
        {
            Symbol = "USDINR";
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource("https://www.dropbox.com/s/m6ecmkg9aijwzy2/USDINR.csv?dl=1", SubscriptionTransportMedium.RemoteFile);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method, and returns a new instance of the object
        /// each time it is called.
        /// </summary>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            //New USDINR object
            var currency = new DollarRupee();

            try
            {
                var data = line.Split(',');
                currency.Time = DateTime.Parse(data[0], CultureInfo.InvariantCulture);
                currency.Close = Convert.ToDecimal(data[1], CultureInfo.InvariantCulture);
                currency.Symbol = "USDINR";
                currency.Value = currency.Close;
            }
            catch
            {
            }
            return currency;
        }
    }

    /// <summary>
    /// Correlation Pair is a helper class to combine two data points which we'll use to perform the correlation.
    /// </summary>
    public class CorrelationPair
    {
        /// <summary>
        /// Date of the correlation pair
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// Nifty price for this correlation pair
        /// </summary>
        public double NiftyPrice;

        /// <summary>
        /// Currency price for this correlation pair
        /// </summary>
        public double CurrencyPrice;

        /// <summary>
        /// Default initializer
        /// </summary>
        public CorrelationPair()
        { }

        /// <summary>
        /// Date based correlation pair initializer
        /// </summary>
        public CorrelationPair(DateTime date)
        {
            Date = date.Date;
        }
    }
}