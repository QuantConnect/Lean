import base64
import os
import os.path
import re
import json

def Base64(image):
    if not os.path.isfile(image) :
        return ""
    return 'data:image/png;base64,' + base64.b64encode(open(image, "rb").read()).decode('utf-8').replace('\n', '')
    
def MethodGetTableHTML(title, ls):
    ret = '''
    <div class="col-xs-4">
        <table id="key-characteristics" class="table qc-table compact">
            <thead><tr>
            <th colspan="2" >''' + title + '''</th>
            </tr></thead>
            <tbody>
            <tr><td>''' + str(ls[0][0]) + ':' + '''</td>
            <td>''' + ("&#10004" if ls[0][1] == 1 else ("&#10006" if ls[0][1] == 0 else str(ls[0][1]))) + '''</td></tr>
            <tr><td>''' + str(ls[1][0]) + ':' + '''</td>
            <td>''' + ("&#10004" if ls[1][1] == 1 else ("&#10006" if ls[1][1] == 0 else str(ls[1][1]))) + '''</td></tr>
            <tr><td>''' + str(ls[2][0]) + ':' + '''</td>
            <td>''' + ("&#10004" if ls[2][1] == 1 else ("&#10006" if ls[2][1] == 0 else str(ls[2][1]))) + '''</td></tr>
            <tr><td>''' + str(ls[3][0]) + ':' + '''</td>
            <td>''' + ("&#10004" if ls[3][1] == 1 else ("&#10006" if ls[3][1] == 0 else str(ls[3][1]))) + '''</td></tr>
            <tr><td>''' + str(ls[4][0]) + ':' + '''</td>
            <td>''' + (str(ls[4][1]) if type(ls[4][1])!=list else ", ".join(ls[4][1])) + '''</td></tr>
            </tbody>
        </table>
    </div>'''
    return ret

def MethodGetImageBoxHTML(title, url, col = 4):
    if not url:
        return ""
    ret = '''
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
    return ret

def MethodGetCrisisImageHTML(ls,crisis_list, outdir):
    crisis_title = ["Crisis "+ x for x in crisis_list]
    crisis_image = [outdir + "/" + re.sub(r' ','-',x.lower())+".png" for x in crisis_title ]
    
    ret = '''
    <div class="content">
    '''
    count = ls[0]
    index = ls[1]
    for i in range(index,len(crisis_title)):
        if os.path.isfile(crisis_image[i]):
            ls[2] = True
            if count % 3 == 0 :
                ret += '''<div class="container-row">'''
                ret += MethodGetImageBoxHTML(crisis_title[i], Base64(crisis_image[i]))
            elif count % 3 == 1 :
                ret += MethodGetImageBoxHTML(crisis_title[i], Base64(crisis_image[i]))
            else:
                ret += MethodGetImageBoxHTML(crisis_title[i], Base64(crisis_image[i]))
                ret += '''</div>'''
            count += 1
        ls[0], ls[1] = count, i 
        if count == 5*3:
            ls[0] = 0
            break
    ls[1] += 1
    
    if count % 3 != 0:
        ret += '''</div>'''
    ret += '''</div>'''
    return ret

def MethodGetCrisisPageHTML(outdir):
    crisis_list = ["Dotcom","9-11","US Housing Bubble 2003","Lehman Brothers","Flash Crash",
                       "Aug07","Mar08","Sept08","2009Q1","2009Q2",
                       "US Downgrade-European Debt Crisis","Fukushima Melt Down 2011","ECB IR Event 2012",
                       "Apr14","Oct14","Fall2015",
                       "Low Volatility Bull Market","GFC Crash","Recovery","New Normal"]
    ls = [0,0,False]
    ret = ''''''
    while(ls[1] < len(crisis_list)):
        tmp = '''
        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-title">Backtest Crisis Analysis</div>
                <div class="header-right">''' + rightHeaderText + '''</div>
            </div> ''' + MethodGetCrisisImageHTML(ls, crisis_list,outdir) + '''
            <div class="footer">
                <div class="footer-id">Hash:''' + footerId + '''</div>
                Democratizing Finance, Empowering Individuals
                <div class="footer-page"><?= $pageNumber++ ?></div>
            </div>
        </div>'''
        if ls[2]:
            ret += tmp
        ls[2] = False
    return ret

def MethodGetAssetAllocationImageHTML(ls,asset_list, outdir):
    asset_title = asset_list
    asset_image = [outdir + "/" + "asset-allocation-"+x+".png" for x in asset_title ]
    
    ret = '''
    <div class="content">
    '''
    count = ls[0]
    index = ls[1]
    for i in range(index,len(asset_title)):
        if os.path.isfile(asset_image[i]):
            ls[2] = True
            if count % 3 == 0 :
                ret += '''<div class="container-row">'''
                ret += MethodGetImageBoxHTML(asset_title[i], Base64(asset_image[i]))
            elif count % 3 == 1 :
                ret += MethodGetImageBoxHTML(asset_title[i], Base64(asset_image[i]))
            else:
                ret += MethodGetImageBoxHTML(asset_title[i], Base64(asset_image[i]))
                ret += '''</div>'''
            count += 1
        ls[0], ls[1] = count, i 
        if count == 5*3:
            ls[0] = 0
            break
    ls[1] += 1
    
    if count % 3 != 0:
        ret += '''</div>'''
    ret += '''</div>'''
    return ret 

def MethodGetAssetAllocationPageHTML(outdir):
    asset_list = ['Equity', 'Option', 'Commodity', 'Forex', 'Future', 'Cfd', 'Crypto']
    ls = [0,0,False]
    ret = ''''''
    while(ls[1] < len(asset_list)):
        tmp = '''
        <div class="page">
            <div class="header">
                <div class="header-left">
                    <img src="https://cdn.quantconnect.com/web/i/logo.png">
                </div>
                <div class="header-title">Backtest Crisis Analysis</div>
                <div class="header-right">''' + rightHeaderText + '''</div>
            </div> ''' + MethodGetAssetAllocationImageHTML(ls, asset_list, outdir) + '''
            <div class="footer">
                <div class="footer-id">Hash:''' + footerId + '''</div>
                Democratizing Finance, Empowering Individuals
                <div class="footer-page"><?= $pageNumber++ ?></div>
            </div>
        </div>'''
        if ls[2]:
            ret += tmp
        ls[2] = False
    return ret

rightHeaderText = "Basic Template Algorithm"
footerId = ""
strategyDescription = "Put your strategy description here:"
authorProfile = "D:\fakepath\LeanReportCreatorInPython\AuthorProfile.jpg"
authorName = "Xiang Li"
authorBio = "Put your biography here."

def GenerateHTMLReport(outdir):
    
    f = open(outdir + "/" + 'Report.html','w+')
    
    chartMonthlyReturns = Base64(outdir + "/" + "monthly-returns.png")
    chartCumulativeReturns = Base64(outdir + "/" + "cumulative-return.png")
    chartAnnualReturns = Base64(outdir + "/" + "annual-returns.png")
    chartReturnsHistogram = Base64(outdir + "/" + "distribution-of-monthly-returns.png")
    chartAssetAllocation = Base64(outdir + "/" + "asset-allocation-all.png")
    chartDrawdown = Base64(outdir + "/" + "drawdowns.png")
    chartDailyReturns = Base64(outdir + "/" + "daily-returns.png")
    chartRollingBeta = Base64(outdir + "/" + "rolling-portfolio-beta-to-equity.png")
    chartRollingSP = Base64(outdir + "/" + "rolling-sharpe-ratio(6-month).png")
    chartNetHoldings = Base64(outdir + "/" + "net-holdings.png")
    chartLeverage = Base64(outdir + "/" + "leverage.png")
    
    locationPrefix = "https://www.quantconnect.com/terminal"
    
    with open(outdir + "/" + "strategy-statistics.json") as ff:    
        tmp = json.load(ff)    
        keyCharacteristics  =  tmp["Key Characteristics"]
        keyStatistics = tmp["Key Statistics"]
    
    html = '''<!DOCTYPE html>
    <html xmlns="http://www.w3.org/1999/xhtml">
    <head>
        <meta charset="utf-8"/>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
        <meta property="og:title" content="QuantConnect Backtest Report: ''' + rightHeaderText + '''"/>
        <meta property="og:type" content="website"/>
        <meta property="og:url" content="https://www.quantconnect.com/terminal/reports/''' + footerId + '''"/>
        <meta property="og:description" content="''' + strategyDescription + '''"/>
        <meta property="og:site_name" content="QuantConnect.com"/>
        <meta property="og:url" content="https://www.quantconnect.com/terminal/reports/''' + footerId + '''"/>
        <meta property="og:image" content="''' + chartCumulativeReturns + '''"/>
        <meta property="og:image" content="''' + chartAnnualReturns + '''"/>
        <meta property="og:image" content="https://cdn.quantconnect.com/web/i/users/profile/''' + authorProfile + '''"/>
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
    
            #author-box .fa-pencil,
            #description-box .fa-pencil {
                display: none;
                position: absolute;
                bottom: 0;
                right: 0;
            }
    
            #description-box:hover .fa-pencil,
            #author-box:hover .fa-pencil {
                display: block;
            }
    
        </style>
    </head>
    <body>
    
    <div style="position: fixed;bottom: 50px;right: 50px;">
        <a href="https://www.quantconnect.com/terminal/processReports/get/pdf/''' + footerId + '''" target="_blank" class="btn btn-lg btn-default hide-print">
            <i class="fa fa-download"></i>
        </a>
    </div>
    
    <div class="page">
        <div class="header">
            <div class="header-left">
                <img src="https://cdn.quantconnect.com/web/i/logo.png">
            </div>
            <div class="header-title">Strategy Report Summary</div>
            <div class="header-right">''' + rightHeaderText + '''</div>
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
                                <p id="strategy-description" class="text-justify editable" contentEditable="true" style="max-width: 590px;overflow: hidden;">''' + strategyDescription + '''</p>
                                <i class="fa fa-pencil" style="margin: 15px;"></i>
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
                                About the Author <span class="pull-right">''' + authorName + '''</span>
                            </th>
                        </tr>
                        </thead>
                        <tbody>
                        <tr>
                            <td>
                                <img src="''' + authorProfile + '''">
                                <p id="author-bio" class="text-justify editable" contentEditable="true" style="max-width: 286px;">''' + authorBio + '''</p>
                                <i class="fa fa-pencil" style="margin: 15px 10px"></i>
                            </td>
                        </tr>
                        </tbody>
                    </table>
                </div>
            </div>
            <div class="container-row">
                ''' + MethodGetTableHTML('Key Characteristics', keyCharacteristics)  + '''
                ''' + MethodGetTableHTML('Key Statistics', keyStatistics) + '''
                ''' + MethodGetImageBoxHTML('Monthly Returns', chartMonthlyReturns) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Cumulative Returns', chartCumulativeReturns, 12) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Annual Returns', chartAnnualReturns) + '''
                ''' + MethodGetImageBoxHTML('Return Histogram', chartReturnsHistogram) + '''
                ''' + MethodGetImageBoxHTML('Asset Allocation', chartAssetAllocation) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Drawdown', chartDrawdown, 12) + '''
            </div>
        </div>
        <div class="footer">
            <div class="footer-id">Hash:''' + footerId + '''</div>
            Democratizing Finance, Empowering Individuals
            <div class="footer-page"><?= $pageNumber++ ?></div>
        </div>
    </div>
    
    <div class="page">
        <div class="header">
            <div class="header-left">
                <img src="https://cdn.quantconnect.com/web/i/logo.png">
            </div>
            <div class="header-title">Backtest Strategy Analysis</div>
            <div class="header-right">''' + rightHeaderText + '''</div>
        </div>
        <div class="content">
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Daily Returns', chartDailyReturns, 12) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Rolling Portfolio Beta to Equity', chartRollingBeta, 12) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Rolling Sharpe Ratio (6 Months)', chartRollingSP, 12) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Net Holdings', chartNetHoldings, 12) + '''
            </div>
            <div class="container-row">
                ''' + MethodGetImageBoxHTML('Leverage', chartLeverage, 12) + '''
            </div>
        </div>
        <div class="footer">
            <div class="footer-id">Hash:''' + footerId + '''</div>
            Democratizing Finance, Empowering Individuals
            <div class="footer-page"><?= $pageNumber++ ?></div>
        </div>
    </div>
    ''' + MethodGetCrisisPageHTML(outdir) + ''' 
    ''' + MethodGetAssetAllocationPageHTML(outdir) + ''' 
    </body>
    </html>'''
    
    f.write(html)
    f.close()
    
    dir_name = outdir
    test = os.listdir(dir_name)
    
    for item in test:
        if item.endswith(".png") or item.endswith(".json"):
            os.remove(os.path.join(dir_name, item))