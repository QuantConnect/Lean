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
 *
*/

using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages.Paper;

namespace QuantConnect.Tests.Brokerages.Models;

/// <summary>
/// A customized PaperBrokerage implementation that allows manual control over the cash balance.
/// </summary>
internal class PaperBrokerageWithManualCashBalance : PaperBrokerage
{
    private decimal _cashBalance;

    /// <summary>
    /// Gets the current cash balance.
    /// </summary>
    public decimal CashBalance => _cashBalance;

    /// <summary>
    /// Increases the cash balance by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to add to the cash balance.</param>
    public void IncreaseCashBalance(decimal amount)
    {
        _cashBalance += amount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualCashBalancePaperBrokerage"/> class
    /// with the specified algorithm, job packet, and initial cash balance.
    /// </summary>
    /// <param name="algorithm">The algorithm instance.</param>
    /// <param name="job">The live node job packet.</param>
    /// <param name="initialCashBalance">The initial cash balance.</param>
    public PaperBrokerageWithManualCashBalance(IAlgorithm algorithm, LiveNodePacket job, decimal initialCashBalance) : base(algorithm, job)
    {
        _cashBalance = initialCashBalance;
    }

    /// <summary>
    /// Reduces the cash balance by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to subtract from the cash balance.</param>
    public void DecreaseCashBalance(decimal amount)
    {
        _cashBalance -= amount;
    }

    /// <summary>
    /// Gets the current cash balances held in the account.
    /// </summary>
    /// <returns>A list containing the current cash balance in USD.</returns>
    public override List<CashAmount> GetCashBalance()
    {
        return [new CashAmount(_cashBalance, Currencies.USD)];
    }
}
