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
using QuantConnect.Data.Market;

namespace QuantConnect.Data.UniverseSelection;

/// <summary>
/// Represents derivative market data including trade and open interest information.
/// </summary>
public class DerivativeUniverseData
{
    private readonly Symbol _symbol;
    private decimal _open;
    private decimal _high;
    private decimal _low;
    private decimal _close;
    private decimal _volume;
    private decimal? _openInterest;

    /// <summary>
    /// Initializes a new instance of <see cref="DerivativeUniverseData"/> using open interest data.
    /// </summary>
    /// <param name="openInterest">The open interest data.</param>
    public DerivativeUniverseData(OpenInterest openInterest)
    {
        _symbol = openInterest.Symbol;
        _openInterest = openInterest.Value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DerivativeUniverseData"/> using trade bar data.
    /// </summary>
    /// <param name="tradeBar">The trade bar data.</param>
    public DerivativeUniverseData(TradeBar tradeBar)
    {
        _symbol = tradeBar.Symbol;
        _open = tradeBar.Open;
        _high = tradeBar.High;
        _low = tradeBar.Low;
        _close = tradeBar.Close;
        _volume = tradeBar.Volume;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DerivativeUniverseData"/> using quote bar data.
    /// </summary>
    /// <param name="quoteBar">The quote bar data.</param>
    public DerivativeUniverseData(QuoteBar quoteBar)
    {
        _symbol = quoteBar.Symbol;
        _open = quoteBar.Open;
        _high = quoteBar.High;
        _low = quoteBar.Low;
        _close = quoteBar.Close;
    }

    /// <summary>
    /// Updates the instance with new trade bar data.
    /// </summary>
    /// <param name="tradeBar">The new trade bar data.</param>
    /// <exception cref="ArgumentNullException">Thrown when tradeBar is null.</exception>
    public void UpdateByTradeBar(TradeBar tradeBar)
    {
        // If price data has already been initialized (likely from a QuoteBar)
        if (_open != 0 || _high != 0 || _low != 0 || _close != 0)
        {
            _volume = tradeBar.Volume;
            return;
        }

        _open = tradeBar.Open;
        _high = tradeBar.High;
        _low = tradeBar.Low;
        _close = tradeBar.Close;
    }

    /// <summary>
    /// Updates the instance with new quote bar data.
    /// </summary>
    /// <param name="quoteBar">The new quote bar data.</param>
    public void UpdateByQuoteBar(QuoteBar quoteBar)
    {
        _open = quoteBar.Open;
        _high = quoteBar.High;
        _low = quoteBar.Low;
        _close = quoteBar.Close;
    }

    /// <summary>
    /// Updates the instance with new open interest data.
    /// </summary>
    /// <param name="openInterest">The new open interest data.</param>
    /// <exception cref="ArgumentNullException">Thrown when openInterest is null.</exception>
    public void UpdateByOpenInterest(OpenInterest openInterest)
    {
        _openInterest = openInterest.Value;
    }

    /// <summary>
    /// Converts the current data to a CSV format string.
    /// </summary>
    /// <returns>A CSV formatted string representing the data.</returns>
    public string ToCsv()
    {
        return OptionUniverse.ToCsv(_symbol, _open, _high, _low, _close, _volume, _openInterest, null, NullGreeks.Instance);
    }
}
