from io import BytesIO
from zipfile import ZipFile
from os.path import abspath

import pandas as pd
from dateutil.parser import parse


def parse_zipped_data_file(zip_file_path, csv_file=None):
    abs_path = abspath(zip_file_path)
    if 'minute' in abs_path or 'second' in abs_path:
        return parse_zipped_high_resolution_file(abs_path, csv_file)
    elif 'daily' in abs_path or 'hour' in abs_path:
        return parse_zipped_low_resolution_file(abs_path)
    else:
        raise NotImplementedError("Data type or resolution not implemented yet.")
    pass


def parse_zipped_high_resolution_file(zip_file_path, csv_file=None):
    """
    Retrieves one zipped file of minute or second resolution data from Lean into a DataFrame.
    The file is a zipped CSV.
    :param csv_file:
    :param zip_file_path: the path to the zipped CSV file
    :return: a Dataframe with a day of second quote data.
    """
    df, data_date = read_zipped_csv_data(zip_file_path, False, csv_file)
    idx = data_date + pd.to_timedelta(df.index.to_series(), 'ms')
    return df.set_index(idx)


def read_zipped_csv_data(zip_file_path, is_low_resolution, csv_file=None):
    with open(zip_file_path, 'rb') as file_handler, ZipFile(file_handler) as zip_file:
        csv_filename = csv_file if csv_file is not None else zip_file.namelist()[0]
        try:
            data = BytesIO(zip_file.read(csv_filename))
        except KeyError:
            raise ValueError(f'There is no file named {csv_file} in the zipped {zip_file_path} data.')
        df = pd.read_csv(data, header=None, index_col=0, parse_dates=is_low_resolution,
                         names=get_column_names(zip_file_path))
        data_date = parse(csv_filename[:8]) if not is_low_resolution else None
    if 'equity' in abspath(zip_file_path):
        df.ix[:, ['Open', 'High', 'Low', 'Close']] = df.ix[:, ['Open', 'High', 'Low', 'Close']].div(10000).round(2)

    return df, data_date


def parse_zipped_low_resolution_file(zip_file_path):
    """
    Retrieves one zipped file of daily or minute resolution data from Lean into a DataFrame.
    The file is a zipped CSV.
    :param zip_file_path: the path to the zipped CSV file
    :return: a Dataframe with a day of second quote data.
    """
    df, _ = read_zipped_csv_data(zip_file_path, True)
    return df


def get_column_names(zip_file_path):
    if 'quote' in zip_file_path or 'cfd' in zip_file_path:
        column_names = ['BidOpen', 'BidHigh', 'BidLow', 'BidClose', 'BidSize',
                        'AskOpen', 'AskHigh', 'AskLow', 'AskClose', 'AskSize']
    else:
        column_names = ['Open', 'High', 'Low', 'Close', 'Volume']
    return column_names
