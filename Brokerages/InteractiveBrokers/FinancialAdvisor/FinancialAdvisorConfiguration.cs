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
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using IBApi;
using QuantConnect.Brokerages.InteractiveBrokers.Client;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages.InteractiveBrokers.FinancialAdvisor
{
    /// <summary>
    /// Contains configuration data for a Financial Advisor
    /// </summary>
    public class FinancialAdvisorConfiguration
    {
        // Financial Advisor configuration data
        private List<AccountAlias> _accountAliases = new List<AccountAlias>();
        private List<Group> _accountGroups = new List<Group>();
        private List<AllocationProfile> _allocationProfiles = new List<AllocationProfile>();

        /// <summary>
        /// The financial advisor master account code
        /// </summary>
        public string MasterAccount { get; private set; } = string.Empty;

        /// <summary>
        /// The sub-account codes managed by the financial advisor
        /// </summary>
        public IEnumerable<string> SubAccounts
        {
            get { return _accountAliases.Where(x => x.Account != MasterAccount).Select(x => x.Account); }
        }

        /// <summary>
        /// Clears this instance of <see cref="FinancialAdvisorConfiguration"/>
        /// </summary>
        public void Clear()
        {
            MasterAccount = string.Empty;

            _accountAliases.Clear();
            _accountGroups.Clear();
            _allocationProfiles.Clear();
        }

        /// <summary>
        /// Downloads the financial advisor configuration
        /// </summary>
        /// <param name="client">The IB client</param>
        /// <returns>true if successfully completed</returns>
        public bool Load(InteractiveBrokersClient client)
        {
            var faResetEvent = new AutoResetEvent(false);

            var xmlGroups = string.Empty;
            var xmlProfiles = string.Empty;
            var xmlAliases = string.Empty;

            EventHandler<ReceiveFaEventArgs> handler = (sender, e) =>
            {
                switch (e.FaDataType)
                {
                    case Constants.FaAliases:
                        xmlAliases = e.FaXmlData;
                        break;

                    case Constants.FaGroups:
                        xmlGroups = e.FaXmlData;
                        break;

                    case Constants.FaProfiles:
                        xmlProfiles = e.FaXmlData;
                        break;
                }

                faResetEvent.Set();
            };

            client.ReceiveFa += handler;

            // request FA Aliases
            Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): requesting FA Aliases");
            client.ClientSocket.requestFA(Constants.FaAliases);
            if (!faResetEvent.WaitOne(2000))
            {
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): Download FA Aliases failed. Operation took longer than 2 seconds.");
                return false;
            }

            // request FA Groups
            Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): requesting FA Groups");
            client.ClientSocket.requestFA(Constants.FaGroups);
            if (!faResetEvent.WaitOne(2000))
            {
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): Download FA Groups failed. Operation took longer than 2 seconds.");
                return false;
            }

            // request FA Profiles
            Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): requesting FA Profiles");
            client.ClientSocket.requestFA(Constants.FaProfiles);
            if (!faResetEvent.WaitOne(2000))
            {
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): Download FA Profiles failed. Operation took longer than 2 seconds.");
                return false;
            }

            client.ReceiveFa -= handler;

            // load FA configuration
            var serializer = new XmlSerializer(typeof(List<AccountAlias>), new XmlRootAttribute("ListOfAccountAliases"));
            using (var stringReader = new StringReader(xmlAliases))
            {
                _accountAliases = (List<AccountAlias>)serializer.Deserialize(stringReader);
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): FA Aliases found: " + _accountAliases.Count);
            }

            serializer = new XmlSerializer(typeof(List<Group>), new XmlRootAttribute("ListOfGroups"));
            using (var stringReader = new StringReader(xmlGroups))
            {
                _accountGroups = (List<Group>)serializer.Deserialize(stringReader);
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): FA Groups found: " + _accountGroups.Count);
            }

            serializer = new XmlSerializer(typeof(List<AllocationProfile>), new XmlRootAttribute("ListOfAllocationProfiles"));
            using (var stringReader = new StringReader(xmlProfiles))
            {
                _allocationProfiles = (List<AllocationProfile>)serializer.Deserialize(stringReader);
                Log.Trace("InteractiveBrokersBrokerage.DownloadFinancialAdvisorConfiguration(): FA Profiles found: " + _allocationProfiles.Count);
            }

            // save the master account code
            var entry = _accountAliases.FirstOrDefault(x => InteractiveBrokersBrokerage.IsMasterAccount(x.Account));
            if (entry == null)
            {
                throw new Exception("The Financial Advisor master account was not found.");
            }

            MasterAccount = entry.Account;

            return true;
        }
    }
}
