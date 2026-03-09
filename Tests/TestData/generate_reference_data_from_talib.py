import pandas as pd
import talib.abstract


def format_number(x):
    if pd.isnull(x):
        return ""
    else:
        return f"{x:.6f}"


def write_dataframe(df, fname_stem, format_dict):
    # format index
    datetime_output_fmt = "%m/%d/%Y %I:%M:%S %p"
    df.index = df.index.strftime(datetime_output_fmt)
    # format columns
    for col in df.columns:
        if format_dict is not None and col in format_dict:
            format_function = format_dict[col]
            df[col] = df[col].map(format_function)
    df.to_csv(f"{fname_stem}.csv", sep=",")


def generate_reference_data_for_single_output_indicator(
    df, indicator_type, parameters, fname_stem, output_name, format_dict=None
):
    print("* Processing %s" % indicator_type.info)
    series_output = indicator_type(df, **parameters)
    series_output.name = output_name
    df_output = series_output.to_frame()
    df.columns = df.columns.str.capitalize()
    df_all = pd.concat([df, df_output], axis=1)
    write_dataframe(df_all, fname_stem, format_dict)


def generate_reference_data_for_multi_output_indicator(
    df, indicator_type, parameters, fname_stem, output_names=None, format_dict=None
):
    print("* Processing %s" % indicator_type.info)
    df_output = indicator_type(df, **parameters)
    if output_names is not None:
        df_output.columns = output_names
    df.columns = df.columns.str.capitalize()
    df_all = pd.concat([df, df_output], axis=1)
    write_dataframe(df_all, fname_stem, format_dict)


def main():
    fname = "spy_daily_klines_2013-01-16_2015-12-01_no_volume.csv"
    datetime_input_fmt = "%m/%d/%Y %I:%M:%S %p"
    df = pd.read_csv(fname)
    df["Date"] = pd.to_datetime(df["Date"], format=datetime_input_fmt)
    df = df.set_index("Date")
    df.columns = df.columns.str.lower()

    generate_reference_data_for_single_output_indicator(
        df.copy(),
        talib.abstract.ATR,
        {"timeperiod": 14},
        "spy_atr",
        "Average True Range 14",
        {"Average True Range 14": format_number},
    )

    generate_reference_data_for_multi_output_indicator(
        df.copy(),
        talib.abstract.BBANDS,
        {"timeperiod": 20, "nbdevup": 2.0, "nbdevdn": 2.0},
        "spy_bollinger_bands",
        [
            "Bollinger Bands® 20 2 Top",
            "Moving Average 20",
            "Bollinger Bands® 20 2 Bottom",
        ],
        {
            "Bollinger Bands® 20 2 Top": format_number,
            "Moving Average 20": format_number,
            "Bollinger Bands® 20 2 Bottom": format_number,
        },
    )


if __name__ == "__main__":
    main()
