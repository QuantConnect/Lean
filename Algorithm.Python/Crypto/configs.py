
configs = {
        '__GLOBALS__': {
            '__TARGET_CRYPTOS__': ['ETHUSD', 'BTCUSD'],
            '__INDICATORS__': ['__MACD__', '__BOLLINGER__', '__MOMENTUM__', '__ICHIMOKU__'],
            '__TIME_RESOLUTION__': ['Resolution.Minute'],
            '__WARMUP_LOOKBACK__': [30],
            '__RESUBMIT_ORDER_THRESHOLD__': [0.01],
            '__BAR_SIZE__': [5]
            },


        '__MACD__': {
            'MACD_FAST_PERIOD': [12, 24, 36, 48],
            'MACD_SLOW_PERIOD': [26, 13, 123],
            'MACD_SIGNAL_PERIOD': [9, 12, 26],
            'MACD_MOVING_AVERAGE_TYPE': ['MovingAverageType.Exponential'],
            'MACD_TOLERANCE': [0.0025, 0.05, 0.1]
            },

        '__BOLLINGER__': {
            'MOVING_AVERAGE_TYPE': ['MovingAverageType.Exponential'],
            'BOLLINGER_PERIOD': [20],
            'BOLLINGER_K': [2]
            },

        '__MOMENTUM__': {
            'MOMENTUM_PERIOD': [5],
            'MOMENTUM_BUY_THRESHOLD': [2],
            'MOMENTUM_SELL_THRESHOLD': [0]
            },

        '__ICHIMOKU__': {
            'TENKAN_PERIOD': [9],
            'KIJUN_PERIOD': [26],
            'SENKOU_A_PERIOD': [26],
            'SENKOU_B_PERIOD': [52],
            'SENKOU_A_DELAYED_PERIOD': [26],
            'SENKOU_B_DELAYED_PERIOD': [26]
            }
}

