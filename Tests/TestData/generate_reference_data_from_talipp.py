import numpy as np
import pandas as pd
from pathlib import Path
import talipp
from talipp.indicators import ZLEMA, McGinleyDynamic  # SISO
from talipp.ohlcv import OHLCVFactory
from talipp.indicators import VWMA, ChaikinOsc, CHOP, ForceIndex, IBS, KVO, SOBV, RogersSatchell  # MISO
from talipp.indicators import StochRSI, KST  # SIMO
from talipp.indicators import ChandeKrollStop, SFX, TTM, VTX, ZigZag  # MIMO


def main():
    fname = "spy_ppo.txt"
    datetime_fmt = "%m/%d/%Y %I:%M:%S %p"

    df = pd.read_csv(fname)
    df["Date"] = pd.to_datetime(df["Date"], format=datetime_fmt)
    df = df.set_index("Date")

    def generate_reference_data_for_siso_indicator(
        indicator_type, parameters, output_name, fname_stem
    ):
        df_in = df["Close"]
        indicator = indicator_type(**parameters, input_values=df["Close"].values)
        output_values = {output_name: list(indicator)}
        df_out = pd.concat([df_in, pd.DataFrame(output_values, index=df.index)], axis=1)
        df_out.index = df_out.index.strftime(datetime_fmt)
        df_out.to_csv(Path("out") / f"{fname_stem}.csv")

    def generate_reference_data_for_miso_indicator(
        indicator_type, parameters, output_name, fname_stem
    ):
        df_in = df[["Open", "High", "Low", "Close", "Volume"]]
        input_values = OHLCVFactory.from_dict(
            {
                "open": df.Open,
                "high": df.High,
                "low": df.Low,
                "close": df.Close,
                "volume": df.Volume,
            }
        )
        indicator = indicator_type(**parameters, input_values=input_values)
        output_values = {output_name: list(indicator)}
        df_out = pd.concat([df_in, pd.DataFrame(output_values, index=df.index)], axis=1)
        df_out.index = df_out.index.strftime(datetime_fmt)
        df_out.to_csv(Path("out") / f"{fname_stem}.csv")

    def generate_reference_data_for_simo_indicator(
        indicator_type, parameters, output_names, fname_stem
    ):
        df_in = df["Close"]
        indicator = indicator_type(**parameters, input_values=df["Close"].values)
        output_values = {output_name: [] for output_name in output_names}
        raw_output_values = list(indicator)
        for raw_output_value in raw_output_values:
            if raw_output_value is None:
                for output_name in output_names:
                    output_value_for_name = np.nan
                    output_values[output_name].append(output_value_for_name)
            else:
                for output_name in output_names:
                    output_value_for_name = getattr(raw_output_value, output_name)
                    output_values[output_name].append(output_value_for_name)
        df_out = pd.concat([df_in, pd.DataFrame(output_values, index=df.index)], axis=1)
        df_out.index = df_out.index.strftime(datetime_fmt)
        df_out.to_csv(Path("out") / f"{fname_stem}.csv")

    def generate_reference_data_for_mimo_indicator(
        indicator_type, parameters, output_names, fname_stem
    ):
        df_in = df[["Open", "High", "Low", "Close", "Volume"]]
        input_values = OHLCVFactory.from_dict(
            {
                "open": df.Open,
                "high": df.High,
                "low": df.Low,
                "close": df.Close,
                "volume": df.Volume,
            }
        )
        indicator = indicator_type(**parameters, input_values=input_values)
        output_values = {output_name: [] for output_name in output_names}
        raw_output_values = list(indicator)
        for raw_output_value in raw_output_values:
            if raw_output_value is None:
                for output_name in output_names:
                    output_value_for_name = np.nan
                    output_values[output_name].append(output_value_for_name)
            else:
                for output_name in output_names:
                    output_value_for_name = getattr(raw_output_value, output_name)
                    output_values[output_name].append(output_value_for_name)
        df_out = pd.concat([df_in, pd.DataFrame(output_values, index=df.index)], axis=1)
        df_out.index = df_out.index.strftime(datetime_fmt)
        df_out.to_csv(Path("out") / f"{fname_stem}.csv")

    def generate_reference_data_for_zigzag_indicator(
        indicator_type, parameters, fname_stem
    ):
        input_values = OHLCVFactory.from_dict(
            {
                "open": df.Open,
                "high": df.High,
                "low": df.Low,
                "close": df.Close,
                "volume": df.Volume,
            }
        )
        output_names = ["ohlcv", "type"]
        indicator = indicator_type(**parameters, input_values=input_values)
        output_values = {output_name: [] for output_name in output_names}
        raw_output_values = list(indicator)
        for raw_output_value in raw_output_values:
            if raw_output_value is None:
                for output_name in output_names:
                    output_value_for_name = np.nan
                    output_values[output_name].append(output_value_for_name)
            else:
                for output_name in output_names:
                    output_value_for_name = getattr(raw_output_value, output_name)
                    output_values[output_name].append(output_value_for_name)
        df_out = pd.DataFrame(output_values)
        df_out["open"] = df_out["ohlcv"].map(lambda cdl: cdl.open)
        df_out["high"] = df_out["ohlcv"].map(lambda cdl: cdl.high)
        df_out["low"] = df_out["ohlcv"].map(lambda cdl: cdl.low)
        df_out["close"] = df_out["ohlcv"].map(lambda cdl: cdl.close)
        df_out.index.name = "Id"
        del df_out["ohlcv"]
        df_out.to_csv(Path("out") / f"{fname_stem}.csv")

    # SISO
    generate_reference_data_for_siso_indicator(
        ZLEMA, {"period": 5}, "ZLEMA5", "spy_with_zlema"
    )
    generate_reference_data_for_siso_indicator(
        McGinleyDynamic, {"period": 14}, "McGinleyDynamic14", "spy_with_McGinleyDynamic"
    )

    # MISO
    generate_reference_data_for_miso_indicator(
        VWMA, {"period": 20}, "VWMA20", "spy_with_vwma"
    )
    generate_reference_data_for_miso_indicator(
        ChaikinOsc,
        {"fast_period": 5, "slow_period": 7},
        "ChaikinOsc5_7",
        "spy_with_ChaikinOsc",
    )
    generate_reference_data_for_miso_indicator(
        CHOP, {"period": 14}, "CHOP14", "spy_with_chop"
    )
    generate_reference_data_for_miso_indicator(
        ForceIndex, {"period": 20}, "ForceIndex20", "spy_with_ForceIndex"
    )
    generate_reference_data_for_miso_indicator(IBS, {}, "IBS", "spy_with_ibs")
    generate_reference_data_for_miso_indicator(
        KVO, {"fast_period": 5, "slow_period": 10}, "KVO5_10", "spy_with_kvo"
    )
    generate_reference_data_for_miso_indicator(
        SOBV, {"period": 20}, "SOBV20", "spy_with_sobv"
    )
    generate_reference_data_for_miso_indicator(
        RogersSatchell, {"period": 9}, "RSVolat9", "spy_with_rsvolat"
    )

    # SIMO
    generate_reference_data_for_simo_indicator(
        StochRSI,
        {
            "rsi_period": 14,
            "stoch_period": 14,
            "k_smoothing_period": 3,
            "d_smoothing_period": 3,
        },
        ["k", "d"],
        "spy_with_StochRSI",
    )
    generate_reference_data_for_simo_indicator(
        KST,
        {
            "roc1_period": 5,
            "roc1_ma_period": 5,
            "roc2_period": 10,
            "roc2_ma_period": 5,
            "roc3_period": 15,
            "roc3_ma_period": 5,
            "roc4_period": 25,
            "roc4_ma_period": 10,
            "signal_period": 9,
        },
        ["kst", "signal"],
        "spy_with_kst",
    )

    # MIMO
    generate_reference_data_for_mimo_indicator(
        ChandeKrollStop,
        {"atr_period": 5, "atr_mult": 2.0, "period": 3},
        ["short_stop", "long_stop"],
        "spy_with_ChandeKrollStop",
    )
    generate_reference_data_for_mimo_indicator(
        SFX,
        {"atr_period": 12, "std_dev_period": 12, "std_dev_smoothing_period": 3},
        ["atr", "std_dev", "ma_std_dev"],
        "spy_with_sfx",
    )
    generate_reference_data_for_mimo_indicator(
        TTM,
        {"period": 20, "bb_std_dev_mult": 2.0, "kc_atr_mult": 2.0},
        ["squeeze", "histogram"],
        "spy_with_ttm",
    )
    generate_reference_data_for_mimo_indicator(
        VTX, {"period": 14}, ["plus_vtx", "minus_vtx"], "spy_with_vtx"
    )

    # Other MIMO (ZigZag)
    generate_reference_data_for_zigzag_indicator(
        ZigZag, {"sensitivity": 0.05, "min_trend_length": 3}, "spy_with_ZigZag"
    )


if __name__ == "__main__":
    main()
