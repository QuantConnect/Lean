"""
Usage:
    QuantConnect.Visualizer.py DATAFILE [--output file_path] [--size height,width] [--reset]

Arguments:
    DATAFILE   Absolute or relative path to a zipped data file to plot.
               Optionally the zip entry file can be declared by using '#' as separator.

Options:
    -h --help                 show this.
    -o --output file_path     path or filename for the output plot. If not declared, it will save with an
                              auto-generated name at the default folder defined in the config.json file.
    -s, --size height,width   plot size in pixels [default: 800,400].
    -r, --reset               Resets it forces the writing of the config.json file in the assembly folder.

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

import matplotlib as mpl

mpl.use('Agg')

from docopt import docopt
from matplotlib.dates import DateFormatter


class VisualizerWrapper:
    """
    Python wrapper for the Lean ToolBox.Visualizer.

    This class is instantiated with the dictionary docopt generates from the CLS arguments.

    It contains the methods for set up and load the C# assemblies into Python, in order to do that we need to declare
    two folders in the config.json file, the QuantConnect.ToolBox assembly folder and the Lean's Data folder.

    A copy of the config.json file will be written in the QuantConnect.ToolBox assembly folder for use of the Lean
    Composer. The CRITICAL assumption is that if there is config.json in the QuantConnect.ToolBox assembly folder, it
    has the correct paths, if not, there is a --reset option for force overwriting the file.

    Is highly recommended to use absolute paths in the config.json.
    """
    def __init__(self, arguments):

        self.palette = ['#f5ae29', '#657584', '#b1b9c3', '#222222']
        self.data_file_argument = arguments['DATAFILE']
        self.reset = arguments['--reset']

        with open('config.json', 'r') as json_data:
            self.config = json.load(json_data)

        # Loads the Toolbox to access Visualizer
        self.setup_and_load_toolbox()

        # Get plot image name, if not defined, generate one.
        self.plot_filename = arguments['--output']
        if self.plot_filename is None:
            self.plot_filename = self.generate_plot_filename()

        self.size_px = [int(p) for p in arguments['--size'].split(',')]

    def setup_and_load_toolbox(self):
        """
        Checks if the path given in the config.json file contains the needed assemblies, if so it returns the
        absolute path to that folder.
        Also, saves a copy of the config.json with the data needed by the Composer to run.
        :return: void.
        :raise: NotImplementedError: if the needed assemblies dll are not available.
        """
        # Check Lean assemblies are present in the composer-dll-directory key provided.
        assemblies_folder_info = (Path(self.config['composer-dll-directory'])
                                  if 'composer-dll-directory' in self.config else Path('../'))
        if not assemblies_folder_info.joinpath('QuantConnect.ToolBox.exe').exists():
            raise KeyError(
                "Please set up the 'composer-dll-directory' key in config.json with the path to Lean assemblies." +
                f"Absolute path to 'composer-dll-directory' provided: {assemblies_folder_info.resolve().absolute()}\n" +
                'If the issue continues even then the folder is correctly declared, please use the --reset option.')
        # Check Data folder is correctly set up
        data_folder_info = (
            Path(self.config['data-folder']) if 'data-folder' in self.config else Path('../../../Data/'))
        if not data_folder_info.joinpath('market-hours', 'market-hours-database.json').exists():
            raise KeyError("Please set up a valid 'data-folder' key in config.json.\n" +
                           f"Absolute path to 'data-folder' provided: {data_folder_info.resolve().absolute()}\n" +
                           'If the issue continues even then the folder is correctly declared, please use the --reset option.')
        assembly_folder_path = str(assemblies_folder_info.resolve().absolute())
        # Check if config.json exist in the composer-dll-directory, if not creates it.
        config_file = assemblies_folder_info.joinpath('config.json')
        if not config_file.exists() or self.reset:
            cfg_content = {'composer-dll-directory': assembly_folder_path,
                           'data-folder': str(data_folder_info.resolve().absolute())}
            with open(str(config_file.resolve().absolute()), 'w') as cfg:
                json.dump(cfg_content, cfg)

        # Load the visualizer
        os.chdir(assembly_folder_path)
        sys.path.append(assembly_folder_path)
        AddReference("QuantConnect.ToolBox")
        return

    def generate_plot_filename(self):
        """
        Generates a random name for the output plot image file in the default folder defined in the config.json file.
        :return: an absolute path to the output plot image file.
        """
        default_output_folder = (Path(self.config['default-output-folder'])
                                 if 'default-output-folder' in self.config else Path('./output_plots'))
        if not default_output_folder.exists():
            os.makedirs(str(default_output_folder.resolve().absolute()))
        file_name = f'{str(uuid.uuid4())[:8]}.png'
        file_path = default_output_folder.joinpath(file_name)
        return str(file_path.resolve().absolute())

    def get_data(self):
        """
        Makes use of the Lean's Toolbox Visualizer to parse the data as pandas.DataFrame from a given zip file and
        an optional internal filename for option and futures.
        :return: a pandas.DataFrame with the data from the file.
        """
        from QuantConnect.ToolBox.Visualizer import Visualizer
        vsz = Visualizer(self.data_file_argument)
        df = vsz.ParseDataFrame()
        if df.empty:
            raise Exception("Data frame is empty")
        return df

    def filter_data(self, df):
        """
        Applies the filters defined in the CLI arguments to the parsed data.
        Not fully implemented yet, it only select the close columns right now.
        :param df: pandas.DataFrame with all the data form the selected file.
        :return: a filtered pandas.DataFrame.

        TODO: implement column and time filters.
        """
        symbol = df.index.levels[0][0]
        df = df.loc[symbol]
        if 'tick' in self.data_file_argument:
            cols_to_plot = [col for col in df.columns if 'price' in col]
        else:
            cols_to_plot = [col for col in df.columns if 'close' in col]
        if 'openinterest' in self.data_file_argument:
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
        plot = data.plot(grid=True, color=self.palette)
        fig = plot.get_figure()
        is_low_resolution_data = 'hour' in self.data_file_argument or 'daily' in self.data_file_argument
        if not is_low_resolution_data:
            plot.xaxis.set_major_formatter(DateFormatter("%H:%M"))
        fig.set_size_inches(self.size_px[0] / fig.dpi, self.size_px[1] / fig.dpi)
        fig.savefig(self.plot_filename, transparent=True, dpi=fig.dpi)
        return


if __name__ == "__main__":
    arguments = docopt(__doc__)
    visualizer = VisualizerWrapper(arguments)
    # Gets the pandas.DataFrame from the data file
    df = visualizer.get_data()
    # Selects the columns you want to plot
    df = visualizer.filter_data(df)
    # Save the image
    visualizer.plot_and_save_image(df)
    print(visualizer.plot_filename)
    sys.exit(0)
