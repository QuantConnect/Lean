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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuantConnect.Data.Custom.TradingEconomics
{
    /// <summary>
    /// Provides methods to filter and standardize Trading Economics calendar event names.
    /// </summary>
    public static class TradingEconomicsEventFilter
    {
        // Companies that publish reports. We move these to the beginning of the string if encountered.
        private static readonly List<string> _companyReports = new List<string> { "westpac", "markit" };

        // Strings to replace using the string `Replace(...)` method (no regex).
        // Key is current, value is the replacement
        private static readonly Dictionary<string, string> _rawReplace = new Dictionary<string, string>
        {
            {"(r)", "revised"},
            {" t-bill", " bill"},
            {" n.s.a", " not seasonally adjusted"},
            {" nsa", " not seasonally adjusted"},
            {" s.a", " seasonally adjusted"},
            {"aig performance of", "aig performance"},
            {"baker hughes total rig count", "baker hughes oil rig count"},
            {"trade-final", "trade final"},
            {"chain stores wow", "chain stores sales wow"},
            {" cost ", " costs "},
            {" serv ", " services "},
            {" constr ", " construction "},
            {" approval ", " approvals "},
            {" perfor ", " performance "},
            {" perf ", " performance "},
            {"weeks ", "week "},
            {"2 est", "second estimate"},
            {"2nd est", "second estimate"},
            {"1 est", "first estimate"},
            {"1st est", "first estimate"},
            {" advertisment ", " advertisements "},
            {" advertisments ", " advertisements "},
            {" advertisement ", " advertisements "},
            {"labour", "labor"},
            {"ai group", "aig performance"},
            {" mfg ", " manufacturing "},
            {"api crude oil stock change", "api weekly crude stock change"},
            {"api crude stock change", "api weekly crude stock change"},
            {"api weekly crude oil stock", "api weekly crude stock change"},
            {"api gasoline stock change", "api weekly gasoline stock change"},
            {" monitor - all ", " "},
            {" monitor yoy", " yoy"},
            {"pce prices", "pce price index"},
            {" pce mom", " pce price index mom"},
            {" pce yoy", " pce price index yoy"},
            {"core personal consumption expenditure - price index mom", "core personal consumption expenditure price index mom"},
            {"core personal consumption expenditure yoy", "core personal consumption expenditure price index yoy"},
            {"core personal consumption expenditures", "core personal consumption expenditure price index"},
            {"consumption expenditures", "consumption expenditure"},
            {"export price index", "export prices"},
            {" ex-", " excluding "},
            {" ex ", " excluding "},
            {" exl.", " excluding "},
            {" excl. ", " excluding "},
            {" incl. ", " including "},
            {"cb leading i", "cb leading economic i"},
            {"portfolio investment", "investment"},
            {"commbank australia", "commbank"},
            {"fulltime", "full time"},
            {"harmonised", "harmonized"},
            {"import price index", "import prices"},
            {"capacity capacity", "capacity"},
            {"labor costs qoq", "labor costs index qoq"},
            {" prod ", " production "},
            {"michigan expectations prel", "michigan consumer expectations preliminary"},
            {"nab quarterly", "nab"},
            {"new orders manufacturing", "new orders"},
            {"raw material price", "raw materials price index"},
            {"raw materials prices", "raw materials price index"},
            {"&", "and"},
            {"capex", "capital expenditure"},
            {"tankan large all", "tankan all large"},
            {"manufacturers", "manufacturing"},
            {"aig construction", "aig performance construction"},
            {"aig manufacturing", "aig performance manufacturing"},
            {"aig services", "aig performance services"},
            {" ads ", " advertisements "},
            {"industrail", "industrial"},
            {"nonfarm", "non farm"},
            {"bank austria", "bank of austria"},
            {"consumer confidence index", "consumer confidence"},
            {"2014", ""},
            {"2015", ""}
        };

        // Strings to replace using a regular expression match.
        // Key is regular expression match, value is replacement.
        private static readonly Dictionary<string, string> _regexReplace = new Dictionary<string, string>
        {
            { @" transp[.]* ", " transportation " },
            { @"^priv[.]* ", "private " },
            { @"cons[. ]*conf[.]*", "consumer confidence" },
            { @"^svme [a-z ]*pmi", "svme purchasing managers index" },
            { @"^svme[\- ]*purchasing managers index", "svme manufacturing pmi" },
            { @"core personal cons[.]* exp[.]*", "core personal consumption expenditure price index" },
            { @"pce ", "personal consumption expenditure " },
            { @"personal consumption expenditure yoy", "personal consumption expenditure price index yoy" },
            { @"business nz$", "business nz pmi" },
            { @"full time employment$", "full time employment change" },
            { @"mm$", "mom" },
            { @"chg$", "change" },
            { @" sa$", " seasonally adjusted" },
            { @"indicator$", "indicators" },
            { @"sentiment indicators$", "sentiment" },
            { @"expectation$", "expectations" },
            { @"approval$", "approvals" },
            { @"advertisment$", "advertisements" },
            { @"advertisments$", "advertisements" },
            { @"advertisement$", "advertisements" },
            { @" cost$", "costs" },
            { @" f[.]$", "final" },
            { @" p$", "preliminary" },
            { @" p\.$", "preliminary" },
            { @"prel$", "preliminary" },
            { @"prel.$", "preliminary" },
            { @"prelim$", "preliminary" },
            { @"prelim.$", "preliminary" },
            { @" exp[.]* ", " expenditure " }
        };

        // Standalone words to replace using the SingleWordFilter method.
        // Key is match, value is replacement.
        private static readonly Dictionary<string, string> _singleWordFilters = new Dictionary<string, string>
        {
            { "pmi", "purchasing managers index" },
            { "boc", "bank of canada" },
            { "boe", "bank of england" },
            { "boj", "bank of japan" },
            { "bsi", "business survey index" },
            { "cpi", "consumer price index" },
            { "ppi", "producer price index" },
            { "rpi", "retail price index" },
            { "cpif", "consumer price index fixed interest rate" },
            { "fdi", "foreign direct investment" },
            { "qe", "quantitative easing" },
            { "mbs", "mortgage backed securities" },
            { "psi", "performance of services index" },
            { "mi", "melbourne institute" },
            { "est", "estimate" },
            { "1st", "first" },
            { "2nd", "second" },
        };

        /// <summary>
        /// Convert and normalizes the Trading Economics calendar "Event" field.
        /// </summary>
        /// <param name="eventName">Raw event name</param>
        /// <returns>Event name normalized</returns>
        public static string FilterEvent(string eventName)
        {
            var filteredName = eventName.Trim().ToLowerInvariant();

            foreach (var kvp in _rawReplace)
            {
                filteredName = filteredName.Replace(kvp.Key, kvp.Value);
            }
            foreach (var kvp in _regexReplace)
            {
                filteredName = Regex.Replace(filteredName, kvp.Key, kvp.Value);
            }

            filteredName = filteredName.Replace("-", " ").Replace("/", " ");
            filteredName = Regex.Replace(filteredName, @"[^a-zA-Z0-9 ]", "");

            filteredName = RemoveExtraSpaces(filteredName);
            filteredName = MoveCompanyToStart(filteredName);

            foreach (var kvp in _singleWordFilters)
            {
                filteredName = SingleWordFilter(filteredName, kvp.Key, kvp.Value);
            }

            return filteredName.Trim();
        }

        /// <summary>
        /// Moves the company contained within the event name to the start of the string
        /// </summary>
        /// <param name="filteredName">Filtered string (all lowercase)</param>
        /// <returns>New string with company at the start of the string</returns>
        private static string MoveCompanyToStart(string filteredName)
        {
            var words = filteredName.Split(' ').ToList();
            foreach (var company in _companyReports)
            {
                if (!filteredName.Contains(company))
                {
                    continue;
                }

                var i = 0;
                foreach (var word in words)
                {
                    if (word == company)
                    {
                        break;
                    }

                    i++;
                }

                if (i != 0)
                {
                    words.RemoveAt(i);
                    words.Insert(0, company);
                    filteredName = string.Join(" ", words);
                    break;
                }
            }

            return filteredName;
        }

        /// <summary>
        /// Replaces any word that is self-contained and not a part of another word.
        /// </summary>
        /// <param name="filteredName">String to apply replacements to</param>
        /// <param name="oldWord">Old word</param>
        /// <param name="newWord">New word to replace old word with</param>
        /// <returns>New string with self-contained word replaced</returns>
        private static string SingleWordFilter(string filteredName, string oldWord, string newWord)
        {
            filteredName = Regex.Replace(filteredName, @"^" + oldWord + " ", $"{newWord} ");
            filteredName = Regex.Replace(filteredName, @" " + oldWord + " ", $" {newWord} ");
            filteredName = Regex.Replace(filteredName, @" " + oldWord + "$", $" {newWord}");

            return filteredName;
        }

        /// <summary>
        /// Removes any instance(s) of duplicate spaces
        /// </summary>
        /// <param name="word">Word to remove extra spaces for</param>
        /// <returns>Word with no duplicate spaces</returns>
        private static string RemoveExtraSpaces(string word)
        {
            while (word.Contains("  "))
            {
                word = word.Replace("  ", " ");
            }

            return word;
        }
    }
}
