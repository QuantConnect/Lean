"""
Usage:
    QuantConnect.Visualizer.py DATAFILE PLOTFILE [--size height,width] [-c columns]
    QuantConnect.Visualizer.py DATAFILE CSVFILE PLOTFILE [--size height,width] [-c columns]
    QuantConnect.Visualizer.py -t ticker -f startdate -u enddate -o PLOTFILE [-k securitytype] [-r resolution] [-d datatype] [-m market] [--size height,width] [-c columns]

Arguments:
    DATAFILE   path or filename to the zipped data file to plot.
    CSVFILE   specific CSV file to plot from an option or future file.
    PLOTFILE  path or filename for the output plot.

Options:
    -h --help                         show this.
    -c, --columns <arg>               Columns to plot separated by commas [default: Close]
    -o, --outplot PLOTFILE            output plot file name.
    -s, --size height,width           plot size in pixels [default: 800,400].
    -t, --ticker ticker               Ticker of the requested security.
    -f, --fromdate startdate          ISO 8601 formatted time, first timestamp included in the plot.
    -u, --todate enddate              ISO 8601 formatted time, last timestamp included in the plot.
    -k, --securitytype securitytype   Security type {'equity', 'forex', 'cfd', 'crypto', 'future', 'option'} [default: equity].
    -r, --resolution resolution       Data resolution {'tick', 'second', 'minute', 'hour', 'daily'} [default: minute].
    -d, --datatype datatype           Data type {'trade', 'quote'} [default: trade], used for crypto, futures and options.
    -m, --market market               Market, used when multiple markets are available, e.g. Forex pairs available in Oanda and FXCM.
"""

import sys

import matplotlib.pyplot as plt
from docopt import docopt

from lean_data_parser import parse_zipped_data_file


def plot_single_file(zip_file_path, plot_filename, csv_filename, size_px):

    df = parse_zipped_data_file(zip_file_path, csv_filename)
    cols_to_plot = [col for col in df.columns if 'Close' in col]
    plot = df.loc[:, cols_to_plot].plot(grid=True)
    fig = plot.get_figure()
    fig.set_size_inches(size_px[0] / fig.dpi, size_px[1] / fig.dpi)
    fig.savefig(f'{plot_filename}.png', transparent=True, dpi=fig.dpi)
    plt.close()


if __name__ == "__main__":
    arguments = docopt(__doc__)
    if arguments['DATAFILE'] is None or arguments['PLOTFILE'] is None:
        raise NotImplementedError("WIP - First iterations, all options will be implemented soon.")
    size_px = [int(p) for p in arguments['--size'].split(',')]
    # Get rid of the extension, for now all plots will be png.
    plot_filename = ''.join(arguments['PLOTFILE'].split('.')[:-1])
    plot_single_file(arguments['DATAFILE'], plot_filename, arguments['CSVFILE'], size_px)
    sys.exit(0)
