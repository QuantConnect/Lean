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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Defines the different types of Correlation
    /// </summary>  
    public enum CorrelationIndicatorType
    {
        /// <summary>
        /// Pearson Correlation (Product-Moment Correlation):
        /// Measures the linear relationship between two datasets. The coefficient ranges from -1 to 1.
        /// A value of 1 indicates a perfect positive linear relationship, -1 indicates a perfect
        /// negative linear relationship, and 0 indicates no linear relationship.
        /// It assumes that both datasets are normally distributed and the relationship is linear.
        /// It is sensitive to outliers which can affect the correlation significantly.
        /// </summary>
        Pearson,
        /// <summary>
        /// Pearson Correlation (Product-Moment Correlation):
        /// Measures the linear relationship between two datasets. The coefficient ranges from -1 to 1.
        /// A value of 1 indicates a perfect positive linear relationship, -1 indicates a perfect
        /// negative linear relationship, and 0 indicates no linear relationship.
        /// It assumes that both datasets are normally distributed and the relationship is linear.
        /// It is sensitive to outliers which can affect the correlation significantly.
        /// </summary>
        Spearman

    }
}
