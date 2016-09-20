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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantLib;

namespace QuantConnect.Util
{
    public static class QuantLibUtils
    {
        public static Date ToQLDate (this DateTime date)
        {
            return new Date(date.Day, ToDLMonth(date.Month), date.Year);
        }

        static Month ToDLMonth(int month)
        {
            switch (month)
            {
                case 1: return Month.January;
                case 2: return Month.February;
                case 3: return Month.March;
                case 4: return Month.April;
                case 5: return Month.May;
                case 6: return Month.June;
                case 7: return Month.July;
                case 8: return Month.August;
                case 9: return Month.September;
                case 10: return Month.October;
                case 11: return Month.November;
                case 12: return Month.December;
                default:
                    throw new ArgumentException("Input argument was not in the range of 1 to 12", "month");
            }
        }
    }
}
