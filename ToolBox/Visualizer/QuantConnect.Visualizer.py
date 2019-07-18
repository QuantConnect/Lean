"""
Usage:
    QuantConnect.Visualizer.py DATAFILE [--assembly assembly_path] [--output output_folder] [--size height,width]

Arguments:
    DATAFILE   Absolute or relative path to a zipped data file to plot.
               Optionally the zip entry file can be declared by using '#' as separator.

Options:
    -h --help                    show this.
    -a --assembly assembly_path  path to the folder with the assemblies dll/exe [default: ../.].
    -o --output output_folder    path to the output folder, each new plot will be saved there with a random name [default: ./output_folder].
    -s, --size height,width      plot size in pixels [default: 800,400].

Examples:
    QuantConnect.Visualizer.py ../relative/path/to/file.zip
    QuantConnect.Visualizer.py absolute/path/to/file.zip#zipEntry.csv
    QuantConnect.Visualizer.py absolute/path/to/file.zip -o path/to/image.png -s 1024,800
"""

import json
import os
import sys
import uuid
from clr import AddReference
from pathlib import Path
from numpy import NaN

import matplotlib as mpl

mpl.use('Agg')

from docopt import docopt
from matplotlib.dates import DateFormatter


class Visualizer:
    """
    Python wrapper for the Lean ToolBox.Visualizer.

    This class is instantiated with the dictionary docopt generates from the CLI arguments.

    It contains the methods for set up and load the C# assemblies into Python. The QuantConnect.ToolBox assembly folder
    can be declared in the module's CLI.
    """
    def __init__(self, arguments):
        self.arguments = arguments
        zipped_data_file = Path(self.arguments['DATAFILE'].split('#')[0])
        if not zipped_data_file.exists():
            raise FileNotFoundError(f'File {zipped_data_file.resolve().absolute()} does not exist')
        self.palette = ['#f5ae29', '#657584', '#b1b9c3', '#222222']
        # Loads the Toolbox to access Visualizer
        self.setup_and_load_toolbox()
        # Sets up the Composer
        from QuantConnect.Data.Auxiliary import LocalDiskMapFileProvider
        from QuantConnect.Util import Composer
        from QuantConnect.Interfaces import IMapFileProvider
        localDiskMapFileProvider = LocalDiskMapFileProvider()
        Composer.Instance.AddPart[IMapFileProvider](localDiskMapFileProvider)
        # Initizlize LeanDataReader and PandasConverter
        from QuantConnect.ToolBox import LeanDataReader
        from QuantConnect.Python import PandasConverter
        self.lean_data_reader = LeanDataReader(self.arguments['DATAFILE'])
        self.pandas_converter = PandasConverter()
        # Generate random name for the plot.
        self.plot_filename = self.generate_plot_filename()

    def setup_and_load_toolbox(self):
        """
        Checks if the path given in the CLI (or its defaults values) contains the needed assemblies.

        :return: void.
        :raise: NotImplementedError: if the needed assemblies dll are not available.
        """
        # Check Lean assemblies are present in the composer-dll-directory key provided.
        assemblies_folder_info = (Path(self.arguments['--assembly']))
        toolbox_assembly = assemblies_folder_info.joinpath('QuantConnect.ToolBox.exe')
        common_assembly = assemblies_folder_info.joinpath('QuantConnect.Common.dll')
        if not (toolbox_assembly.exists() and common_assembly.exists()):
            raise KeyError("Please set up the '--assembly' option with the path to Lean assemblies.\n" +
                           f"Absolute path provided: {assemblies_folder_info.resolve().absolute()}")

        AddReference(str(toolbox_assembly.resolve().absolute()))
        AddReference(str(common_assembly.resolve().absolute()))
        os.chdir(str(assemblies_folder_info.resolve().absolute()))
        return

    def generate_plot_filename(self):
        """
        Generates a random name for the output plot image file in the default folder defined in the CLI.

        :return: an absolute path to the output plot image file.
        """
        default_output_folder = (Path(self.arguments['--output']))
        if not default_output_folder.exists():
            os.makedirs(str(default_output_folder.resolve().absolute()))
        file_name = f'{str(uuid.uuid4())[:8]}.png'
        file_path = default_output_folder.joinpath(file_name)
        return str(file_path.resolve().absolute())

    def get_data(self):
        """
        Makes use of the Lean's Toolbox LeanDataReader plus the PandasConverter to parse the data as pandas.DataFrame
        from a given zip file and an optional internal filename for option and futures.

        :return: a pandas.DataFrame with the data from the file.
        """

        from QuantConnect.Data import BaseData
        df = self.pandas_converter.GetDataFrame[BaseData](self.lean_data_reader.Parse())
        if df.empty:
            raise Exception("Data frame is empty")
        symbol = df.index.levels[0][0]
        return df.loc[symbol]

    def filter_data(self, df):
        """
        Applies the filters defined in the CLI arguments to the parsed data.
        Not fully implemented yet, it only selects the close columns.

        :param df: pandas.DataFrame with all the data form the selected file.
        :return: a filtered pandas.DataFrame.

        TODO: implement column and time filters.
        """
        if 'tick' in self.arguments['DATAFILE']:
            cols_to_plot = [col for col in df.columns if 'price' in col]
        else:
            cols_to_plot = [col for col in df.columns if 'close' in col]
        if 'openinterest' in self.arguments['DATAFILE']:
            cols_to_plot = ['openinterest']
        cols_to_plot = cols_to_plot[:2] if len(cols_to_plot) == 3 else cols_to_plot
        df = df.loc[:, cols_to_plot]
        return df

    def plot_and_save_image(self, data):
        """
        Plots the data and saves the plot as a png image.

        :param data: a pandas.DataFrame with the data to plot.
        :return: void
        """
        is_future_tick = ('future' in self.arguments['DATAFILE'] and 'tick' in self.arguments['DATAFILE']
                          and 'quote' in self.arguments['DATAFILE'])
        if is_future_tick:
            data = data.replace(0, NaN)

        plot = data.plot(grid=True, color=self.palette)

        is_low_resolution_data = 'hour' in self.arguments['DATAFILE'] or 'daily' in self.arguments['DATAFILE']
        if not is_low_resolution_data:
            plot.xaxis.set_major_formatter(DateFormatter("%H:%M"))
            plot.set_xlabel(self.lean_data_reader.GetDataTimeZone().Id)

        is_forex = 'forex' in self.arguments['DATAFILE']
        is_open_interest = 'openinterest' in self.arguments['DATAFILE']
        if is_forex:
            plot.set_ylabel('exchange rate')
        elif is_open_interest:
            plot.set_ylabel('open contracts')
        else:
            plot.set_ylabel('price (USD)')

        fig = plot.get_figure()
        size_px = [int(p) for p in self.arguments['--size'].split(',')]
        fig.set_size_inches(size_px[0] / fig.dpi, size_px[1] / fig.dpi)
        fig.savefig(self.plot_filename, transparent=True, dpi=fig.dpi)
        return


if __name__ == "__main__":
    arguments = docopt(__doc__)
    visualizer = Visualizer(arguments)
    # Gets the pandas.DataFrame from the data file
    df = visualizer.get_data()
    # Selects the columns you want to plot
    df = visualizer.filter_data(df)
    # Save the image
    visualizer.plot_and_save_image(df)
    print(visualizer.plot_filename)
    sys.exit(0)
