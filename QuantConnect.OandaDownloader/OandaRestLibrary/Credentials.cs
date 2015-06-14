using System.Collections.Generic;

namespace OANDARestLibrary
{
	public enum EServer
	{
		Account,
		Rates,
		StreamingRates,
		StreamingEvents,
		Labs
	}

	public enum EEnvironment
	{
		Sandbox,
		Practice,
		Trade
	}

	public class Credentials
	{
		public bool HasServer(EServer server)
		{
			return Servers[Environment].ContainsKey(server);
		}

		public string GetServer(EServer server)
		{
			if (HasServer(server))
			{
				return Servers[Environment][server];
			}
			return null;
		}

		private static readonly Dictionary<EEnvironment, Dictionary<EServer, string>> Servers = new Dictionary<EEnvironment, Dictionary<EServer, string>>
			{
				{EEnvironment.Sandbox, new Dictionary<EServer, string>
					{
						{EServer.Account, "http://api-sandbox.oanda.com/v1/"},
						{EServer.Rates, "http://api-sandbox.oanda.com/v1/"},
						{EServer.StreamingRates, "http://stream-sandbox.oanda.com/v1/"},
						{EServer.StreamingEvents, "http://stream-sandbox.oanda.com/v1/"},
					}
				},
				{EEnvironment.Practice, new Dictionary<EServer, string>
					{
						{EServer.StreamingRates, "https://stream-fxpractice.oanda.com/v1/"},
						{EServer.StreamingEvents, "https://stream-fxpractice.oanda.com/v1/"},
						{EServer.Account, "https://api-fxpractice.oanda.com/v1/"},
						{EServer.Rates, "https://api-fxpractice.oanda.com/v1/"},
						{EServer.Labs, "https://api-fxpractice.oanda.com/labs/v1/"},
					}
				},
				{EEnvironment.Trade, new Dictionary<EServer, string>
					{
						{EServer.StreamingRates, "https://stream-fxtrade.oanda.com/v1/"},
						{EServer.StreamingEvents, "https://stream-fxtrade.oanda.com/v1/"},
						{EServer.Account, "https://api-fxtrade.oanda.com/v1/"},
						{EServer.Rates, "https://api-fxtrade.oanda.com/v1/"},
						{EServer.Labs, "https://api-fxtrade.oanda.com/labs/v1/"},
					}
				}
			};
		public string AccessToken;

		private static Credentials _instance;
		public int DefaultAccountId;
		public EEnvironment Environment;
		public string Username;

		public static Credentials GetDefaultCredentials()
		{
			return _instance;
		}

		public static void SetCredentials(EEnvironment environment, string accessToken, int defaultAccount = 0)
		{
			_instance = new Credentials
				{
					Environment = environment,
					AccessToken = accessToken,
					DefaultAccountId = defaultAccount
				};
		}
	}
}
