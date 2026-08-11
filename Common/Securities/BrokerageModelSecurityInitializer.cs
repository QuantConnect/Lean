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

using System;
using Python.Runtime;
using QuantConnect.Brokerages;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityInitializer"/> that initializes a security
    /// by settings the <see cref="Security.FillModel"/>, <see cref="Security.FeeModel"/>,
    /// <see cref="Security.SlippageModel"/>, and the <see cref="Security.SettlementModel"/> properties
    /// </summary>
    public class BrokerageModelSecurityInitializer : ISecurityInitializer
    {
        private readonly IBrokerageModel _brokerageModel;
        private readonly ISecuritySeeder _securitySeeder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageModelSecurityInitializer"/> class
        /// for the specified algorithm
        /// </summary>
        public BrokerageModelSecurityInitializer()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageModelSecurityInitializer"/> class
        /// for the specified algorithm
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to initialize the security models</param>
        /// <param name="securitySeeder">An <see cref="ISecuritySeeder"/> used to seed the initial price of the security</param>
        public BrokerageModelSecurityInitializer(IBrokerageModel brokerageModel, ISecuritySeeder securitySeeder)
        {
            _brokerageModel = brokerageModel;
            _securitySeeder = securitySeeder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageModelSecurityInitializer"/> class
        /// for the specified algorithm
        /// </summary>
        /// <param name="brokerageModel">The brokerage model used to initialize the security models</param>
        /// <param name="securitySeeder">An <see cref="ISecuritySeeder"/> instance or a Python callable, like
        /// the algorithm's get_last_known_price method, used to seed the initial price of the security.
        /// It can also be None, in which case no seeding is performed</param>
        public BrokerageModelSecurityInitializer(IBrokerageModel brokerageModel, PyObject securitySeeder)
        {
            _brokerageModel = brokerageModel;
            using (Py.GIL())
            {
                if (securitySeeder is null || securitySeeder.IsNone())
                {
                    _securitySeeder = SecuritySeeder.Null;
                }
                else if (securitySeeder.TryConvert<ISecuritySeeder>(out var seeder))
                {
                    _securitySeeder = seeder;
                }
                else if (securitySeeder.IsCallable())
                {
                    // Wrap python callables, like a get_last_known_price method reference or a lambda,
                    // the same way QCAlgorithm.SetSecurityInitializer accepts a function
                    _securitySeeder = new FuncSecuritySeeder(securitySeeder);
                }
                else
                {
                    throw new ArgumentException(
                        $"BrokerageModelSecurityInitializer(): unsupported security seeder '{securitySeeder.GetPythonType().Name}'. " +
                        "Please provide an ISecuritySeeder instance (e.g. FuncSecuritySeeder), a callable taking a Security " +
                        "and returning its seed data (e.g. self.get_last_known_price), or None to skip seeding.");
                }
            }
        }

        /// <summary>
        /// Initializes the specified security by setting up the models
        /// </summary>
        /// <param name="security">The security to be initialized</param>
        public virtual void Initialize(Security security)
        {
            // Sets the security models
            security.FillModel = _brokerageModel.GetFillModel(security);
            security.FeeModel = _brokerageModel.GetFeeModel(security);
            security.SlippageModel = _brokerageModel.GetSlippageModel(security);
            security.SettlementModel = _brokerageModel.GetSettlementModel(security);
            security.BuyingPowerModel = _brokerageModel.GetBuyingPowerModel(security);
            security.MarginInterestRateModel = _brokerageModel.GetMarginInterestRateModel(security);
            // Sets the leverage after the buying power model. Otherwise we would set the leverage of the default model.
            security.SetLeverage(_brokerageModel.GetLeverage(security));
            security.SetShortableProvider(_brokerageModel.GetShortableProvider(security));

            _securitySeeder.SeedSecurity(security);
        }
    }
}
