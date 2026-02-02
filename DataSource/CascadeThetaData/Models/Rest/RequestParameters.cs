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

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;

/// <summary>
/// Contains constant values for various request parameters used in API queries.
/// </summary>
public static class RequestParameters
{
    /// <summary>
    /// Represents the time interval in milliseconds since midnight Eastern Time (ET).
    /// Example values:
    /// - 09:30:00 ET = 34_200_000 ms
    /// - 16:00:00 ET = 57_600_000 ms
    /// </summary>
    public const string IntervalInMilliseconds = "ivl";

    /// <summary>
    /// Represents the start date for a query or request.
    /// </summary>
    public const string StartDate = "start_date";

    /// <summary>
    /// Represents the end date for a query or request.
    /// </summary>
    public const string EndDate = "end_date";
}
