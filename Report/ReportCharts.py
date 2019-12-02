# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import re
import matplotlib
import numpy as np
import pandas as pd
from base64 import b64encode
from datetime import date, datetime, timedelta
from pandas.plotting import register_matplotlib_converters
from clr import AddReference
AddReference("System")
from System import *

register_matplotlib_converters()



matplotlib.use('Agg')
font = {'family': 'DejaVu Sans'}
matplotlib.rc('font',**font)
matplotlib.rc('axes', edgecolor='#d5d5d5')
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
import matplotlib.colors as mcolors
from matplotlib.dates import DateFormatter
from matplotlib.ticker import MaxNLocator, NullFormatter, ScalarFormatter, FormatStrFormatter
la = matplotlib.font_manager.FontManager()
lu = matplotlib.font_manager.FontProperties(family = "Open Sans Condensed")

class ReportCharts:

    def fig_to_base64(self, filename = '', fig = None, dpi = 200):
        base64 = 'data:image/png;base64,'
        if fig is not None:
            fig.savefig(filename, dpi=dpi, bbox_inches='tight')
            with open(filename, "rb") as fp:
                base64 += b64encode(fp.read()).decode('utf-8').replace('\n', '')
            return base64

    def GetReturnsPerTrade(self, returns_per_trade = [], live_returns_per_trade = [],
                           name = "returns-per-trade.png", width = 7, height = 5,
                           live_color = "#ff9914", backtest_color = "#71c3fc"):

        if len(returns_per_trade) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        if len(live_returns_per_trade) > 0:
            width = 11.5
            height = 5
            plt.figure()
            fig, ax = plt.subplots(1, 2, tight_layout=True)
            ax[0].hist(returns_per_trade, bins=75, color=backtest_color)
            ax[1].hist(live_returns_per_trade, bins=25, color=live_color)
            for i in range(2):
                    if i == 0:
                        ax[i].set_ylabel('Backtest', fontweight='demibold')
                        ax[i].axvline(x=np.median(returns_per_trade), color="red", ls="dashed", label="median", linewidth=0.5)
                    else:
                        ax[i].set_ylabel('Live', fontweight='demibold')
                        ax[i].axvline(x=np.median(live_returns_per_trade), color="red", ls="dashed", label="median",
                                      linewidth=0.5)
                    ax[i].tick_params(labelsize=8)
                    ax[i].spines['right'].set_visible(False)
                    ax[i].spines['top'].set_visible(False)
        else:
            fig = plt.figure()
            plt.hist(returns_per_trade, bins=75, color=backtest_color)
            plt.xticks(fontsize=8)
            plt.yticks(fontsize=8)
            plt.gca().spines['right'].set_visible(False)
            plt.gca().spines['top'].set_visible(False)
            plt.gca().axvline(x=np.median(returns_per_trade), color="red", ls="dashed", label="median", linewidth=0.5)
            plt.ylabel('')

        # Set the x ticks as percentage to keep consistency
        plt.xticks(ticks=plt.xticks()[0], labels=["{:.1f}%".format(tick * 100) for tick in plt.xticks()[0]])

        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetCumulativeReturns(self, data = None, live_data = None, benchmark_symbol = 'SPY',
                                 name = "cumulative-return.png", width = 11.5, height = 2.5, live_color = "#ff9914",
                                 backtest_color = "#71c3fc", gray = "#b3bcc0"):
        '''
        data: [ [strategyTime], [strategyPoints], [benchTime], [benchResults] ]
        live_data: [ [strategyTime], [strategyPoints], [benchTime], [benchResults] ]
        '''

        # Initialize lists here instead of method signature to avoid
        # unintended behavior when calling this method twice
        if data is None:
            data = [[],[],[],[]]
        if live_data is None:
            live_data = [[],[],[],[]]

        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        plt.figure()
        ax = plt.gca()
        labels = ['Backtest', 'Benchmark']
        labels_removed = []

        rectangles = []
        colors = [backtest_color, gray]
        values = [[data[0], data[1]], [data[2], data[3]]]

        for i, array in enumerate(values):
            if any(array[0]):
                ax.plot(array[0], array[1], linewidth=0.5, color=colors[i])
            else:
                # We have nothing for this graph. Wipe any mention of it
                labels_removed.append(labels[i])

            rectangles.append(plt.Rectangle((0, 0), 1, 1, fc=colors[i]))

        # Only get the labels we didn't remove (i.e. labels that have a graph, guaranteed)
        labels = [label for label in labels if label not in labels_removed]

        # Return if we don't have any valid labels
        if not any(labels):
            return ""

        live_labels = []
        live_labels_removed = []

        if len(live_data[0]) > 0:
            colors = [live_color, '#ff2000']
            live_labels.append('Live')
            live_labels.append('Live Benchmark')
            values = [[live_data[0], live_data[1]], [live_data[2], live_data[3]]]

            for i, array in enumerate(values):
                if any(array[0]):
                    ax.plot(array[0], array[1], linewidth=0.5, color=colors[i])
                    rectangles.append(plt.Rectangle((0, 0), 1, 1, fc=colors[i]))
                else:
                    live_labels_removed.append(live_labels[i])

            # Only get the labels we didn't remove (i.e. labels that have a graph, guaranteed)
            live_labels = [live_label for live_label in live_labels if live_label not in live_labels_removed]
            labels += live_labels

        ax.legend(rectangles, labels, handlelength=0.8, handleheight=0.8,
                  frameon=False, fontsize=8, ncol=len(labels))
        fig = ax.get_figure()
        plt.xticks(rotation=0, ha='center', fontsize=8)
        plt.yticks(fontsize=8)
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))
        ax.yaxis.set_major_formatter(FormatStrFormatter('%.0f'))
        ax.yaxis.set_major_locator(MaxNLocator(6))
        # Convert the raw numbers to numbers with a percentage sign.
        # This means that '25' would become '25%'
        ax.set_yticklabels(['{0:g}%'.format(i) for i in ax.get_yticks()])
        plt.axhline(y=0, color='#d5d5d5', zorder=1)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        plt.ylabel("")
        plt.xlabel("")
        ax.yaxis.grid(True, color="#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetDailyReturns(self, returns = [[],[]], live_returns = [[],[]],
                            name = "daily-returns.png", width = 11.5, height = 2.5,
                            live_color = "#ff9914", backtest_color = "#71c3fc", gray = "#b3bcc0"):
        if len(returns[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        returns[0] = list(returns[0])
        returns[1] = list(returns[1])
        live_returns[0] = list(live_returns[0])
        live_returns[1] = list(live_returns[1])

        plt.figure()
        ax = plt.gca()

        backtest_series = pd.Series(returns[1], index=returns[0])
        live_series = pd.Series(live_returns[1], index=live_returns[0])

        backtest_positive = backtest_series[backtest_series > 0]
        backtest_negative = backtest_series[backtest_series < 0]
        live_positive = live_series[live_series > 0]
        live_negative = live_series[live_series < 0]

        # Backtest
        #ax.bar(returns[0][:min(len(returns[0]),len(returns[1]))], returns[1], color=backtest_color,zorder=2)
        ax.bar(backtest_positive.index, backtest_positive.values, color = backtest_color, zorder = 2)
        ax.bar(backtest_negative.index, backtest_negative.values, color=gray, zorder=2)

        # Live
        #ax.bar(live_returns[0][:min(len(live_returns[0]),len(live_returns[1]))], live_returns[1], color=live_color,zorder=2)
        ax.bar(live_positive.index, live_positive.values, color=live_color, zorder=2)
        ax.bar(live_negative.index, live_negative.values, color=gray, zorder=2)

        # Need to handle this since we don't use a legend if it is only backtesting
        if len(live_returns[0]) > 0:
            rectangles = [plt.Rectangle((0, 0), 1, 1, fc=backtest_color), plt.Rectangle((0, 0), 1, 1, fc=live_color)]
            ax.legend(rectangles, [label for label in ['Backtest', "Live"]], handlelength=0.8, handleheight=0.8,
                      frameon=False, fontsize=8)

        fig = ax.get_figure()
        ax.xaxis_date()
        #ax.set_xticks(fontsize = 8)
        #ax.set_yticks(fontsize = 8)
        ax.set_ylabel("")
        ax.set_xlabel("")
        ax.set_xticklabels(['{0:g}%'.format(i) for i in ax.get_xticks()])
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))
        plt.axhline(y = 0, color = '#d5d5d5')
        plt.setp(ax.spines.values(), color='#d5d5d5')
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.set_axisbelow(True)
        ax.yaxis.grid(True, color = "#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        return base64

    def GetMonthlyReturns(self, returns = {}, live_returns = {}, width=7, height=5, name='monthly-returns.png'):
        months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

        # Populate the list with np.nan so that we can successfully 
        # convert this dict into a DataFrame
        for k in returns.keys():
            while len(returns[k]) != 12:
                returns[k].append(np.nan)

        if len(returns) == 0:
            print("No monthly returns found")
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        # Make data frame
        returns = pd.DataFrame(returns, index = months).transpose()

        def make_colormap(seq):
            seq = [(None,) * 3, 0.0] + list(seq) + [1.0, (None,) * 3]
            cdict = {'red': [], 'green': [], 'blue': []}
            for i, item in enumerate(seq):
                if isinstance(item, float):
                    r1, g1, b1 = seq[i - 1]
                    r2, g2, b2 = seq[i + 1]
                    cdict['red'].append([item, r1, r2])
                    cdict['green'].append([item, g1, g2])
                    cdict['blue'].append([item, b1, b2])
            return mcolors.LinearSegmentedColormap('CustomMap', cdict)

        c = mcolors.ColorConverter().to_rgb
        c_map = make_colormap([c('#CC0000'), 0.1, c('#FF0000'), 0.2, c('#FF3333'),
                               0.3, c('#FF9933'), 0.4, c('#FFFF66'), 0.5, c('#FFFF99'),
                               0.6, c('#B2FF66'), 0.7, c('#99FF33'), 0.8,
                               c('#00FF00'), 0.9, c('#00CC00')])

        if len(live_returns) > 0:
            live_returns = pd.DataFrame(live_returns, index=months).transpose()
            liveC = mcolors.ColorConverter().to_rgb
            live_c_map = make_colormap([liveC('#CC0000'), 0.1, liveC('#FF0000'), 0.2, c('#FF3333'),
                                        0.3, liveC('#FF9933'), 0.4, liveC('#FFFF66'), 0.5, liveC('#FFFF99'),
                                        0.6, liveC('#B2FF66'), 0.7, liveC('#99FF33'), 0.8,
                                        liveC('#00FF00'), 0.9, liveC('#00CC00')])

            fig, ax = plt.subplots(2, 1, gridspec_kw={'height_ratios': [6, 1]})
            ax[0].matshow(returns, aspect='auto', cmap=c_map, interpolation='none', vmin=-10, vmax=10)
            ax[1].matshow(live_returns, aspect='auto', cmap=live_c_map, interpolation='none')
            ax[0].xaxis.set_major_locator(ticker.MaxNLocator(min(12, len(returns.columns))))
            ax[0].yaxis.set_major_locator(ticker.MaxNLocator(len(returns.index.values)))
            ax[0].set_yticklabels([''] + list(returns.index.values))
            ax[0].set_xticklabels([''] + [x for x in returns.columns])
            ax[0].tick_params(labelsize=8, bottom=True, labelbottom=True, top=False, labeltop=False)
            ax[0].set_ylabel('Backtest', rotation='vertical', fontweight='black')
            for (j, i), label in np.ndenumerate(returns):
                if j == 0:
                    ax[0].text(i, j + 0.1, round(label, 1), ha='center', va='top', fontsize=7)
                elif j == (returns.shape[0] - 1):
                    ax[0].text(i, j - 0.1, round(label, 1), ha='center', va='bottom', fontsize=7)
                else:
                    ax[0].text(i, j, round(label, 1), ha='center', va='center', fontsize=7)

            ax[1].xaxis.set_major_locator(ticker.MaxNLocator(min(12, len(live_returns.columns))))
            ax[1].yaxis.set_major_locator(ticker.MaxNLocator(len(live_returns.index.values)))
            ax[1].set_xticklabels([''] + [x for x in live_returns.columns])  ## will need to be fixed for more than 1 year
            ax[1].set_yticklabels([''] + list(live_returns.index.values))
            ax[1].tick_params(labelsize=8, bottom=True, labelbottom=True, top=False, labeltop=False)
            ax[1].set_ylabel('Live', rotation='vertical', fontweight='black')
            for (j, i), label in np.ndenumerate(live_returns):
                ax[1].text(i, j, round(label, 1), ha='center', va='center', fontsize=7)

        else:
            ax = plt.imshow(returns, aspect='auto', cmap=c_map, interpolation='none', vmin=-10, vmax=10)
            fig = ax.get_figure()
            plt.xlabel('')
            plt.ylabel('')
            plt.yticks(range(len(returns.index.values)), returns.index.values, fontsize=8)
            plt.xticks(range(12), ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"])
            for (j, i), label in np.ndenumerate(returns):
                if np.isnan(label):
                    plt.text(i, j, "", ha='center', va='center', fontsize=7)
                else:
                    plt.text(i, j, str(round(label, 1)), ha='center', va='center', fontsize=7)

        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetAnnualReturns(self, data = None, live_data = None, name = "annual-returns.png",width = 3.5*2, height = 2.5*2):

        if data is None:
            data = [[], []]
        if live_data is None:
            live_data = [[], []]

        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        # Cast to list since the data might come in numpy arrays
        time = list(data[0]) + list(live_data[0])
        returns = list(data[1]) + list(live_data[1])

        plt.figure()
        ax = plt.gca()
        # Prevent value speculation on the y-axis ticks by
        # converting to string before plotting.
        ax.barh([str(i) for i in time], returns, color = ["#428BCA"])
        # Add a percentage sign at the end of each x-axis tick
        ax.set_xticklabels(["{0:g}%".format(i) for i in ax.get_xticks()])

        fig = ax.get_figure()
        plt.xticks(rotation=0, ha='center', fontsize=8)
        plt.yticks(fontsize=8)
        plt.axvline(x=0, color='#d5d5d5', linewidth=0.5)
        vline = plt.axvline(x=np.mean(returns), color="red", ls="dashed", label="mean", linewidth=1)
        plt.legend([vline], ["mean"], loc='upper right', frameon=False, fontsize=8)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        plt.xlabel("")
        plt.ylabel("")
        ax.xaxis.grid(True)
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetDrawdown(self, data = [[],[]], live_data = [[],[]], worst = {}, name = "drawdowns.png",
                        width = 11.5, height = 2.5, gray = "#b3bcc0"):
        if len(worst) == 0:
            worst = self.GetDrawdownPeriods(data, live_data)

        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        df, _, __ = self.GetDrawdownPlot(list(data), list(live_data))

        time = list(df.index)
        drawdown = list(df['dd'].mul(-1).values)

        colors = ["#FFCCCCCC", "#FFE5CCCC", "#FFFFCCCC", "#E5FFCCCC", "#CCFFCCCC"]
        labels = ["1st Worst", "2nd Worst", "3rd Worst", "4th Worst", "5th Worst"]
        plt.figure()
        ax = plt.gca()
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))

        # Backtest
        ax.plot(time, drawdown, color=gray, zorder=2)
        ax.fill_between(time, drawdown, 0, color=gray, zorder=3)

        for index, values in enumerate(worst):
            start = values['Begin']
            end = values['End']

            if start == end:
                worst_point = start
            else:
                sub_data = drawdown[time.index(start):time.index(end)]
                worst_point = time[drawdown.index(min(sub_data))]

            plt.axvspan(start, end, 0, 0.95, color = colors[index], zorder = 1)
            plt.axvline(worst_point, 0, 0.95, ls = 'dashed', color = 'black', zorder = 4, linewidth = 0.5)
            ax.text(worst_point, min(drawdown) * 0.75, labels[index], rotation = 90, zorder = 4, va='bottom')

        # Live
        live_time = live_data[0]
        live_drawdown = live_data[1]

        # No need to draw the live mode stuff since we've already taken care of it

        #ax.plot(live_time[:min(len(live_drawdown), len(live_time))], live_drawdown, color = gray, zorder = 2)
        #ax.fill_between(live_time[:min(len(live_drawdown), len(live_time))], live_drawdown, 0, color = gray, zorder = 3)

        plt.axvline(live_time[0], 0, 0.95, ls='dotted', color='red', zorder=4) if len(live_time) > 0 else None
        plt.text(live_time[0], min(min(drawdown), min(live_drawdown)) * 0.75, "Live Trading", rotation=90, zorder=4, fontsize=7) if len(live_time) > 0 else None

        fig = ax.get_figure()
        plt.xticks(rotation=0, ha='center', fontsize=8)
        plt.yticks(fontsize=8)
        plt.ylabel("")
        plt.xlabel("")
        plt.axhline(y=0, color='#d5d5d5', zorder=1)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.grid(True, color="#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetDrawdownPlot(self, data, live_data):
        # Solution taken from https://stackoverflow.com/questions/39058034/pandas-duration-of-drawdown/39080464
        df = pd.DataFrame(list(data[1]) + list([i + data[1][-1] for i in live_data[1]]), index=(list(data[0] + list(live_data[0]))), columns=['close'])

        df['ret'] = df.close/df.close[0]
        df['modMax'] = df.ret.cummax()
        df['dd'] = 1 - df.ret.div(df['modMax'])

        groups = df.groupby(df['modMax'])
        dd = groups['modMax','dd'].apply(lambda g: g[g['dd'] == g['dd'].max()])
        top10dd = dd.sort_values('dd', ascending=False).head(10)

        return (df, dd, top10dd)

    def GetDrawdownPeriods(self, data, live_data):
        # Solution taken from https://stackoverflow.com/questions/39058034/pandas-duration-of-drawdown/39080464
        df, dd, top10dd = self.GetDrawdownPlot(data, live_data)

        def drawdown_group(df,index_list):
            group_max,dd_date = index_list
            ddGroup = df[df['modMax'] == group_max]
            group_length = len(ddGroup)
            group_dd = ddGroup['dd'].max()
            group_dd_length = len(ddGroup[ddGroup.index <= dd_date])
            group_start = ddGroup[0:1].index[0]
            group_end = ddGroup.tail(1).index[0]
            group_rec = group_length - group_dd_length
            return group_start,group_end,group_max,group_dd,dd_date,group_dd_length,group_rec,group_length

        dd_col = ('start','end','peak', 'dd','dd_date','dd_length','dd_rec','tot_length')
        df_dd = pd.DataFrame(columns = dd_col)
        for i in range(1,10):
            index_list = top10dd[i-1:i].index.tolist()[0]
            start,end,peak,dd,dd_date,dd_length,dd_rec,tot_length = drawdown_group(df,index_list)
            df_dd.loc[i-1] = drawdown_group(df,index_list)

        worst = []
        for idx, row in df_dd.drop_duplicates(['start', 'end']).head(n=5).iterrows():
            worst.append({'Begin': row['start'], 'End': row['end'], 'Total': row['dd']})

        return worst

    def GetCrisisEventsPlots(self, data = [[],[],[]], name = '', width = 7, height = 5,
                             backtest_color = "#71c3fc", gray = "#b3bcc0"):
        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(f'{name}.png', fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        plt.figure()
        ax = plt.gca()
        fig = ax.get_figure()
        ax.xaxis.set_major_formatter(DateFormatter("%Y-%m-%d"))
        colors = [backtest_color, gray]
        for j, values in enumerate(data[1:]):
            ax.plot(data[0][:min(len(data[0]),len(values))], values, color=colors[j], linewidth=0.5, zorder=2)
        labels = ['Backtest', 'Benchmark']
        rectangles = [plt.Rectangle((0, 0), 1, 1, fc=backtest_color), plt.Rectangle((0, 0), 1, 1, fc=gray)]
        leg = ax.legend(rectangles, labels, handlelength=0.8, handleheight=0.8,
                        frameon=False, fontsize=8, ncol=len(labels))
        for line in leg.get_lines(): line.set_linewidth(3)
        plt.axhline(y=0, color= gray, zorder=1)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        ax.tick_params(axis='x', labelsize=8, labelrotation=45)
        plt.yticks(fontsize=8)
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        plt.xlabel("")
        plt.ylabel("")
        ax.yaxis.grid(True, color="#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(f'{name}.png', fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetRollingBeta(self, data = [[],[],[],[]], live_data = [[],[],[],[]], name = "rolling-portfolio-beta-to-equity.png",
                           width = 11.5, height = 2.5):
        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        # Data will come in the following format:
        # [backtest time, backtest returns, benchmark time, benchmark returns]
        

        # TODO: Different asset classes can have different rolling window periods due to their trading
        # days. Equities trade 252 days a year, whereas crypto and FOREX are closer to 365 days per year.
        backtest_six_month_beta, backtest_twelve_month_beta = self.CalculateRollingBeta(data)
        live_six_month_beta, live_twelve_month_beta = self.CalculateRollingBeta(live_data)

        labels = ['6 mo.', '12 mo.']
        rectangles = [plt.Rectangle((0, 0), 1, 1, fc="#71c3fc"), plt.Rectangle((0, 0), 1, 1, fc="#1d7dc1")]
        if len(live_six_month_beta) > 0:
            labels += ['Live 6 mo.', 'Live 12 mo.']
            rectangles += [plt.Rectangle((0, 0), 1, 1, fc="#ff9914"), plt.Rectangle((0, 0), 1, 1, fc="#ffd700")]

        plt.figure()
        ax = plt.gca()
        fig = ax.get_figure()
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))

        # Backtest
        ax.plot(backtest_six_month_beta.index, backtest_six_month_beta.values, linewidth = 0.5, color = "#71c3fc")
        ax.plot(backtest_twelve_month_beta.index, backtest_twelve_month_beta.values, linewidth=0.5, color="#1d7dc1")

        # Live
        if len(live_six_month_beta) > 0:
            print(live_six_month_beta)

            ax.plot(live_six_month_beta.index, live_six_month_beta.values, linewidth=0.5, color="#ff9914")
            ax.plot(live_twelve_month_beta.index, live_twelve_month_beta.values, linewidth=0.5, color="#ffd700")

        leg = ax.legend(rectangles, labels, handlelength=0.8, handleheight=0.8,
                        frameon=False, fontsize=8, ncol=2)
        for line in leg.get_lines(): line.set_linewidth(3)
        plt.axhline(y=0, color='#d5d5d5', zorder=1)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        ax.tick_params(axis='both', labelsize=8, labelrotation=0)
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        plt.xlabel("")
        plt.ylabel("")
        ax.set_axisbelow(True)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.grid(True, color="#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def CalculateRollingBeta(self, data):
        if not any(data[0]):
            return pd.Series(), pd.Series()

        equity = pd.DataFrame(data[1], index=data[0], columns=['Strategy']).resample('D').sum()
        benchmark = pd.DataFrame(data[3], index=data[2], columns=['Benchmark'])
        benchmark.index = benchmark.index.date

        returns = equity.join(benchmark).dropna()

        six_month_corr = returns.rolling(22*6).corr()
        six_month_corr = six_month_corr.dropna()['Benchmark'][::2]
        portfolio_std = returns['Strategy'].rolling(22*6).std().dropna()
        benchmark_std = returns['Benchmark'].rolling(22*6).std().dropna()

        six_month_beta = six_month_corr.values * (portfolio_std / benchmark_std)

        twelve_month_corr = returns.rolling(252).corr()
        twelve_month_corr = twelve_month_corr.dropna()['Benchmark'][::2]
        portfolio_std = returns['Strategy'].rolling(252).std().dropna()
        benchmark_std = returns['Benchmark'].rolling(252).std().dropna()

        twelve_month_beta = twelve_month_corr.values * (portfolio_std / benchmark_std)

        return six_month_beta, twelve_month_beta


    def GetRollingSharpeRatio(self, data = [[],[]], live_data = [[],[]], name = "rolling-sharpe-ratio.png",
                                  width = 11.5, height = 2.5, live_color = "#ff9914", backtest_color = "#71c3fc"):
        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        labels = ['6 mo.']
        rectangles = [plt.Rectangle((0, 0), 1, 1, fc=backtest_color)]
        if len(live_data[0]) > 0:  # Need to handle this...
            rectangles += [plt.Rectangle((0, 0,), 1, 1, fc=live_color)]
            labels += ["Live 6 mo."]

        plt.figure()
        ax = plt.gca()
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))

        # Backtest
        ax.plot(data[0][:min(len(data[0]),len(data[1]))], data[1], color = backtest_color, linewidth = 0.5, zorder = 2)

        # Live
        ax.plot(live_data[0][:min(len(live_data[0]), len(live_data[1]))], live_data[1], color=live_color, linewidth=0.5, zorder=2)

        leg = ax.legend(rectangles, labels, handlelength=0.8, handleheight=0.8,
                        frameon=False, fontsize=8)
        for line in leg.get_lines(): line.set_linewidth(3)
        plt.axhline(y=0, color='#d5d5d5', zorder=1)
        plt.setp(ax.spines.values(), color='#d5d5d5')
        ax.tick_params(axis='both', labelsize=8, labelrotation=0)
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        plt.ylabel("")
        plt.xlabel("")
        ax.set_axisbelow(True)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.grid(True, color="#ececec")
        fig = ax.get_figure()
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetAssetAllocation(self, data = [[],[]], live_data = [[],[]],
                              width = 7, height = 5):
        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64("asset-allocation.png", fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        symbols = [data[0], live_data[0]]
        data = [data[1], live_data[1]]
        colors = ['#fce0bd', '#fcd6a7', '#fbcd92', '#fac37c', '#f8af53', '#f79b31', '#de8b2c', "#dde1e3"]
        pies = {}

        for i in range(len(data)):
            symbols_to_use, to_label = symbols[i], data[i]

            # No need to plot if there are no symbols/data -- necessary as we don't want to return a dictionary
            # with even a blank plot for live if only using a backtest
            if len(symbols_to_use) == 0:
                continue

            to_label = to_label[:7]
            symbols_to_use = symbols_to_use[:7]
            if sum(to_label) < 1:
                to_label.append(1 - sum(to_label))
                symbols_to_use.append('Others')

            labels = [f'{symbol}\n{value*100}%' for symbol, value in zip(symbols_to_use, to_label)]

            fig = plt.figure()
            plt.pie(to_label, colors = colors)
            plt.legend(labels, frameon = False, fontsize = 8, loc = 'center left', bbox_to_anchor=(0, 0.5))
            plt.axis('equal')
            fig.set_size_inches(width, height)
            if i == 0:
                pies.update({"Backtest Asset Allocation": self.fig_to_base64(f"asset-allocation-backtest.png", fig)})
            else:
                pies.update({"Live Asset Allocation": self.fig_to_base64(f"asset-allocation-live.png", fig)})
            plt.cla()
            plt.clf()
            plt.close('all')
        return pies

    def GetLeverage(self, data = [[],[]], live_data = [[],[]], name = "leverage.png",width = 11.5,
                        height = 2.5, backtest_color = "#71c3fc", live_color = "#ff9914",):
        if len(data[0]) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        plt.figure()
        ax = plt.gca()
        fig = ax.get_figure()

        # Backtest
        ax.plot(data[0][:min(len(data[0]),len(data[1]))], data[1], color = backtest_color, alpha = 0.75)
        ax.fill_between(data[0][:min(len(data[0]),len(data[1]))], 0, data[1], color = backtest_color, alpha = 0.75)

        # Live
        ax.plot(live_data[0][:min(len(live_data[0]),len(live_data[1]))], live_data[1], color = live_color, alpha = 0.75)
        ax.fill_between(live_data[0][:min(len(live_data[0]),len(live_data[1]))], 0, live_data[1], color=live_color, alpha=0.75, step = 'pre')

        rectangles = [plt.Rectangle((0, 0), 1, 1, fc=backtest_color), plt.Rectangle((0, 0), 1, 1, fc=live_color)]
        ax.legend(rectangles, [label for label in ['Backtest', "Live"]], handlelength=0.8, handleheight=0.8,
                  frameon=False, fontsize=8)
        ax.set_xticklabels(ax.get_xticklabels(), rotation=0, ha='center')
        ax.tick_params(axis='both', labelsize=8, labelrotation=0)
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))
        plt.axhline(y=0, color='#d5d5d5')
        plt.setp(ax.spines.values(), color='#d5d5d5')
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        plt.ylabel("")
        plt.xlabel("")
        ax.set_axisbelow(True)
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        ax.yaxis.grid(True, color="#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64

    def GetExposure(self, time = [], long_securities = [], short_securities = [], long_data = [[]], short_data = [[]],
                        live_time = [], live_long_securities = [], live_short_securities = [], live_long_data = [[]],
                        live_short_data = [[]], name = "exposure.png", width = 11.5, height = 2.5):
        if len(time) == 0:
            fig = plt.figure()
            fig.set_size_inches(width, height)
            base64 = self.fig_to_base64(name, fig)
            plt.cla()
            plt.clf()
            plt.close('all')
            return base64

        color_map = {'Equity': "#71c3fc", 'Option':'#A0522D', 'Commodity':'#4B0082',
                    'Forex':'#0000FF', 'Future':'#6B8E23', 'Cfd':'#FF8C00', 'Crypto':'#BDB76B'}
        live_color_map = {'Equity': "#ff9914" , 'Option': '#DAA520', 'Commodity': '#9400D3',
                          'Forex':'#6495ED', 'Future':'#808000', 'Cfd':'#FFD700', 'Crypto':'#FFDAB9'}
        labels = long_securities + short_securities
        live_labels = live_long_securities + live_short_securities

        ax = plt.gca()

        # No need to check if live is empty or not, this will handle it, just needs to plot whichever has the longer time index first
        if max([len(x) for x in long_data]) > max([len(x) for x in short_data]):
            ax.stackplot(time[:max([len(x) for x in long_data])], np.vstack(long_data),
                         color = [color_map[security] for security in long_securities], alpha = 0.75)
            ax.stackplot(time[:max([len(x) for x in short_data])], np.vstack(short_data),
                         color=[color_map[security] for security in short_securities], alpha=0.75)
        else:
            ax.stackplot(time[:max([len(x) for x in short_data])], np.vstack(short_data),
                         color=[color_map[security] for security in short_securities], alpha=0.75)
            ax.stackplot(time[:max([len(x) for x in long_data])], np.vstack(long_data),
                         color=[color_map[security] for security in long_securities], alpha=0.75)

        if max([len(x) for x in live_long_data]) > max([len(x) for x in live_short_data]):
            ax.stackplot(live_time[:max([len(x) for x in live_long_data])], np.vstack(live_long_data),
                         color=[live_color_map[security] for security in live_long_securities], alpha = 0.75)
            ax.stackplot(live_time[:max([len(x) for x in live_short_data])], np.vstack(live_short_data),
                         color=[live_color_map[security] for security in live_short_securities], alpha = 0.75)
        else:
            ax.stackplot(live_time[:max([len(x) for x in live_short_data])], np.vstack(live_short_data),
                         color=[live_color_map[security] for security in live_short_securities], alpha=0.75)
            ax.stackplot(live_time[:max([len(x) for x in live_long_data])], np.vstack(live_long_data),
                         color=[live_color_map[security] for security in live_long_securities], alpha=0.75)

        labels = list(set(labels))
        live_labels = list(set(live_labels))
        rectangles = [plt.Rectangle((0, 0), 1, 1, fc=color_map[lab]) for lab in labels]
        live_rectangles = [plt.Rectangle((0, 0), 1, 1, fc=live_color_map[lab]) for lab in live_labels]
        ax.legend(rectangles + live_rectangles, labels + [f'{lab} - Live' for lab in live_labels], handlelength=0.8,
                  handleheight=0.8, frameon=False, fontsize=8, ncol=len(labels), loc='upper right')
        fig = ax.get_figure()
        plt.xticks(rotation = 0,ha = 'center', fontsize = 8)
        plt.yticks(fontsize = 8)
        plt.xlabel("")
        ax.axhline(y=0, color = 'black', linewidth = 0.5)
        ax.xaxis.set_major_formatter(DateFormatter("%b %Y"))
        plt.setp(ax.spines.values(), color='#d5d5d5')
        ax.spines['right'].set_visible(False)
        ax.spines['top'].set_visible(False)
        plt.setp([ax.get_xticklines(), ax.get_yticklines()], color='#d5d5d5')
        plt.ylabel("")
        plt.xlabel("")
        ax.set_axisbelow(True)
        ax.yaxis.grid(True, color = "#ececec")
        fig.set_size_inches(width, height)
        base64 = self.fig_to_base64(name, fig)
        plt.cla()
        plt.clf()
        plt.close('all')
        return base64