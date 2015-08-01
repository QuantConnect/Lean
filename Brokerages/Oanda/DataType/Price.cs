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
namespace QuantConnect.Brokerages.Oanda.DataType
{
    /// <summary>
    /// Represents the Price object creating Orders for each instrument.
    /// </summary>
    public class Price
    {
        public enum State
        {
            Default,
            Increasing,
            Decreasing
        };

        public string instrument { get; set; }
        public string time;
        public double bid { get; set; }
        public double ask { get; set; }
	    public string status;
        public State state = State.Default;

	    public void update( Price update )
        {
            if ( this.bid > update.bid )
            {
                state = State.Decreasing;
            }
            else if ( this.bid < update.bid )
            {
                state = State.Increasing;
            }
            else
            {
                state = State.Default;
            }

            this.bid = update.bid;
            this.ask = update.ask;
            this.time = update.time;
        }
    }
}
