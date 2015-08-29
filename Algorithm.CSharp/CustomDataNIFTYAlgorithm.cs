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

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// 3.0 CUSTOM DATA SOURCE: USE YOUR OWN MARKET DATA (OPTIONS, FOREX, FUTURES, DERIVATIVES etc).
    /// 
    /// The new QuantConnect Lean Backtesting Engine is incredibly flexible and allows you to define your own data source. 
    /// 
    /// This includes any data source which has a TIME and VALUE. These are the *only* requirements. To demonstrate this we're loading
    /// in "Nifty" data. This by itself isn't special, the cool part is next:
    /// 
    /// We load the "Nifty" data as a tradable security we're calling "NIFTY".
    /// 
    /// </summary>
    public class CustomDataNIFTYAlgorithm : QCAlgorithm
    {
        //Create variables for analyzing Nifty
        CorrelationPair today = new CorrelationPair();
        List<CorrelationPair> prices = new List<CorrelationPair>();
        int minimumCorrelationHistory = 50;

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
            AddData<DollarRupee>("USDINR");
            AddData<Nifty>("NIFTY");
        }

        /// <summary>
        /// Event Handler for Nifty Data Events: These Nifty objects are created from our 
        /// "Nifty" type below and fired into this event handler.
        /// </summary>
        /// <param name="data">One(1) Nifty Object, streamed into our algorithm synchronised in time with our other data streams</param>
        public void OnData(DollarRupee data)
        {
            today = new CorrelationPair(data.Time);
            today.CurrencyPrice = Convert.ToDouble(data.Close);
        }

        /// <summary>
        /// OnData is the primary entry point for youm algorithm. New data is piped into your algorithm here
        /// via TradeBars objects.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object</param>
        public void OnData(Nifty data)
        {
            try
            {
                int quantity = (int)(Portfolio.TotalPortfolioValue * 0.9m / data.Close);

                today.NiftyPrice = Convert.ToDouble(data.Close);
                if (today.Date == data.Time)
                {
                    prices.Add(today);

                    if (prices.Count > minimumCorrelationHistory)
                    {
                        prices.RemoveAt(0);
                    }
                }

                //Strategy
                double highestNifty = (from pair in prices select pair.NiftyPrice).Max();
                double lowestNifty = (from pair in prices select pair.NiftyPrice).Min();
                if (Time.DayOfWeek == DayOfWeek.Wednesday) //prices.Count >= minimumCorrelationHistory && 
                {
                    //List<double> niftyPrices = (from pair in prices select pair.NiftyPrice).ToList();
                    //List<double> currencyPrices = (from pair in prices select pair.CurrencyPrice).ToList();
                    //double correlation = Correlation.Pearson(niftyPrices, currencyPrices);
                    //double niftyFraction = (correlation)/2;

                    if (Convert.ToDouble(data.Open) >= highestNifty)
                    {
                        int code = Order("NIFTY", quantity - Portfolio["NIFTY"].Quantity);
                        Debug("LONG " + code + " Time: " + Time.ToShortDateString() + " Quantity: " + quantity + " Portfolio:" + Portfolio["NIFTY"].Quantity + " Nifty: " + data.Close + " Buying Power: " + Portfolio.TotalPortfolioValue);
                    }
                    else if (Convert.ToDouble(data.Open) <= lowestNifty)
                    {
                        int code = Order("NIFTY", -quantity - Portfolio["NIFTY"].Quantity);
                        Debug("SHORT " + code + " Time: " + Time.ToShortDateString() + " Quantity: " + quantity + " Portfolio:" + Portfolio["NIFTY"].Quantity + " Nifty: " + data.Close + " Buying Power: " + Portfolio.TotalPortfolioValue);
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
            //if(niftyData != null)
            {
                Plot("Nifty Closing Price", today.NiftyPrice);
            }
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
        public decimal Close = 0;

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
            Nifty index = new Nifty();

            try
            {
                //Example File Format:
                //Date,       Open       High        Low       Close     Volume      Turnover
                //2011-09-13  7792.9    7799.9     7722.65    7748.7    116534670    6107.78
                string[] data = line.Split(',');
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
        public decimal Close = 0;

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
            DollarRupee currency = new DollarRupee();

            try
            {
                string[] data = line.Split(',');
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
        public DateTime Date = new DateTime();

        /// <summary>
        /// Nifty price for this correlation pair
        /// </summary>
        public double NiftyPrice = 0;

        /// <summary>
        /// Currency price for this correlation pair
        /// </summary>
        public double CurrencyPrice = 0;

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