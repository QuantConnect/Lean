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
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.YahooDownloader
{
    /// <summary>
    /// Class for fetching token (cookie and crumb) from Yahoo Finance
    /// </summary>
    public class Token
    {
        public static string Cookie { get; private set; }
        public static string Crumb { get; private set; }

        private static Regex _regexCrumb;

        /// <summary>
        /// Refresh cookie and crumb value
        /// </summary>
        /// <param name="symbol">Stock ticker symbol</param>
        /// <returns></returns>
        public static bool Refresh(string symbol)
        {

            try
            {
                Cookie = "";
                Crumb = "";

                var url = $"https://finance.yahoo.com/quote/{symbol}?p={symbol}";

                var request = (HttpWebRequest)WebRequest.Create(url);

                request.CookieContainer = new CookieContainer();
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var cookie = response.GetResponseHeader("Set-Cookie").Split(';')[0];

                    var html = "";

                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            html = new StreamReader(stream).ReadToEnd();
                        }
                    }

                    if (html.Length < 5000)
                    {
                        return false;
                    }
                    var crumb = GetCrumb(html);

                    if (crumb != null)
                    {
                        Cookie = cookie;
                        Crumb = crumb;
                        Log.Debug($"Crumb: '{crumb}', Cookie: '{cookie}'");
                        return true;
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            return false;

        }

        /// <summary>
        /// Reset the value for Cookie and Crumb
        /// </summary>
        public static void Reset()
        {
            Crumb = "";
            Cookie = "";
        }

        /// <summary>
        /// Get crumb value from HTML
        /// </summary>
        /// <param name="html">HTML code</param>
        /// <returns></returns>
        private static string GetCrumb(string html)
        {

            string crumb = null;

            try
            {
                //initialize on first time use
                if (_regexCrumb == null)
                {
                    _regexCrumb = new Regex("CrumbStore\":{\"crumb\":\"(?<crumb>.+?)\"}",
                        RegexOptions.CultureInvariant | RegexOptions.Compiled);
                }

                var matches = _regexCrumb.Matches(html);

                if (matches.Count > 0)
                {
                    crumb = matches[0].Groups["crumb"].Value;
                }
                else
                {
                    Log.Debug("Regex no match");
                }

                //prevent regex memory leak
                matches = null;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            GC.Collect();
            return crumb;
        }
    }
}
