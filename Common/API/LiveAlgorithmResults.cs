using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Api;
using QuantConnect.Orders;

namespace QuantConnect.API
{
    /// <summary>
    /// Details a live algorithm from the "live/read" Api endpoint
    /// </summary>
    public class LiveAlgorithmResults : RestResponse
    {
        /// <summary>
        /// Represents data about the live running algorithm returned from the server
        /// </summary>
        public LiveResults LiveResults { get; set; }
    }

    /// <summary>
    /// Holds information about the state and operation of the live running algorithm
    /// </summary>
    public class LiveResults
    {
        /// <summary>
        /// Results version
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Temporal resolution of the results returned from the Api
        /// </summary>
        [JsonProperty(PropertyName = "resolution")]
        public string Resolution { get; set; }

        /// <summary>
        /// Class to represent the data groups results return from the Api
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public Results Results { get; set; }
    }

    /// <summary>
    /// Class to represent the major groups of data returned from the Api
    /// </summary>
    public class Results
    {
        /// <summary>
        /// Information about the portfolio over time
        /// </summary>
        public Charts Charts { get; set; }

        /// <summary>
        /// Information about the current portfolio holdings
        /// </summary>
        public Holding Holdings { get; set; }

        /// <summary>
        /// Orders that have been made
        /// </summary>
        public Order[] Orders { get; set; }
    }

    /// <summary>
    /// Information about the portfolio over time
    /// </summary>
    public class Charts
    {
        /// <summary>
        /// Information specific to the security being traded
        /// </summary>
        [JsonProperty(PropertyName = "Strategy Equity")]
        public Strategy StrategyEquity { get; set; }

        /// <summary>
        /// Meta information about the data
        /// </summary>
        public Meta Meta { get; set; }
    }

    /// <summary>
    /// Information about the live algorithms performance
    /// </summary>
    public class Strategy
    {
        /// <summary>
        /// Name of the strategy
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Type of data
        /// </summary>
        public int ChartType { get; set; }
        /// <summary>
        /// Class the holds the series of data
        /// </summary>
        public Series Series { get; set; }
    }

    /// <summary>
    /// Generic X, Y coordinates of the data returned by the Api
    /// </summary>
    public class Value
    {
        /// <summary>
        /// X-coordinate of the data returned from the Api, typically time
        /// </summary>
        [JsonProperty(PropertyName = "x")]
        public int X { get; set; }
        /// <summary>
        /// Y-coordinate of the data returned from the Api, usually value of the portfolio
        /// </summary>
        [JsonProperty(PropertyName = "y")]
        public float? Y { get; set; }
    }

    /// <summary>
    /// Meta information about the chart data
    /// </summary>
    public class Meta
    {
        /// <summary>
        /// Type of chart
        /// </summary>
        public int ChartType { get; set; }
        /// <summary>
        /// Class the holds the series of data
        /// </summary>
        public Series Series { get; set; }
    }

    /// <summary>
    /// Details of the state and operation of the algorithm
    /// </summary>
    public class Series
    {
        /// <summary>
        ///  Details of the algorithms launch
        /// </summary>
        public Status Launched { get; set; }
        /// <summary>
        ///  Details if the algorithm has been liquidated
        /// </summary>
        public Status Liquidated { get; set; }
        /// <summary>
        ///  Details if the algorithm has been stopped
        /// </summary>
        public Status Stopped { get; set; }
        /// <summary>
        /// Data about runtime errors encountered by the algorithm
        /// </summary>
        public Status RuntimeError { get; set; }
        /// <summary>
        /// Data about algorithm's equity trading
        /// </summary>
        public Status Equity { get; set; }
        /// <summary>
        /// Data about the algorithm's FX trading
        /// </summary>
        public Status Forex { get; set; }

    }

    /// <summary>
    /// Status of a particular operation of an algorithm
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Name of the status
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Generic class to hold a series of data
        /// </summary>
        public Value[] Values { get; set; }
        /// <summary>
        /// Type of series represented
        /// </summary>
        public int SeriesType { get; set; }
    }

    /// <summary>
    /// Symbol data
    /// </summary>
    public class Symbol
    {
        /// <summary>
        /// Type of security this symbol represents
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        /// <summary>
        /// Value of the security this symbol represents
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Id of the symbol
        /// </summary>
        [JsonProperty(PropertyName = "ID")]
        public string Id { get; set; }
        /// <summary>
        /// Unique identifier of the symbol
        /// </summary>
        public string Permtick { get; set; }
    }

}
