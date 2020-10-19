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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Configuration;


namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Test class for all nodes/ endpoints:
    /// create, read, update, delete, stop
    /// </summary>
    [TestFixture, Ignore("These tests require an account, token, organization ID and billing permissions")]
    public class NodeTests
    {
        private int _testAccount;
        private string _testToken;
        private string _testOrganization;
        private string _dataFolder;
        private Api.Api _api;

        /// <summary>
        /// Setup for this text fixture, will create API connection and load in configuration
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            _testAccount = Config.GetInt("job-user-id", 1);
            _testToken = Config.Get("api-access-token", "EnterTokenHere");
            _testOrganization = Config.Get("job-organization-id", "EnterOrgHere");
            _dataFolder = Config.Get("data-folder");

            _api = new Api.Api();
            _api.Initialize(_testAccount, _testToken, _dataFolder);
        }

        /// <summary>
        /// Create a new backtest node with 2 CPU cores and 8GB of RAM check for successful
        /// creation in API response.
        /// </summary>
        [Test]
        public void CreateNewNode()
        {
            var sku = new SKU(2, 8, NodeType.Backtest);
            var nodeName = $"{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second}-Pinocho";
            var newNode = _api.CreateNode(nodeName, _testOrganization, sku);
            Assert.IsTrue(newNode.Success);
        }

        /// <summary>
        /// Read all the nodes from the organization and check for a successful reply
        /// from the API.
        /// </summary>
        [Test]
        public void ReadNode()
        {
            var result = _api.ReadNodes(_testOrganization);
            Assert.IsTrue(result.Success);
        }

        /// <summary>
        /// Create, Read, Update, and Delete a node!
        /// </summary>
        /// <param name="cores">Number of cores for the node</param>
        /// <param name="memory">Amount of RAM in GB for the node</param>
        /// <param name="target">Target environment for the node</param>
        [TestCase(2, 8, NodeType.Backtest)]
        [TestCase(0, 0, NodeType.Live)]
        [TestCase(8, 16, NodeType.Research)]
        public void CRUDNodes(int cores, int memory, NodeType target)
        {
            var sku = new SKU(cores, memory, target);
            var nodeName = $"{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second}-Pinocho";
            var nodeName2 = $"{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second}-Monstro";

            // First create a new node
            var createdNode = _api.CreateNode(nodeName, _testOrganization, sku);

            //Check for the nodes existance using the helper function
            var foundNode = FindNodeByName(nodeName);
            Assert.IsNotNull(foundNode);

            //Update that node with a new name
            var updateNodeRequest = _api.UpdateNode(foundNode.Id, nodeName2, _testOrganization);
            Assert.IsTrue(updateNodeRequest.Success);

            //Read again and check for the new name (nodeName2)
            foundNode = FindNodeByName(nodeName2);
            Assert.IsNotNull(foundNode);

            //Delete this node
            var deleteNodeRequest = _api.DeleteNode(foundNode.Id, _testOrganization);
            Assert.IsTrue(deleteNodeRequest.Success);

            //Read again and ensure the node does not exist.
            foundNode = FindNodeByName(nodeName2);
            Assert.IsNull(foundNode);
        }

        /// <summary>
        /// Helper function for finding a node by name, used by tests that are looking
        /// for a certain node.
        /// With some small adjustments could be moved to an api function.
        /// </summary>
        /// <param name="name">Name of the node</param>
        /// <returns>The Node if found, null if not</returns>
        public Node FindNodeByName(string name)
        {
            Node result = null;
            var readNodeRequest = _api.ReadNodes(_testOrganization);
            var allNodes = readNodeRequest.BacktestNodes.Concat(readNodeRequest.LiveNodes).Concat(readNodeRequest.ResearchNodes);

            foreach (var Node in allNodes)
            {
                if (Node.Name == name)
                {
                    result = Node;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Test to ensure SKUs are generated correctly for a given Node configuration
        /// </summary>
        /// <param name="cores">Number of cores</param>
        /// <param name="memory">Size of memory</param>
        /// <param name="target">Target node environment</param>
        /// <param name="expectedResult">Expected Result</param>
        [TestCase(0, 0, NodeType.Live, "L-MICRO")]
        [TestCase(1, 1, NodeType.Live, "L1-1")]
        [TestCase(1, 2, NodeType.Live, "L1-2")]
        [TestCase(1, 4, NodeType.Live, "L1-4")]
        [TestCase(2, 8, NodeType.Backtest, "B2-8")]
        [TestCase(4, 12, NodeType.Backtest, "B4-12")]
        [TestCase(1, 4, NodeType.Research, "R1-4")]
        [TestCase(2, 8, NodeType.Research, "R2-8")]
        [TestCase(4, 12, NodeType.Research, "R4-12")]
        [TestCase(8, 16, NodeType.Research, "R8-16")]
        public void SkuIsGeneratedCorrectly(int cores, int memory, NodeType target, string expectedResult)
        {
            var SKU = new SKU(cores, memory, target);
            Assert.IsTrue(SKU.ToString() == expectedResult);
        }

        /// <summary>
        /// Read in the list of nodes from an organizations and stop any
        /// that are currently busy
        /// </summary>
        [Test]
        public void ReadAndStop()
        {
            // Then read the nodes from the org
            var readNodeRequest = _api.ReadNodes(_testOrganization);
            Assert.IsTrue(readNodeRequest.Success);

            //Iterate through all nodes and stop them if they are running
            var allNodes = readNodeRequest.BacktestNodes.Concat(readNodeRequest.LiveNodes).Concat(readNodeRequest.ResearchNodes);
            foreach (var Node in allNodes)
            {
                // If it is busy then stop it
                if (Node.Busy)
                {
                    var stopNodeRequest = _api.StopNode(Node.Id, _testOrganization);
                    Assert.IsTrue(stopNodeRequest.Success);
                }
            }
        }
    }
}
