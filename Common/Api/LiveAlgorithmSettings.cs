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

using Newtonsoft.Json;
using QuantConnect.Brokerages;

namespace QuantConnect.Api
{
    /// <summary>
    /// Helper class to put BaseLiveAlgorithmSettings in proper format.
    /// </summary>
    public class LiveAlgorithmApiSettingsWrapper
    {
        /// <summary>
        /// Constructor for LiveAlgorithmApiSettingsWrapper
        /// </summary>
        /// <param name="projectId">Id of project from QuantConnect</param>
        /// <param name="compileId">Id of compilation of project from QuantConnect</param>
        /// <param name="nodeId">Server type to run live Algorithm</param>
        /// <param name="settings"><see cref="BaseLiveAlgorithmSettings ">Live Algorithm Settings</see> for a specific brokerage</param>
        /// <param name="version">The version identifier</param>
        public LiveAlgorithmApiSettingsWrapper(int projectId, string compileId, string nodeId, BaseLiveAlgorithmSettings settings, string version = "-1")
        {
            VersionId = version;
            ProjectId = projectId;
            CompileId = compileId;
            NodeId = nodeId;
            Brokerage = settings;
        }

        /// <summary>
        /// -1 is master
        /// </summary>
        [JsonProperty(PropertyName = "versionId")]
        public string VersionId { get; set; }

        /// <summary>
        /// Project id for the live instance
        /// </summary>
        [JsonProperty(PropertyName = "projectId")]
        public int ProjectId { get; private set; }

        /// <summary>
        /// Compile Id for the live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "compileId")]
        public string CompileId { get; private set; }

        /// <summary>
        /// Id of the node being used to run live algorithm
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; private set; }

        /// <summary>
        /// The API expects the settings as part of a brokerage object
        /// </summary>
        [JsonProperty(PropertyName = "brokerage")]
        public BaseLiveAlgorithmSettings Brokerage { get; private set; }
    }

    /// <summary>
    /// Base class for settings that must be configured per Brokerage to create new algorithms via the API.
    /// </summary>
    public class BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Constructor used by FXCM
        /// </summary>
        /// <param name="user">Username associated with brokerage</param>
        /// <param name="password">Password associated with brokerage</param>
        /// <param name="environment">'live'/'paper'</param>
        /// <param name="account">Account id for brokerage</param>
        public BaseLiveAlgorithmSettings(string user,
                                         string password,
                                         BrokerageEnvironment environment,
                                         string account)
        {
            User = user;
            Password = password;
            Environment = environment;
            Account = account;
        }

        /// <summary>
        /// Constructor used by Interactive Brokers
        /// </summary>
        /// <param name="user">Username associated with brokerage</param>
        /// <param name="password">Password associated with brokerage</param>
        public BaseLiveAlgorithmSettings(string user,
                                         string password)
        {
            Password = password;
            User = user;
        }

        /// <summary>
        /// The constructor used by Oanda
        /// </summary>
        /// <param name="environment">'live'/'paper'</param>
        /// <param name="account">Account id for brokerage</param>
        public BaseLiveAlgorithmSettings(BrokerageEnvironment environment,
                                         string account)
        {
            User = "";
            Password = "";
            Environment = environment;
            Account = account;
        }

        /// <summary>
        /// The constructor used by Tradier
        /// </summary>
        /// <param name="account">Account id for brokerage</param>
        public BaseLiveAlgorithmSettings(string account)
        {
            User = "";
            Password = "";
            Account = account;
        }

        /// <summary>
        /// The constructor used by Bitfinex
        /// </summary>
        public BaseLiveAlgorithmSettings()
        {
            User = "";
            Password = "";
            Account = "";
        }

        /// <summary>
        /// 'Interactive' / 'FXCM' / 'Oanda' / 'Tradier' /'PaperTrading'
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Username associated with brokerage
        /// </summary>
        [JsonProperty(PropertyName = "user")]
        public string User { get; private set; }

        /// <summary>
        /// Password associated with brokerage
        /// </summary>
        [JsonProperty(PropertyName = "password")]
        public string Password { get; private set; }

        /// <summary>
        /// 'live'/'paper'
        /// </summary>
        [JsonProperty(PropertyName = "environment")]
        public BrokerageEnvironment Environment { get; set; }

        /// <summary>
        /// Account of the associated brokerage
        /// </summary>
        [JsonProperty(PropertyName = "account")]
        public string Account { get; set; }
    }

    /// <summary>
    /// Default live algorithm settings
    /// </summary>
    public class DefaultLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Constructor for default algorithms
        /// </summary>
        /// <param name="user">Username associated with brokerage</param>
        /// <param name="password">Password associated with brokerage</param>
        /// <param name="environment">'live'/'paper'</param>
        /// <param name="account">Account id for brokerage</param>
        public DefaultLiveAlgorithmSettings(string user,
                                                string password,
                                                BrokerageEnvironment environment,
                                                string account)
            : base(user, password, environment, account)
        {
            Id = BrokerageName.QuantConnectBrokerage.ToStringInvariant();
        }
    }

    /// <summary>
    /// Algorithm setting for trading with FXCM
    /// </summary>
    public class FXCMLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {

        /// <summary>
        /// Contructor for live trading with FXCM
        /// </summary>
        /// <param name="user">Username associated with brokerage</param>
        /// <param name="password">Password associated with brokerage</param>
        /// <param name="environment">'live'/'paper'</param>
        /// <param name="account">Account id for brokerage</param>
        public FXCMLiveAlgorithmSettings(string user,
                                         string password,
                                         BrokerageEnvironment environment,
                                         string account)
            : base(user, password, environment, account)
        {
            Id = BrokerageName.FxcmBrokerage.ToStringInvariant();
        }

    }

    /// <summary>
    /// Live algorithm settings for trading with Interactive Brokers
    /// </summary>
    public class InteractiveBrokersLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Contructor for live trading with IB.
        /// </summary>
        /// <param name="user">Username associated with brokerage</param>
        /// <param name="password">Password of assciate brokerage</param>
        /// <param name="account">Account id for brokerage</param>
        public InteractiveBrokersLiveAlgorithmSettings(string user,
                                                       string password,
                                                       string account)
            : base(user, password)
        {
            Account = account;
            Environment = Account.Substring(0, 2) == "DU" ? BrokerageEnvironment.Paper : BrokerageEnvironment.Live;
            Id = BrokerageName.InteractiveBrokersBrokerage.ToStringInvariant();
        }
    }

    /// <summary>
    /// Live algorithm settings for trading with Oanda
    /// </summary>
    public class OandaLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Contructor for live trading with Oanda.
        /// </summary>
        /// <param name="accessToken">Access Token (specific for Oanda Brokerage)</param>
        /// <param name="environment">'live'/'paper'</param>
        /// <param name="account">Account id for brokerage</param>
        public OandaLiveAlgorithmSettings(string accessToken,
                                          BrokerageEnvironment environment,
                                          string account)
            : base(environment, account)
        {
            AccessToken = accessToken;
            // The DateIssued parameter is required by the Api, but not required to trade.
            // This should be fixed on the Api side.
            DateIssued = "1";
            Id = BrokerageName.OandaBrokerage.ToStringInvariant();
        }

        /// <summary>
        /// Access token for Oanda
        /// </summary>
        [JsonProperty(PropertyName = "accessToken")]
        public string AccessToken { get; private set; }


        /// <summary>
        /// Date token was issued
        /// </summary>
        [JsonProperty(PropertyName = "dateIssued")]
        public string DateIssued { get; private set; }
    }

    /// <summary>
    /// Live algorithm settings for trading with Tradier
    /// </summary>
    public class TradierLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Contructor for live trading with Tradier.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="dateIssued">Specific for live trading with Tradier.  See Tradier account for more details.</param>
        /// <param name="refreshToken">Specific for live trading with Tradier.  See Tradier account for more details.</param>
        /// <param name="account">Account id for brokerage</param>
        public TradierLiveAlgorithmSettings(string accessToken,
                                            string dateIssued,
                                            string refreshToken,
                                            string account)
            : base(account)
        {
            Environment = BrokerageEnvironment.Live;
            AccessToken = accessToken;
            DateIssued = dateIssued;
            RefreshToken = refreshToken;
            Lifetime = "86399";
            Id = BrokerageName.TradierBrokerage.ToString();
        }

        /// <summary>
        /// Access token for tradier brokerage
        /// </summary>
        [JsonProperty(PropertyName = "accessToken")]
        public string AccessToken { get; private set; }

        /// <summary>
        /// Property specific to Tradier account.  See tradier account for more details.
        /// </summary>
        [JsonProperty(PropertyName = "dateIssued")]
        public string DateIssued { get; private set; }

        /// <summary>
        /// Property specific to Tradier account.  See tradier account for more details.
        /// </summary>
        [JsonProperty(PropertyName = "refreshToken")]
        public string RefreshToken { get; private set; }

        /// <summary>
        /// Property specific to Tradier account.  See tradier account for more details.
        /// </summary>
        [JsonProperty(PropertyName = "lifetime")]
        public string Lifetime { get; private set; }
    }

    /// <summary>
    /// Live algorithm settings for trading with Bitfinex
    /// </summary>
    public class BitfinexLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Constructor for live trading with Bitfinex
        /// </summary>
        /// <param name="key"> Api key to Bitfinex account</param>
        /// <param name="secret">Secret Api key to Bitfinex account</param>
        public BitfinexLiveAlgorithmSettings(string key, string secret)
        {
            Environment = BrokerageEnvironment.Live;
            Id = "BitfinexBrokerage"; // BrokerageName.Bitfinex.ToString() returns "Bitfinex"
            Key = key;
            Secret = secret;
        }

        /// <summary>
        /// Property specific to Bitfinex account. API Key 
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key { get; private set; }

        /// <summary>
        /// Property specific to Bitfinex account. API Secret Key
        /// </summary>
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; private set; }
    }

    /// <summary>
    /// Live algorithm settings for trading with GDAX (Coinbase)
    /// </summary>
    public class GDAXLiveAlgorithmSettings : BaseLiveAlgorithmSettings
    {
        /// <summary>
        /// Constructor for live trading with GDAX (Coinbase)
        /// </summary>
        /// <param name="key"> Api key to GDAX account</param>
        /// <param name="secret">Secret Api key to GDAX account</param>
        /// <param name="passphrase">Passphrase to this API key</param>
        public GDAXLiveAlgorithmSettings(string key, string secret, string passphrase)
        {
            Environment = BrokerageEnvironment.Live;
            Id = "GDAXBrokerage"; //BrokerageName.GDAX.ToString(); returns "GDAX"
            Key = key;
            Secret = secret;
            Passphrase = passphrase;
        }

        /// <summary>
        /// Property specific to GDAX account. API Key 
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key { get; private set; }

        /// <summary>
        /// Property specific to GDAX account. API Secret Key
        /// </summary>
        [JsonProperty(PropertyName = "secret")]
        public string Secret { get; private set; }

        /// <summary>
        /// Property specific to GDAX account. API Passphrase
        /// </summary>
        [JsonProperty(PropertyName = "passphrase")]
        public string Passphrase { get; private set; }
    }
}