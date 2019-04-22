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
using System.Globalization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using QuantConnect.Data;
using System.Linq;

using QuantConnect.Data.Custom.Tiingo;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of using an external custom datasource. LEAN Engine is incredibly flexible and allows you to define your own data source.
    /// This includes any data source which has a TIME and VALUE. These are the *only* requirements. To demonstrate this we're loading Tiingo News data.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="news" />

    public class TiingoNewsAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            //Weather data we have is within these days:
            SetStartDate(2019, 4, 17);
            SetEndDate(2019, 4, 18);

            //Set the cash for the strategy:
            SetCash(100000);

            //Define the symbol and "type" of our generic data:
            AddData<News>("AAPL");
        }

        /// <summary>
        /// Event Handler for news: These news objects are created from our "News" type below and fired into this event handler.
        /// </summary>
        /// <param name="data">One(1) News Object for a symbol, streamed into our algorithm synchronised in time with our other data streams</param>
        public void OnData(News data)
        {

            Log(data.Title);

        }
    }
}
