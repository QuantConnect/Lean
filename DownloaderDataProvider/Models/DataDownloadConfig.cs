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

using QuantConnect.Util;
using QuantConnect.Configuration;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;

namespace QuantConnect.DownloaderDataProvider.Launcher.Models;

/// <summary>
/// Represents the configuration for downloading data.
/// </summary>
public sealed class DataDownloadConfig : BaseDataDownloadConfig
{
    /// <summary>
    /// Gets the type of data download.
    /// </summary>
    public override Type DataType { get => LeanData.GetDataType(Resolution, TickType); }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataDownloadConfig"/> class.
    /// </summary>s
    public DataDownloadConfig()
    {
        TickType = ParseEnum<TickType>(Config.Get(DownloaderCommandArguments.CommandDataType));
        Resolution = ParseEnum<Resolution>(Config.Get(DownloaderCommandArguments.CommandResolution));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataDownloadConfig"/> class with the specified parameters.
    /// </summary>
    /// <param name="tickType">The type of tick data to be downloaded.</param>
    /// <param name="securityType">The type of security for which data is being downloaded.</param>
    /// <param name="resolution">The resolution of the data being downloaded.</param>
    /// <param name="startDate">The start date for the data download range.</param>
    /// <param name="endDate">The end date for the data download range.</param>
    /// <param name="marketName">The name of the market from which the data is being downloaded.</param>
    /// <param name="symbols">A list of symbols for which data is being downloaded.</param>
    public DataDownloadConfig(TickType tickType, SecurityType securityType, Resolution resolution, DateTime startDate, DateTime endDate, string marketName, List<Symbol> symbols)
        : base(tickType, securityType, resolution, startDate, endDate, marketName, symbols)
    { }
}
