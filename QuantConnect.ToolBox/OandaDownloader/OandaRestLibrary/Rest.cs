using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OANDARestLibrary.TradeLibrary.DataTypes;
using OANDARestLibrary.TradeLibrary.DataTypes.Communications;
using OANDARestLibrary.TradeLibrary.DataTypes.Communications.Requests;

namespace OANDARestLibrary
{
	/// Best Practices Notes
	/// 
	/// Keep alive is on by default
    public class Rest
    {
		// Convenience helpers
		private static string Server(EServer server) { return Credentials.GetDefaultCredentials().GetServer(server); }
		private static string AccessToken { get { return Credentials.GetDefaultCredentials().AccessToken; } }

		/// <summary>
		/// More detailed request to retrieve candles
		/// </summary>
		/// <param name="request">the request data to use when retrieving the candles</param>
		/// <returns>List of Candles received (or empty list)</returns>
		public static async Task<List<Candle>> GetCandlesAsync(CandlesRequest request)
		{
			string requestString = Server(EServer.Rates) + request.GetRequestString();

			CandlesResponse candlesResponse = await MakeRequestAsync<CandlesResponse>(requestString);
            List<Candle> candles = new List<Candle>();
            if (candlesResponse != null)
		    {
		        candles.AddRange(candlesResponse.candles);
		    }
		    return candles;
		}

		private static string GetCommaSeparatedList(List<string> items)
		{
			StringBuilder result = new StringBuilder();
			foreach (var item in items)
			{
				result.Append(item + ",");
			}
			return result.ToString().Trim(',');
		}

		/// <summary>
		/// Retrieves the list of instruments available for the given account
		/// </summary>
		/// <param name="account">the account to check</param>
		/// <param name="fields">optional - the fields to request in the response</param>
		/// <param name="instrumentNames">optional - the instruments to request details for</param>
		/// <returns>List of Instrument objects with details about each instrument</returns>
		public static async Task<List<Instrument>> GetInstrumentsAsync(int account, List<string> fields=null, List<string> instrumentNames=null)
        {
			string requestString = Server(EServer.Rates) + "instruments?accountId=" + account;

			// TODO: make sure this works
			if (fields != null)
			{
				string fieldsParam = GetCommaSeparatedList(fields);
				requestString += "&fields=" + Uri.EscapeDataString(fieldsParam);
			}
			if (instrumentNames != null)
			{
				string instrumentsParam = GetCommaSeparatedList(instrumentNames);
				requestString += "&instruments=" + Uri.EscapeDataString(instrumentsParam);
			}

            InstrumentsResponse instrumentResponse = await MakeRequestAsync<InstrumentsResponse>(requestString);

            List<Instrument> instruments = new List<Instrument>();
            instruments.AddRange(instrumentResponse.instruments);

            return instruments;
        }

		/// <summary>
		/// Primary (internal) request handler
		/// </summary>
		/// <typeparam name="T">The response type</typeparam>
		/// <param name="requestString">the request to make</param>
		/// <param name="method">method for the request (defaults to GET)</param>
		/// <param name="requestParams">optional parameters (note that if provided, it's assumed the requestString doesn't contain any)</param>
		/// <returns>response via type T</returns>
		private static async Task<T> MakeRequestAsync<T>(string requestString, string method="GET", Dictionary<string, string> requestParams=null)
        {
			if (requestParams != null && requestParams.Count > 0)
			{
				var parameters = CreateParamString(requestParams);
				requestString = requestString + "?" + parameters;
			}
			HttpWebRequest request = WebRequest.CreateHttp(requestString);
			request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;
			request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
			request.Method = method;

	        try
	        {
				using (WebResponse response = await request.GetResponseAsync())
				{
                    var stream = GetResponseStream(response);
                    var reader = new StreamReader(stream);
                    var result = reader.ReadToEnd();
				    return JsonConvert.DeserializeObject<T>(result);
				}
			}
			catch (WebException ex)
			{
				var stream = GetResponseStream(ex.Response);
				var reader = new StreamReader(stream);
				var result = reader.ReadToEnd();
				throw new Exception(result);
			}
        }

		private static Stream GetResponseStream(WebResponse response)
		{
			var stream = response.GetResponseStream();
			if (response.Headers["Content-Encoding"] == "gzip")
			{	// if we received a gzipped response, handle that
				stream = new GZipStream(stream, CompressionMode.Decompress);
			}
			return stream;
		}
        
		/// <summary>
		/// Helper function to create the parameter string out of a dictionary of parameters
		/// </summary>
		/// <param name="requestParams">the parameters to convert</param>
		/// <returns>string containing all the parameters for use in requests</returns>
	    private static string CreateParamString(Dictionary<string, string> requestParams)
	    {
		    string requestBody = "";
		    foreach (var pair in requestParams)
		    {
			    requestBody += WebUtility.UrlEncode(pair.Key) + "=" + WebUtility.UrlEncode(pair.Value) + "&";
		    }
		    requestBody = requestBody.Trim('&');
		    return requestBody;
	    }

		/// <summary>
		/// Initializes a streaming rates session with the given instruments on the given account
		/// </summary>
		/// <param name="instruments">list of instruments to stream rates for</param>
		/// <param name="accountId">the account ID you want to stream on</param>
		/// <returns>the WebResponse object that can be used to retrieve the rates as they stream</returns>
        public static async Task<WebResponse> StartRatesSession( List<Instrument> instruments, int accountId )
        {
	        string instrumentList = "";
			foreach (var instrument in instruments)
			{
				instrumentList += instrument.instrument + ",";
			}
			// Remove the extra ,
			instrumentList = instrumentList.TrimEnd(',');
			instrumentList = Uri.EscapeDataString(instrumentList);

			string requestString = Server(EServer.StreamingRates) + "prices?accountId=" + accountId + "&instruments=" + instrumentList;
			
            HttpWebRequest request = WebRequest.CreateHttp( requestString );
            request.Method = "GET";
			request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;

			try
			{
				WebResponse response = await request.GetResponseAsync();
				return response;
			}
			catch (WebException ex)
			{
				var response = (HttpWebResponse)ex.Response;
				var stream = new StreamReader(response.GetResponseStream());
				var result = stream.ReadToEnd();
				throw new Exception(result);
			}
        }

		/// <summary>
		/// Initializes a streaming events session which will stream events for the given accounts
		/// </summary>
		/// <param name="accountId">the account IDs you want to stream on</param>
		/// <returns>the WebResponse object that can be used to retrieve the events as they stream</returns>
		public static async Task<WebResponse> StartEventsSession(List<int> accountId=null)
		{
			string requestString = Server(EServer.StreamingEvents) + "events";
			if (accountId != null && accountId.Count > 0)
			{
				string accountIds = "";
				foreach (var account in accountId)
				{
					accountIds += account + ",";
				}
				accountIds = accountIds.Trim(',');
				requestString += "?accountIds=" + WebUtility.UrlEncode(accountIds);
			}

			HttpWebRequest request = WebRequest.CreateHttp(requestString);
			request.Method = "GET";
			request.Headers[HttpRequestHeader.Authorization] = "Bearer " + AccessToken;

			try
			{
				WebResponse response = await request.GetResponseAsync();
				return response;
			}
			catch (WebException ex)
			{
				var response = (HttpWebResponse)ex.Response;
				var stream = new StreamReader(response.GetResponseStream());
				var result = stream.ReadToEnd();
				throw new Exception(result);
			}
		}

    }
}
