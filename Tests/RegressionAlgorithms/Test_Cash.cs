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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using QuantConnect.Securities;

using System.Globalization;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Algorithm;
using QuantConnect.Orders;

namespace QuantConnect
{

    public class TestCashStrategy : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2013, 12, 31);
            SetCash(100000);
            AddData<CashType>("CASH");
        }

        public void OnData(CashType data)
        {
            try
            {
                //TEST: FULL SWEEP TESTING:
                if (Time == new DateTime(2013, 1, 1))
                {
                    Order("CASH", 100); // +100 Holdings
                }
                else if (Time == new DateTime(2013, 2, 1))
                {
                    Order("CASH", -50); // +50 Holdings
                }
                else if (Time == new DateTime(2013, 3, 1))
                {
                    Order("CASH", -100); // -50 Holdings
                }
                else if (Time == new DateTime(2013, 4, 1))
                {
                    Order("CASH", -50); // -100 Holdings
                }
                else if (Time == new DateTime(2013, 5, 1))
                {
                    Order("CASH", 50); // -50 Holdings
                }
                else if (Time == new DateTime(2013, 6, 1))
                {
                    Order("CASH", 100);// +50 Holdings
                }
                else if (Time == new DateTime(2013, 7, 1))
                {
                    Order("CASH", 50); // +100 Holdings
                }
                else if (Time == new DateTime(2013, 8, 1))
                {
                    Order("CASH", -50); // +50 Holdings
                }
                else if (Time == new DateTime(2013, 9, 1))
                {
                    Order("CASH", -100); // -50 Holdings
                }
                else if (Time == new DateTime(2013, 10, 1))
                {
                    Order("CASH", -50); // -100 Holdings
                }
                else if (Time == new DateTime(2013, 11, 1))
                {
                    Order("CASH", +50); // -50 Holdings
                }
                else if (Time == new DateTime(2013, 12, 1))
                {
                    Order("CASH", +100); // +50 Holdings
                }
                else if (Time == new DateTime(2013, 12, 15))
                {
                    Order("CASH", -50); // +0 Holdings
                }
            }
            catch (Exception err)
            {
                Debug("Err: " + err.Message);
            }
        }

        // PLOT OUR CASH POSITION:
        public override void OnEndOfDay()
        {
            try
            {
                Plot("Cash", Portfolio.Cash);
                Plot("PortfolioValue", Portfolio.TotalPortfolioValue);
                Plot("HoldingValue", Portfolio["CASH"].HoldingsValue);
                Plot("HoldingQuantity", Portfolio["CASH"].Quantity);
            }
            catch (Exception err)
            {
                Debug("Err: " + err.Message);
            }
        }
    }


    public class CashType : BaseData
    {
        public CashType()
        {
            this.Symbol = "CASH";
        }

        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            return "https://www.dropbox.com/s/oiliumoyqqj1ovl/2013-cash.csv?dl=1";
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            //New Bitcoin object
            CashType cash = new CashType();

            try
            {
                string[] data = line.Split(',');
                cash.Time = data[0].ParseDateTimeExactInvariant("yyyy-MM-dd");
                cash.Value = data[1].ConvertInvariant<decimal>();
            }
            catch { /* Do nothing, skip first title row */ }

            return cash;
        }
    }

}