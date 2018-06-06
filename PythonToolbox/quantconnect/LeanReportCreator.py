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
import os
import json
from quantconnect.LeanOutputReader import LeanOutputReader

class LeanReportCreator(object):
    def __init__(self, argv, save_images = True):

        input, self.output, user_data = self.read_input(argv)

        self.user = self.read_user_data(user_data)
        self.hash = self.user.pop('backtestHash', '')
        self.count = 0

        # Read input file and pass it to the LeanOutputReader
        data = dict()
        with open(input, 'r') as fp:
            data = json.load(fp)

        outdir = os.path.dirname(self.output) if save_images else None
        self.reader = LeanOutputReader(data, 200, outdir)


    def read_input(self, args):
        if type(args) is str:
            args = args.split(' ')

        tmp = next((x for x in args if x.strip().startswith('--backtest')), None)
        if tmp is None:
            raise KeyError('Please provide --backtest=file.json argument')
        input = os.path.abspath(tmp[11:])
        if not os.path.isfile(input):
            raise FileNotFoundError(f'Backtest file not found: {input}')

        tmp = next((x for x in args if x.strip().startswith('--user')), None)
        if tmp is None:
            tmp = '--user=user_data.json'
        user = tmp[7:]

        tmp = next((x for x in args if x.strip().startswith('--output')), None)
        if tmp is None:
            tmp = f'--output={input[:-5]}.html'
        output = os.path.abspath(tmp[9:])
        
        # create output directory
        os.makedirs(os.path.dirname(output), exist_ok = True)

        return input, output, user

    def read_user_data(self, file):
        if os.path.isfile(file):
            with open(file, 'r', encoding = "utf-8") as fp:
                return json.load(fp)

        return {
            "authorName": "QuantConnect User",
            "authorPicture": "AuthorProfile.png",
            "authorBiography": "Put your biography here.",
            "projectName": "Basic Template Algorithm",
            "projectDescription": "Basic Template Algorithm",
        }

    def get_footer(self):
        self.count += 1
        return f'''
            <div class="footer">
                <div class="footer-id">Hash: {self.hash} </div>
                Democratizing Finance, Empowering Individuals
                <div class="footer-page">{self.count}</div>
            </div>'''

    def create(self):

        assets = self.reader.asset_allocation()
        for title, image in assets.items():
            assets[title] = self.get_image_box(title, image)

        crisis = self.reader.crisis_events()
        for title, image in crisis.items():
            crisis[title] = self.get_image_box(title, image)
        
        chartAssetAllocation = assets.pop("Asset Allocation", str())
        chartAnnualReturns = self.reader.annual_returns()
        chartCumulativeReturns = self.reader.cumulative_return()

        chartMonthlyReturns = self.get_image_box('Monthly Returns', self.reader.monthly_returns())
        chartReturnsHistogram = self.get_image_box('Return Histogram', self.reader.monthly_return_distribution())
        chartDrawdown = self.get_image_box('Drawdown', self.reader.drawdown(), 12)
        chartDailyReturns = self.get_image_box('Daily Returns', self.reader.daily_returns(), 12)
        chartRollingBeta = self.get_image_box('Rolling Portfolio Beta to Equity', self.reader.rolling_beta(), 12)
        chartRollingSP = self.get_image_box('Rolling Sharpe Ratio (6 Months)', self.reader.rolling_sharpe(), 12)
        chartNetHoldings = self.get_image_box('Net Holdings', self.reader.net_holdings(), 12)
        chartLeverage = self.get_image_box('Leverage', self.reader.leverage(), 12)

        tmp = self.reader.statistics()
        keyStatistics = self.get_table('Key Statistics', tmp.get("Key Statistics"))
        keyCharacteristics = self.get_table('Key Characteristics', tmp.get("Key Characteristics"))

        locationPrefix = "https://www.quantconnect.com/terminal"

        downloadButton = '' if len(self.hash) == 0 else f'''
            <div style="position: fixed;bottom: 50px;right: 50px;">
                <a href="https://www.quantconnect.com/terminal/processReports/get/pdf/{self.hash}" target="_blank" class="btn btn-lg btn-default hide-print">
                    <i class="fa fa-download"></i>
                </a>
            </div>'''

        html = '''<!DOCTYPE html>
        <html xmlns="http://www.w3.org/1999/xhtml">
        <head>
            <meta charset="utf-8"/>
            <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
            <meta property="og:title" content="QuantConnect Backtest Report: ''' + self.user['projectName'] + '''"/>
            <meta property="og:type" content="website"/>
            <meta property="og:site_name" content="QuantConnect.com"/>
            <meta property="og:description" content="''' + self.user['projectDescription'] + '''"/>
            <meta property="og:url" content="https://www.quantconnect.com/terminal/reports/''' + self.hash + '''"/>
            <meta property="og:image" content="''' + chartCumulativeReturns + '''"/>
            <meta property="og:image" content="''' + chartAnnualReturns + '''"/>
            <meta property="og:image" content=''' + self.user['authorPicture'] + '''"/>
            <link rel="stylesheet" href="''' + locationPrefix + '''/css/reports/bootstrap.css">
            <link rel="stylesheet" href="''' + locationPrefix + '''/css/reports/font-awesome.css">
            <link rel="stylesheet" href="''' + locationPrefix + '''/css/reports/pdf.css">
            <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">
            <script src="''' + locationPrefix + '''/js/libraries/jquery.js"></script>
            <script src="''' + locationPrefix + '''/js/libraries/bootstrap.min.js"></script>
            <script src="''' + locationPrefix + '''/js/libraries/jquery.growl.js"></script>
            <link rel="stylesheet" href="''' + locationPrefix + '''/css/libraries/jquery.growl.css">
    
            <style>
                #table-summary .fa-times {
                    color: #d9534f;
                }
    
                #key-statistics tr > td:first-child,
                #author-metadata tr > td:first-child,
                #project-metadata tr > td:first-child,
                #key-statistics tr > td:first-child,
                #key-characteristics tr > td:first-child {
                    font-family: Norpeth-Bold, Helvetica, Arial, sans-serif;
                    font-weight: bold;
                }
    
                #author-metadata tr > td:first-child,
                #project-metadata tr > td:first-child,
                #key-characteristics tr > td:first-child {
                    width: 66%;
                }
    
                #key-characteristics tr > td:first-child {
                    width: 50%;
                }
    
                #key-characteristics tr > td:last-child {
                    text-align: center;
                    padding: 0;
                }
    
                #author-metadata tr > td:last-child,
                #project-metadata tr > td:last-child {
                    text-align: center;
                }
    
                #key-statistics tr > td:last-child {
                    text-align: right;
                }
    
                #key-statistics tr > td,
                #key-characteristics tr > td,
                table.table tr > td {
                    vertical-align: middle;
                }
    
                table.table.align-top tr > td {
                    vertical-align: top;
                }
    
                .table.qc-table.compact {
                    height: 234px;
                }
    
                .table.qc-table.compact img {
                    width: 100%;
                    height: 100%;
                    max-height: 196px;
                }
    
                #author-box img {
                    max-width: 33%;
                    height: auto;
                    max-height: 33%;
                    float: left;
                    margin-right: 5px;
                    margin-bottom: 5px;
                }
    
                .header .header-title {
                    margin-top: 40px;
                    line-height: 30px;
                    position: absolute;
                    left: 0;
                    right: 0;
                    text-align: center;
                }
    
                #strategy-description,
                #author-bio {
                    max-height: 194px;
                    word-wrap: break-word;
                    height: 100%;
                }

            </style>
        </head>
        <body>''' + downloadButton + '''
        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-title">Strategy Report Summary</div>
                <div class="header-right">''' + self.user['projectName'] + '''</div>
            </div>
            <div class="content">
                <h1 class="hidden">Strategy Report</h1>
                <div class="container-row">
                    <div class="col-xs-8">
                        <table id="description-box" class="table qc-table compact no-margin align-top">
                            <thead>
                            <tr>
                                <th>
                                    Strategy Description
                                </th>
                            </tr>
                            </thead>
                            <tbody>
                            <tr>
                                <td>
                                    <p id="strategy-description" class="text-justify editable" style="max-width: 590px;overflow: hidden;height:193px">''' + self.user['projectDescription'] + '''</p>
                                </td>
                            </tr>
                            </tbody>
                        </table>
                    </div>
                    <div class="col-xs-4">
                        <table id="author-box" class="table qc-table compact no-margin align-top">
                            <thead>
                            <tr>
                                <th>
                                    About the Author <span class="pull-right">''' + self.user['authorName'] + '''</span>
                                </th>
                            </tr>
                            </thead>
                            <tbody>
                            <tr>
                                <td style="overflow: hidden;">
                                    <img src="''' + self.user['authorPicture'] + '''">
                                    <p id="author-bio" class="text-justify editable" style="max-width: 286px;">''' + self.user['authorBiography']+ '''</p>
                                </td>
                            </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
                <div class="container-row">
                    ''' + keyCharacteristics  + '''
                    ''' + keyStatistics + '''
                    ''' + chartMonthlyReturns + '''
                </div>
                <div class="container-row">
                    ''' + self.get_image_box('Cumulative Returns', chartCumulativeReturns, 12) + '''
                </div>
                <div class="container-row">
                    ''' + self.get_image_box('Annual Returns', chartAnnualReturns) + '''
                    ''' + chartReturnsHistogram + '''
                    ''' + chartAssetAllocation + '''
                </div>
                <div class="container-row">
                    ''' + chartDrawdown + '''
                </div>
            </div>
            ''' + self.get_footer() + '''
        </div>
    
        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-title">Backtest Strategy Analysis</div>
                <div class="header-right">''' + self.user['projectName'] + '''</div>
            </div>
            <div class="content">
                <div class="container-row">
                    ''' + chartDailyReturns + '''
                </div>
                <div class="container-row">
                    ''' + chartRollingBeta + '''
                </div>
                <div class="container-row">
                    ''' + chartRollingSP + '''
                </div>
                <div class="container-row">
                    ''' + chartNetHoldings + '''
                </div>
                <div class="container-row">
                    ''' + chartLeverage + '''
                </div>
            </div>
            ''' + self.get_footer() + '''
        </div>
        ''' + self.get_page_from_dict("Backtest Crisis Analysis", crisis) + ''' 
        ''' + self.get_page_from_dict("Asset Allocation", assets) + ''' 
        </body>
        </html>'''

        with open(self.output, 'w', encoding = "utf-8") as fp:
            fp.write(html)

        return html

    def clean(self):
        outdir = os.path.dirname(self.output)
        items = os.listdir(outdir)
        for item in items:
            if item.endswith(".png"):
                os.remove(os.path.join(outdir, item))

    def get_table(self, title, dict):
        ret = f'''
        <div class="col-xs-4">
            <table id="key-characteristics" class="table qc-table compact">
                <thead><tr>
                <th colspan="2">{title}</th>
                </tr></thead>
                <tbody>'''

        for title, value in dict.items():
            if isinstance(value, list):
                value = ", ".join(value)
            if isinstance(value, bool):
                value = '&#10004' if value else '&#10006'
            ret += f'''<tr><td>{title}</td><td>{value}</td></tr>'''
    
        return ret + '''
                </tbody>
            </table>
        </div>'''

    def get_image_box(self, title, url, col = 4):
        return "" if not url else '''
        <div class="col-xs-''' + str(col) + '''">
            <table class="table qc-table compact">
                <thead>
                <tr>
                    <th>''' + title + '''</th>
                </tr>
                </thead>
                <tbody>
                <tr>
                    <td>
                        ''' + (( '''<img src="''' + url + '''">''' ) if url else "") + '''
                    </td>
                </tr>
                </tbody>
            </table>
        </div>'''

    def get_image_from_dict(self, dict):
        ret = '''<div class="content">'''
        titles = list(dict.keys())
        stop = min(15, len(titles))
        
        for i in range(0, stop, 3):
            ret += '''<div class="container-row">'''
            for j in range(0, 3):
                if i + j >= stop: continue
                ret += dict.pop(titles[i + j])
            ret += '''</div>'''

        return ret + '''</div>'''

    def get_page_from_dict(self, title, dict):
        ret = ''''''
        while(len(dict) > 0):
            ret += '''
            <div class="page">
                <div class="header">
                    <div class="header-left">
                        <img src="https://cdn.quantconnect.com/web/i/logo.png">
                    </div>
                    <div class="header-title">''' + title + '''</div>
                    <div class="header-right">''' + self.user['projectName'] + '''</div>
                </div> ''' + self.get_image_from_dict(dict) + '''
                ''' + self.get_footer() + '''
            </div>'''
        return ret