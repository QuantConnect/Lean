import tempfile
import unittest
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from converter import ConversionManifest, WindSymbol, scale_price


class ConverterTests(unittest.TestCase):
    def test_maps_wind_suffix_to_lean_market(self):
        self.assertEqual(WindSymbol.parse("600000.SH").lean_market, "sh")
        self.assertEqual(WindSymbol.parse("000001.SZ").lean_market, "sz")
        self.assertEqual(WindSymbol.parse("IF2506.CFE").lean_market, "cffex")
        self.assertEqual(WindSymbol.parse("RB2501.SHF").lean_market, "shf")

    def test_builds_lean_equity_zip_name(self):
        symbol = WindSymbol.parse("600000.SH")
        self.assertEqual(symbol.lean_ticker, "600000")
        self.assertEqual(symbol.lean_file_stem, "600000")

    def test_scales_prices_like_lean_tradebar_reader(self):
        self.assertEqual(scale_price(12.34), 123400)
        self.assertEqual(scale_price("12.3456"), 123456)

    def test_manifest_persists_completed_items(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "manifest.json"
            manifest = ConversionManifest(path)
            manifest.mark_completed("equity:600000.SH:daily")

            loaded = ConversionManifest(path)
            self.assertTrue(loaded.is_completed("equity:600000.SH:daily"))


if __name__ == "__main__":
    unittest.main()
