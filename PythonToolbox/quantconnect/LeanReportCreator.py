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
            <link href="https://fonts.googleapis.com/css?family=Open+Sans+Condensed:300,700&display=swap" rel="stylesheet">

            <style>
                #table-summary .fa-times {
                    color: #d9534f;
                }

                #author-metadata tr > td:first-child,
                #project-metadata tr > td:first-child,
                #key-characteristics tr > td:first-child {
                    width: 66%;
                }

                #key-characteristics tr > td:first-child {
                    width: 50%;
                }

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

                table.table {
                    height: 230px;
                }

                table img {
                    width: 100%;
                    height: 100%;
                    max-height: 225px;
                }

                .col-xs-12 table > tbody > tr > td {
                    text-align: center;
                }

                table.table > thead > tr > th {
                    border-bottom: none;
                    font-weight: bold;
                    font-size: 18px;
                    padding-top: 15px;
                    font-family: 'Open Sans Condensed', sans-serif;
                }

                table.table > tbody > tr > td {
                    border-top: none;
                    font-size: 15px;
                    padding-top: 10px;
                    padding-bottom: 10px;
                }

                table#key-characteristics {
                    width: calc(100% - 30px);
                }

                table#key-characteristics > tbody > tr {
                    border-bottom: 1px solid #cbd1d4;
                }

                table#key-characteristics > tbody > tr:first-child {
                    border-top: 1px solid #9c9c9c;
                }

                table#key-characteristics > tbody > tr > td:last-child {
                    width: 5%;
                    text-align: center;
                    position: relative;
                    padding: 0;
                    font-weight: bold;
                }

                table#key-characteristics > tbody > tr > td  > span.markets {
                    background: #8f9ca3;
                    font-size: 11px;
                    color: #fff;
                    padding: 8px 14px;
                    border-radius: 4px;
                }

                .col-xs-4:nth-child(2) table#key-characteristics > tbody > tr > td:last-child {
                    text-align: right;
                }

                table#key-characteristics > tbody > tr > td:first-child {
                    border-top: #c3cace;
                }

                table#description-box {
                    word-wrap: break-word;
                    min-height: 225px;
                }

                table#description-box > thead > tr > th > p{
                    color: #f5ae29;
                    font-size: 24px;
                }

                table#description-box > thead > tr > th > p > span {
                    font-weight: 100;
                }

                table#description-box > thead > tr > th > p > span {
                    margin-right: 10px;
                    width: 1px;
                    height: 24px;
                    background: #f5ae29;
                }

                .page {
                    width: 1200px;
                    height: 1697px;
                    page-break-inside: avoid;
                }

                .page .content {
                    top: 80px;
                    left: 110px;
                    right: 110px;
                    border-top: 1px solid #888888;
                    border-bottom: none;
                    padding: 0;
                }

                .page .header {
                    height: 80px;
                    left: 110px;
                    right: 110px;
                    padding: 0;
                }

                .header .header-left img {
                    width: 230px;
                    height: auto;
                    padding-top: 0;
                    margin-top: 25px;
                }

                .header .header-right {
                    font-family: 'Open Sans Condensed', sans-serif;
                    font-weight: bold;
                    margin-top: 40px;
                    line-height: 23px;
                    float: right;
                    font-size: 18px;
                    max-width: 70%;
                    text-overflow: ellipsis;
                    white-space: nowrap;
                    overflow: hidden;
                }

                .container-row {
                    height: auto;
                    overflow: auto;
                    border-bottom: 1px solid #b8b8b8;
                }

                .page:first-of-type .container-row:first-of-type {
                    padding: 10px 0;
                }

                .container-row:empty {
                    border: none;
                }

                span.checkmark, span.exmark {
                    border-radius: 50%;
                    position: absolute;
                    border: none;
                    top: 10px;
                    right: 25px;
                }

                span.checkmark {
                    background-color: #46bd6a;
                    height: 20px;
                    width: 20px;
                }

                span.checkmark:after {
                    content: "";
                    position: absolute;
                    left: 8px;
                    top: 3px;
                    width: 5px;
                    height: 10px;
                    border: solid #fff;
                    border-width: 0 1px 1px 0;
                    -webkit-transform: rotate(45deg);
                    -ms-transform: rotate(45deg);
                    transform: rotate(45deg);
                }

                span.exmark {
                    background-color:#bc4143;
                    color: white;
                    font-size: 12px;
                    padding: 4px 5px;
                    padding-top: 4px;
                    line-height: 1;
                }

                p#strategy-description {
                    overflow: hidden;
                    max-height: 130px;
                    margin-bottom: 0;
                    word-break: break-word;
                }

            </style>
        </head>
        <body>''' + downloadButton + '''
        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-right">Strategy Report Summary: ''' + self.user['projectName'] + '''</div>
            </div>
            <div class="content">
                <h1 class="hidden">Strategy Report</h1>
                <div class="container-row">
                    <div class="col-xs-12">
                        <table id="description-box" class="table compact no-margin align-top">
                            <thead>
                            <tr>
                                <th>
                                    <p>
                                        <span>|</span>Strategy Description
                                    </p>
                                </th>
                            </tr>
                            </thead>
                            <tbody>
                            <tr>
                                <td>
                                    <p id="strategy-description" class="text-justify editable">''' + self.user['projectDescription'] + '''</p>
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
        </div>

        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-right">Strategy Report Summary: ''' + self.user['projectName'] + '''</div>
            </div>
            <div class="content">
                <div class="container-row">''' + chartDailyReturns + '''</div>
                <div class="container-row">''' + chartRollingBeta + '''</div>
                <div class="container-row">''' + chartRollingSP + '''</div>
                <div class="container-row">''' + chartNetHoldings + '''</div>
                <div class="container-row">''' + chartLeverage + '''</div>
            </div>
        </div>
        ''' + self.get_pages_from_two_dict(crisis, assets) + '''
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
            <table id="key-characteristics" class="table compact">
                <thead><tr>
                <th colspan="2">{title}</th>
                </tr></thead>
                <tbody>'''

        for title, value in dict.items():
            if isinstance(value, list):
                value = ", ".join(value)
            if isinstance(value, bool):
                value = '''<span class="checkmark"></span>''' if value else '''<span class="exmark">&#x2715;</span>'''
            if title == 'Markets':
                ret += f'''<tr><td>{title}</td><td><span class="markets">{value}</span></td></tr>'''
            else:
                ret += f'''<tr><td>{title}</td><td>{value}</td></tr>'''

        return ret + '''
                </tbody>
            </table>
        </div>'''

    def get_image_box(self, title, url, col = 4):
        return "" if not url else '''
        <div class="col-xs-''' + str(col) + '''">
            <table class="table compact">
                <thead>
                <tr>
                    <th>''' + title + '''</th>
                </tr>
                </thead>
                <tbody>
                <tr>
                    <td style="padding:0;">
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
                    if i + j >= stop: break
                    elif titles:
                        ret += dict.pop(titles[i+j])
                ret += '''</div>'''

            return ret + '''</div>'''

    def get_pages_from_two_dict(self, dict1, dict2):
        num_empty_block = 0
        if (len(dict1) % 15) % 3  != 0:
            num_empty_block = 3 - (len(dict1) % 15) % 3
            for i in range (0, num_empty_block):
                dict1["empty_space" + str(i)] = '''<div class="col-xs-4" style="height: 305px;"></div>'''
        dict1.update(dict2)

        ret = ''''''
        while(len(dict1) > 0):
            ret += '''
            <div class="page">
                <div class="header">
                    <div class="header-left">
                        <img src="https://cdn.quantconnect.com/web/i/logo.png">
                    </div>
                    <div class="header-right">Strategy Report Summary: ''' + self.user['projectName'] + '''</div>
                </div> ''' + self.get_image_from_dict(dict1) + '''
            </div>'''
        return ret