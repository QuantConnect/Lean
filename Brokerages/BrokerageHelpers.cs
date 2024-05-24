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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// The <c>BrokerageHelpers</c> class provides utility methods and 
    /// helper functions to assist with brokerage operations.
    /// </summary>
    /// <remarks>
    /// This class includes methods that simplify common tasks 
    /// related to brokerage activities, such as calculating fees,
    /// generating reports, and handling transactions.
    /// </remarks>
    public static class BrokerageHelpers
    {
        /// <summary>
        /// Determines if executing the specified order will cross the zero holdings threshold.
        /// </summary>
        /// <param name="holdingQuantity">The current quantity of holdings.</param>
        /// <param name="orderQuantity">The quantity of the order to be evaluated.</param>
        /// <returns>
        /// <c>true</c> if the order will change the holdings from positive to negative or vice versa; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method checks if the order will result in a position change from positive to negative holdings or from negative to positive holdings.
        /// </remarks>
        public static bool OrderCrossesZero(decimal holdingQuantity, decimal orderQuantity)
        {
            //We're reducing position or flipping:
            if (holdingQuantity > 0 && orderQuantity < 0)
            {
                if ((holdingQuantity + orderQuantity) < 0)
                {
                    //We don't have enough holdings so will cross through zero:
                    return true;
                }
            }
            else if (holdingQuantity < 0 && orderQuantity > 0)
            {
                if ((holdingQuantity + orderQuantity) > 0)
                {
                    //Crossed zero: need to split into 2 orders:
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Calculates the quantities needed to close the current position and establish a new position based on the provided order.
        /// </summary>
        /// <param name="holdingQuantity">The quantity currently held in the position that needs to be closed.</param>
        /// <param name="orderQuantity">The quantity defined in the new order to be established.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item>
        /// <description>The quantity needed to close the current position (negative value).</description>
        /// </item>
        /// <item>
        /// <description>The quantity needed to establish the new position.</description>
        /// </item>
        /// </list>
        /// </returns>
        public static (decimal closePostionQunatity, decimal newPositionQuantity) GetQuantityOnCrossPosition(decimal holdingQuantity, decimal orderQuantity)
        {
            // first we need an order to close out the current position
            var firstOrderQuantity = -holdingQuantity;
            var secondOrderQuantity = orderQuantity - firstOrderQuantity;

            return (firstOrderQuantity, secondOrderQuantity);
        }
    }
}
