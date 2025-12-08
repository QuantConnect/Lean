"""
Options Volume Scanner
======================
Run in QC Research environment to get top stocks by options volume.
This doesn't run a backtest - just queries data.
"""

from AlgorithmImports import *

class OptionsVolumeScanner(QCAlgorithm):
    """
    Simple scanner to log options volume for universe expansion.
    Run as a short backtest (1 day) to get current options volume data.
    """

    def Initialize(self):
        # Just need 1 day of data
        self.SetStartDate(2024, 12, 1)
        self.SetEndDate(2024, 12, 1)
        self.SetCash(100000)

        # Candidate universe - S&P 500 top components + popular options names
        self.candidates = [
            # Current universe (for comparison)
            "NVDA", "TSLA", "META", "AMD", "AAPL", "SMCI", "MSFT", "AMZN",
            "NFLX", "COIN", "GOOGL", "AVGO", "PLTR", "LULU", "ADBE",
            # Additional high-volume candidates
            "SPY", "QQQ", "IWM", "XLF", "BAC", "JPM", "GS", "V", "MA",
            "DIS", "PYPL", "SQ", "SHOP", "ROKU", "SNAP", "UBER", "LYFT",
            "F", "GM", "RIVN", "LCID", "NIO", "BABA", "JD", "PDD",
            "MRNA", "PFE", "ABBV", "JNJ", "UNH", "CVS",
            "XOM", "CVX", "OXY", "SLB",
            "INTC", "MU", "QCOM", "ARM", "MRVL", "AMAT", "KLAC",
            "CRM", "NOW", "SNOW", "DDOG", "NET", "ZS", "CRWD", "PANW",
            "COST", "WMT", "TGT", "HD", "LOW",
            "BA", "CAT", "DE", "RTX", "LMT", "GE",
            "T", "VZ", "TMUS",
            "KO", "PEP", "MCD", "SBUX",
            "GOLD", "NEM", "GLD", "SLV",
        ]

        self.volume_data = {}

        for symbol in self.candidates:
            try:
                equity = self.AddEquity(symbol, Resolution.Daily)
                option = self.AddOption(symbol, Resolution.Daily)
                option.SetFilter(lambda u: u.Strikes(-2, 2).Expiration(30, 60))
            except:
                pass

    def OnData(self, data):
        """Collect options volume data."""
        for symbol in self.candidates:
            try:
                option_symbol = self.Securities.Keys
                for sec in self.Securities.Values:
                    if sec.Type == SecurityType.Option:
                        underlying = sec.Symbol.Underlying.Value
                        if underlying not in self.volume_data:
                            self.volume_data[underlying] = 0
                        self.volume_data[underlying] += sec.Volume
            except:
                pass

    def OnEndOfAlgorithm(self):
        """Output ranked results."""
        self.Log("=" * 60)
        self.Log("OPTIONS VOLUME RANKING")
        self.Log("=" * 60)

        # Sort by volume
        sorted_data = sorted(self.volume_data.items(), key=lambda x: x[1], reverse=True)

        for i, (symbol, volume) in enumerate(sorted_data[:50], 1):
            in_current = "CURRENT" if symbol in ["NVDA", "TSLA", "META", "AMD", "AAPL",
                                                   "SMCI", "MSFT", "AMZN", "NFLX", "COIN",
                                                   "GOOGL", "AVGO", "PLTR", "LULU", "ADBE"] else ""
            self.Log(f"{i:>3}. {symbol:<6} | Volume: {volume:>12,} | {in_current}")
