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

using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.DownloaderDataProvider.Launcher.Models;

/// <summary>
/// Represents the configuration for downloading data for a universe of securities.
/// </summary>
public sealed class DataUniverseDownloadConfig : BaseDataDownloadConfig
{
    /// <summary>
    /// Gets the type of data universe download.
    /// </summary>
    public override Type DataType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataUniverseDownloadConfig"/> class using configuration settings.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an unsupported security type is specified.</exception>
    public DataUniverseDownloadConfig()
    {
        Resolution = Resolution.Daily;
        DataType = GetDataUniverseType(SecurityType);
    }

    /// <summary>
    /// Retrieves the corresponding data universe type based on the specified security type.
    /// </summary>
    /// <param name="securityType">The security type for which the data universe type is determined.</param>
    /// <returns>The corresponding <see cref="Type"/> of the data universe.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the specified <paramref name="securityType"/> is not supported.
    /// </exception>
    private static Type GetDataUniverseType(SecurityType securityType)
    {
        switch (securityType)
        {
            case SecurityType.Option:
            case SecurityType.IndexOption:
                return typeof(OptionUniverse);
            default:
                throw new NotImplementedException($"DataUniverseDownloadConfig.GetDataUniverseType(): The data universe type for SecurityType '{securityType}' is not implemented.");
        }
    }
}
