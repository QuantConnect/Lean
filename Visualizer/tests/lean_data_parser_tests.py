import unittest
import uuid

import os

from datetime import datetime
from lean_data_parser import parse_zipped_low_resolution_file, parse_zipped_high_resolution_file, parse_zipped_data_file

import pytest

# These tests cases doesn't works on VS but they run flawlessly in other Python IDE like Pycharm and Spyder.
resolutions_and_data_types_test_cases = (
    ('security_type', 'data_type', 'resolution', 'path_to_file', 'expected_shape', 'expected_sum', 'csv_file'),
    [('cfd', 'QuoteBars', 'daily', '../Data/cfd/oanda/daily/jp225usd.zip', (3987, 10), 421454410.78, None),
     ('forex', 'QuoteBars', 'minute', '../Data/forex/fxcm/minute/eurusd/20160623_quote.zip', (1440, 10), 13041.43, None),
     ('future', 'QuoteBars', 'minute', '../Data/future/usa/minute/es/20131008_quote.zip', (1411, 10), 19297264,
      '20131008_es_minute_quote_201312.csv'),
     ('future', 'TradeBars', 'minute', '../Data/future/usa/minute/gc/20131008_trade.zip', (1377, 5), 7362103.50,
      '20131008_gc_minute_trade_201312.csv'),
     ('option', 'QuoteBars', 'minute', '../Data/option/usa/minute/aapl/20140609_quote_american.zip', (391, 10), 65784173,
      '20140609_aapl_minute_quote_american_call_914300_20140613.csv'),
     ('option', 'TradeBars', 'minute', '../Data/option/usa/minute/goog/20151224_trade_american.zip', (1, 5), 2001,
      '20151224_goog_minute_trade_american_put_6600000_20151231.csv')
     ])

@pytest.mark.parametrize(*resolutions_and_data_types_test_cases)
def test_parse_spot_price_file(security_type, data_type, resolution, path_to_file, expected_shape, expected_sum,
                               csv_file):
    # Act
    df_data = parse_zipped_data_file(path_to_file, csv_file)
    # Assert
    assert (df_data.shape == expected_shape,
            f'{resolution} {security_type} {data_type} are not correctly parsed, shape mismatch.')
    assert (abs(df_data.sum().sum() - expected_sum) < 0.01,
            f'{resolution} {security_type} {data_type} are not correctly parsed, sum mismatch.')


class LeanDataParserTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.test_data_folder = '../Data'
        cls.random_folder = str(uuid.uuid4())[:8]

    def test_equity_low_resolution_data(self):
        # Arrange
        os.chdir('../Data/equity/usa/daily/')
        filename = 'spy.zip'
        # Act
        df_equity_trade_file = parse_zipped_low_resolution_file(filename)
        # Assert
        os.chdir('../../../')
        self.assertEqual(df_equity_trade_file.shape, (4407, 5),
                         'Low Resolution equity TradeBars are not correctly parsed, shape mismatch.')
        self.assertAlmostEqual(df_equity_trade_file.sum().sum(), 335244905607.45, places=2,
                               msg='Low Resolution equity TradeBars are not correctly parsed.')

    def test_paring_futures_and_option_raises_exception_if_filename_is_wrong(self):
        # Arrange
        path_to_file = '../Data/future/usa/minute/gc/20131007_trade.zip'
        csv_file = 'some_bad_name'
        # Act and Assert
        with pytest.raises(ValueError):
            parse_zipped_data_file(path_to_file, csv_file)

    def test_daily_trade_and_quote_data_is_correctly_parsed(self):
        # Arrange
        daily_quote_file = os.path.join(self.test_data_folder, 'crypto/gdax/daily', 'btcusd_quote.zip')
        daily_trade_file = os.path.join(self.test_data_folder, 'crypto/gdax/daily', 'btcusd_trade.zip')
        # Act
        df_daily_quotes = parse_zipped_low_resolution_file(daily_quote_file)
        df_daily_trades = parse_zipped_low_resolution_file(daily_trade_file)
        # Assert
        self.assertEqual(df_daily_trades.shape, (1025, 5),
                         'Low Resolution TradeBars are not correctly parsed, shape mismatch.')
        self.assertAlmostEqual(df_daily_trades.sum().sum(), 12613345.2555, places=4,
                               msg='Low Resolution TradeBars are not correctly parsed.')
        self.assertEqual(df_daily_quotes.shape, (788, 10),
                         'Low Resolution QuoteBars are not correctly parsed, shape mismatch.')
        self.assertAlmostEqual(df_daily_quotes.sum().sum(), 7617979.4937, places=4,
                               msg='Low Resolution QuoteBars are not correctly parsed.')

    def test_minute_and_second_data_is_correctly_parsed(self):
        # Arrange
        minute_quote_file = os.path.join(self.test_data_folder, 'crypto/gdax/minute', 'btcusd', '20161007_quote.zip')
        second_trade_file = os.path.join(self.test_data_folder, 'crypto/gdax/second', 'btcusd', '20161009_trade.zip')
        # Act
        df_minute_quotes = parse_zipped_high_resolution_file(minute_quote_file, None)
        df_second_trades = parse_zipped_high_resolution_file(second_trade_file, None)
        # Assert
        self.assertEqual(df_second_trades.shape, (4004, 5),
                         'High Resolution TradeBars are not correctly parsed, shape mismatch.')
        self.assertAlmostEqual(df_second_trades.sum().sum(), 9903984.1321, places=4,
                               msg='High Resolution TradeBars are not correctly parsed.')
        self.assertTrue(df_second_trades.first_valid_index().date() == datetime(2016, 10, 9).date())
        self.assertTrue(df_second_trades.last_valid_index().date() == datetime(2016, 10, 9).date())
        self.assertEqual(df_minute_quotes.shape, (1438, 10),
                         'High Resolution QuoteBars are not correctly parsed, shape mismatch.')
        self.assertAlmostEqual(df_minute_quotes.sum().sum(), 7092443.3885, places=4,
                               msg='High Resolution QuoteBars are not correctly parsed.')
        self.assertTrue(df_minute_quotes.first_valid_index().date() == datetime(2016, 10, 7).date())
        self.assertTrue(df_minute_quotes.last_valid_index().date() == datetime(2016, 10, 7).date())
