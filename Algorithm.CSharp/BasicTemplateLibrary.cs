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

namespace QuantConnect
{
    /// <summary>
    /// Basic Template Library Class
    /// Library classes are snippets of code you can reuse between projects. They are added to projects on compile. This can be useful for reusing
    /// indicators, math components, risk modules etc. If you use a custom namespace make sure you add the correct using statement to the
    /// algorithm-user.
    /// </summary>
    /// <meta name="tag" content="using quantconnect" />
    public class BasicTemplateLibrary
    {
        /*
         * To use this library; add its namespace at the top of the page:
         * using QuantConnect
         *
         * Then instantiate the class:
         * var btl = new BasicTemplateLibrary();
         * btl.Add(1,2)
         */

        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Subtract(int a, int b)
        {
            return a - b;
        }
    }
}
