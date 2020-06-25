using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.API;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;


namespace QuantConnect.Tests.API
{
    [TestFixture, Ignore("These tests require an account, token, and organization ID")]
    public class NodeTests
    {
        private int _testAccount = 1;
        private string _testToken = "ec87b337ac970da4cbea648f24f1c851";
        private string _testOrganization = "enter Org ID here";
        private string _dataFolder = Config.Get("data-folder");
        private Api.Api _api;

        /// <summary>
        /// Run before every test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _api = new Api.Api();
            _api.Initialize(_testAccount, _testToken, _dataFolder);
        }

        /// <summary>
        /// Test to creating a New Node
        /// </summary>
        [Test]
        public void CreateNewNode()
        {
            string sku = Node.GetSKU(2, 8, "backtest");
            var nodeName = $"{DateTime.UtcNow.Minute}:{DateTime.UtcNow.Second}-Pinocho";
            var newNode = _api.CreateNode(nodeName, _testOrganization, sku);
            Assert.IsNotNull(newNode);
        }

        /// <summary>
        /// Test to creating a New Node
        /// </summary>
        [Test]
        public void ReadNode()
        {
            var result = _api.ReadNode(_testOrganization);
            Assert.IsTrue(result.Success);
        }

        /// <summary>
        /// Create, Read, Update, and Delete a node!
        /// </summary>
        /// <param name="sku">Node Type Identifier</param>
        [TestCase("B2-8")]
        [TestCase("L-micro")]
        [TestCase("r8-16")]
        public void CRUDNodes(string sku)
        {
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
        /// Helper function for finding a node by name, used by tests that are looking for a certain node.
        /// With some small adjustments could be moved to an api function.
        /// </summary>
        /// <param name="name">Node name</param>
        public Node FindNodeByName(string name)
        {
            Node result = null;
            var readNodeRequest = _api.ReadNode(_testOrganization);
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
        /// Generate some SKUs and ensure they are correct
        /// </summary>
        /// <param name="cores">Number of cores</param>
        /// <param name="memory">Size of memory</param>
        /// <param name="target">Target node environment</param>
        /// <param name="expectedResult">Expected Result</param>
        [TestCase(0, 0, "Live", "L-micro")]
        [TestCase(1, 1, "Live", "L1-1")]
        [TestCase(1, 2, "live", "l1-2")]
        [TestCase(1, 4, "L", "L1-4")]
        [TestCase(2, 8, "B", "B2-8")]
        [TestCase(4, 12, "Backtest", "B4-12")]
        [TestCase(1, 4, "R", "R1-4")]
        [TestCase(2, 8, "R", "R2-8")]
        [TestCase(4, 12, "Research", "R4-12")]
        [TestCase(8, 16, "research", "r8-16")]
        public void SkuIsGeneratedCorrectly(int cores, int memory, string target, string expectedResult)
        {
            string SKU = Node.GetSKU(cores, memory, target);
            Assert.IsTrue(SKU == expectedResult);
        }

        /// <summary>
        /// Read and stop all nodes
        /// </summary>
        [Test]
        public void ReadAndStop()
        {
            // Then read the nodes from the org
            var readNodeRequest = _api.ReadNode(_testOrganization);
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
