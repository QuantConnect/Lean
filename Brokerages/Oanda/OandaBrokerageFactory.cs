using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// 
    /// </summary>
    public class OandaBrokerageFactory: BrokerageFactory
    {

        /// <summary>
        /// Gets tradier values from configuration
        /// </summary>
        public static class Configuration
        {
            /// <summary>
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static int QuantConnectUserId
            {
                get { return Config.GetInt("qc-user-id"); }
            }
            
            /// <summary>
            /// Gets the account ID to be used when instantiating a brokerage
            /// </summary>
            public static int AccountId
            {
                get { return Config.GetInt("oanda-account-id"); }
            }

            /// <summary>
            /// Gets the access token from configuration
            /// </summary>
            public static string AccessToken
            {
                get { return Config.Get("oanda-access-token"); }
            }


            /// <summary>
            /// Gets the access token from configuration
            /// </summary>
            public static string Environment
            {
                get { return Config.Get("oanda-environment"); }
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageFactory"/> class.
        /// </summary>
        public OandaBrokerageFactory() : base(typeof(OandaBrokerage))
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }


        /// <summary>
        /// File path used to store tradier token data
        /// </summary>
        public const string TokensFile = "oanda-tokens.txt";

        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                string accessToken;

                var accountId = Configuration.AccountId.ToString();
                var data = new Dictionary<string, string>();
                if (File.Exists(TokensFile))
                {
                    var tokens = JsonConvert.DeserializeObject<TokenResponse>(File.ReadAllText(TokensFile));
                    accessToken = tokens.AccessToken;
                } 
                else
                {
                    accessToken = Configuration.AccessToken;
                }
                data.Add("oanda-account-id", accountId);
                data.Add("oanda-access-token", accessToken);
                return data;
            }
        }

        /// <summary>
        /// Creates a new IBrokerage instance
        /// </summary>
        /// <param name="job">The job packet to create the brokerage for</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>A new brokerage instance</returns>
        public override IBrokerage CreateBrokerage(LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var accountId = Read<int>(job.BrokerageData, "oanda-account-id", errors);
            var accessToken = Read<string>(job.BrokerageData, "oanda-access-token", errors);
            var refreshToken = Read<string>(job.BrokerageData, "oanda-refresh-token", errors);
            var issuedAt = Read<DateTime>(job.BrokerageData, "oanda-issued-at", errors);
            var lifeSpan = TimeSpan.FromSeconds(Read<double>(job.BrokerageData, "oanda-lifespan", errors));
            var environment = Read<string>(job.BrokerageData, "oanda-environment", errors);

            var brokerage = new OandaBrokerage(algorithm.Transactions, algorithm.Portfolio, accountId);
            // if we're running live locally we'll want to save any new tokens generated so that they can easily be retrieved
            if (Config.GetBool("local"))
            {
                brokerage.SessionRefreshed += (sender, args) =>
                {
                    File.WriteAllText(TokensFile, JsonConvert.SerializeObject(args, Formatting.Indented));
                };
            }

            brokerage.SetTokens(job.UserId, accessToken, refreshToken, issuedAt, lifeSpan);
            brokerage.SetEnvironment(environment);
            return brokerage;
        }

        /// <summary>
        /// Reads the Oanda tokens from the <see cref="TokensFile"/> or from configuration
        /// </summary>
        public static TokenResponse GetTokens()
        {
            // pick a source for our tokens
            if (File.Exists(TokensFile))
            {
                Log.Trace("Reading Oanda tokens from " + TokensFile);
                return JsonConvert.DeserializeObject<TokenResponse>(File.ReadAllText(TokensFile));
            }

            return new TokenResponse
            {
                AccessToken = Config.Get("oanda-access-token"),
                RefreshToken = Config.Get("oanda-refresh-token"),
                IssuedAt = DateTime.Now,
                ExpiresIn = 100000
            };
        }
    }


}
