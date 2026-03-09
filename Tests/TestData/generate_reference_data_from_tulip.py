import numpy as np
import pandas as pd
import tulipy as ti


def write_dataframe(df, output_names, fname_stem):
    # convert columns to np.int64
    for col in ["open", "high", "low", "close", "volume"]:
        df[col] = df[col].astype(np.int64)
    df = df.reset_index()
    df["date"] = df["datetime"].dt.strftime("%m/%d/%Y")
    df["time"] = df["datetime"].dt.strftime("%H:%M")
    df = df[["date", "time", "open", "high", "low", "close", "volume", *output_names]]
    df.to_csv(f"{fname_stem}.csv")


def generate_reference_data_for_siso_indicator(
    df, indicator_type, parameters, output_name, fname_stem
):
    # get close column as numpy array
    input_array = df["close"].values
    # get output array
    output_array = indicator_type(input_array, **parameters)
    # when the size of the output array is less than the input array, insert zeros at the beginning
    missings = len(input_array) - len(output_array)
    output_array = np.insert(output_array, 0, np.zeros(missings))
    df[output_name] = output_array
    write_dataframe(df, [output_name], fname_stem)


def main():
    fname = "../../Data/equity/usa/daily/spy.zip"
    df = pd.read_csv(
        fname,
        names=["datetime", "open", "high", "low", "close", "volume"],
    )
    # convert datetime string to datetime64[ns]
    df["datetime"] = pd.to_datetime(df["datetime"], format="%Y%m%d %H:%M")
    # convert columns to np.float64
    for col in ["open", "high", "low", "close", "volume"]:
        df[col] = df[col].astype(np.float64)
    df = df.set_index("datetime")
    df = df[["open", "high", "low", "close", "volume"]]

    generate_reference_data_for_siso_indicator(
        df, ti.tsf, {"period": 5}, "tsf", "spy_tsf"
    )


if __name__ == "__main__":
    main()
