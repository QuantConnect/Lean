/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Research.RegressionTemplates
{
    /// <summary>
    /// Basic template framework for regression testing of research notebooks
    /// </summary>
    public class BasicTemplateCustomDataTypeHistoryResearchCSharp : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"c5f3ed6d\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.003499,     \"end_t" +
            "ime\": \"2023-02-17T23:37:52.736173\",     \"exception\": false,     \"start_time\": \"2023-02-17T23:37:52.732674\",     \"status\": \"completed\"    " +
            "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/icon.png)\",    \"<hr>\"   ]  },  {   \"cell_type" +
            "\": \"markdown\",   \"id\": \"2025ecbd\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.002997,     \"end_time\": \"2023-02-17T23:37:52.74" +
            "1673\",     \"exception\": false,     \"start_time\": \"2023-02-17T23:37:52.738676\",     \"status\": \"completed\"    },    \"tags\": []   },   \"sou" +
            "rce\": [    \"# Custom data history\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"bbc993a0\",   \"metadata\": {    \"e" +
            "xecution\": {     \"iopub.execute_input\": \"2023-02-17T23:37:52.761679Z\",     \"iopub.status.busy\": \"2023-02-17T23:37:52.753179Z\",     \"iopub.st" +
            "atus.idle\": \"2023-02-17T23:38:01.186963Z\",     \"shell.execute_reply\": \"2023-02-17T23:38:01.173466Z\"    },    \"papermill\": {     \"duration\":" +
            " 8.442283,     \"end_time\": \"2023-02-17T23:38:01.187462\",     \"exception\": false,     \"start_time\": \"2023-02-17T23:37:52.745179\",     \"statu" +
            "s\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [    {     \"data\": {      \"text/" +
            "html\": [       \"\",       \"<div>\",       \"    <div id='dotnet-interactive-this-cell-122456.Microsoft.DotNet.Interactive.Http.HttpPort' style='dis" +
            "play: none'>\",       \"        The below script needs to be able to find the current output cell; this is an easy method to get it.\",       \"    </" +
            "div>\",       \"    <script type='text/javascript'>\",       \"async function probeAddresses(probingAddresses) {\",       \"    function timeout(ms, p" +
            "romise) {\",       \"        return new Promise(function (resolve, reject) {\",       \"            setTimeout(function () {\",       \"              " +
            "  reject(new Error('timeout'))\",       \"            }, ms)\",       \"            promise.then(resolve, reject)\",       \"        })\",       \"   " +
            " }\",       \"\",       \"    if (Array.isArray(probingAddresses)) {\",       \"        for (let i = 0; i < probingAddresses.length; i++) {\",       \"" +
            "\",       \"            let rootUrl = probingAddresses[i];\",       \"\",       \"            if (!rootUrl.endsWith('/')) {\",       \"               " +
            " rootUrl = `${rootUrl}/`;\",       \"            }\",       \"\",       \"            try {\",       \"                let response = await timeout(10" +
            "00, fetch(`${rootUrl}discovery`, {\",       \"                    method: 'POST',\",       \"                    cache: 'no-cache',\",       \"       " +
            "             mode: 'cors',\",       \"                    timeout: 1000,\",       \"                    headers: {\",       \"                        " +
            "'Content-Type': 'text/plain'\",       \"                    },\",       \"                    body: probingAddresses[i]\",       \"                }))" +
            ";\",       \"\",       \"                if (response.status == 200) {\",       \"                    return rootUrl;\",       \"                }\", " +
            "      \"            }\",       \"            catch (e) { }\",       \"        }\",       \"    }\",       \"}\",       \"\",       \"function loadDotn" +
            "etInteractiveApi() {\",       \"    probeAddresses([\\\"http://172.19.192.1:1000/\\\", \\\"http://192.168.56.1:1000/\\\", \\\"http://192.168.16.104:10" +
            "00/\\\", \\\"http://127.0.0.1:1000/\\\"])\",       \"        .then((root) => {\",       \"        // use probing to find host url and api resources\"," +
            "       \"        // load interactive helpers and language services\",       \"        let dotnetInteractiveRequire = require.config({\",       \"     " +
            "   context: '122456.Microsoft.DotNet.Interactive.Http.HttpPort',\",       \"                paths:\",       \"            {\",       \"               " +
            " 'dotnet-interactive': `${root}resources`\",       \"                }\",       \"        }) || require;\",       \"\",       \"            window.dot" +
            "netInteractiveRequire = dotnetInteractiveRequire;\",       \"\",       \"            window.configureRequireFromExtension = function(extensionName, ex" +
            "tensionCacheBuster) {\",       \"                let paths = {};\",       \"                paths[extensionName] = `${root}extensions/${extensionName}" +
            "/resources/`;\",       \"                \",       \"                let internalRequire = require.config({\",       \"                    context: ex" +
            "tensionCacheBuster,\",       \"                    paths: paths,\",       \"                    urlArgs: `cacheBuster=${extensionCacheBuster}`\",     " +
            "  \"                    }) || require;\",       \"\",       \"                return internalRequire\",       \"            };\",       \"        \", " +
            "      \"            dotnetInteractiveRequire([\",       \"                    'dotnet-interactive/dotnet-interactive'\",       \"                ],\"," +
            "       \"                function (dotnet) {\",       \"                    dotnet.init(window);\",       \"                },\",       \"            " +
            "    function (error) {\",       \"                    console.log(error);\",       \"                }\",       \"            );\",       \"        })" +
            "\",       \"        .catch(error => {console.log(error);});\",       \"    }\",       \"\",       \"// ensure `require` is available globally\",      " +
            " \"if ((typeof(require) !==  typeof(Function)) || (typeof(require.config) !== typeof(Function))) {\",       \"    let require_script = document.create" +
            "Element('script');\",       \"    require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');\",    " +
            "   \"    require_script.setAttribute('type', 'text/javascript');\",       \"    \",       \"    \",       \"    require_script.onload = function() {\"" +
            ",       \"        loadDotnetInteractiveApi();\",       \"    };\",       \"\",       \"    document.getElementsByTagName('head')[0].appendChild(requir" +
            "e_script);\",       \"}\",       \"else {\",       \"    loadDotnetInteractiveApi();\",       \"}\",       \"\",       \"    </script>\",       \"</di" +
            "v>\"      ]     },     \"metadata\": {},     \"output_type\": \"display_data\"    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",  " +
            "   \"text\": [      \"Initialize.csx: Loading assemblies from C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    }   ], " +
            "  \"source\": [    \"// We need to load assemblies at the start in their own cell\",    \"#load \\\"./Initialize.csx\\\"\"   ]  },  {   \"cell_type\":" +
            " \"code\",   \"execution_count\": 2,   \"id\": \"0f8ca7c8\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T23:38:01." +
            "204969Z\",     \"iopub.status.busy\": \"2023-02-17T23:38:01.203966Z\",     \"iopub.status.idle\": \"2023-02-17T23:38:01.720374Z\",     \"shell.execute" +
            "_reply\": \"2023-02-17T23:38:01.718372Z\"    },    \"papermill\": {     \"duration\": 0.527908,     \"end_time\": \"2023-02-17T23:38:01.720374\",     " +
            "\"exception\": false,     \"start_time\": \"2023-02-17T23:38:01.192466\",     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"" +
            "languageId\": \"csharp\"    }   },   \"outputs\": [    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23" +
            ":38:01.491 TRACE:: Config.GetValue(): debug-mode - Using default value: False\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stre" +
            "am\",     \"text\": [      \"20230217 23:38:01.493 TRACE:: Config.Get(): Configuration key not found. Key: results-destination-folder - Using default " +
            "value: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.496 TRACE:: Config.Get(" +
            "): Configuration key not found. Key: plugin-directory - Using default value: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stre" +
            "am\",     \"text\": [      \"20230217 23:38:01.501 TRACE:: Config.Get(): Configuration key not found. Key: composer-dll-directory - Using default valu" +
            "e: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.502 TRACE:: Composer(): Loa" +
            "ding Assemblies from C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    },    {     \"name\": \"stdout\",     \"output_type\":" +
            " \"stream\",     \"text\": [      \"20230217 23:38:01.587 TRAC" +
            "E:: Config.Get(): Configuration key not found. Key: version-id - Using default value: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\"" +
            ": \"stream\",     \"text\": [      \"20230217 23:38:01.587 TRACE:: Config.Get(): Configuration key not found. Key: cache-location - Using default valu" +
            "e: ../../../Data/\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.588 TRACE:: E" +
            "ngine.Main(): LEAN ALGORITHMIC TRADING ENGINE v2.5.0.0 Mode: DEBUG (64bit) Host: ABREU\"     ]    },    {     \"name\": \"stdout\",     \"output_type\"" +
            ": \"stream\",     \"text\": [      \"20230217 23:38:01.589 TRACE:: Engine.Main(): Started 7:38 PM\"     ]    },    {     \"name\": \"stdout\",     \"o" +
            "utput_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.597 TRACE:: Config.Get(): Configuration key not found. Key: lean-manager-type - Usi" +
            "ng default value: LocalLeanManager\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38" +
            ":01.619 TRACE:: Config.Get(): Configuration key not found. Key: data-permission-manager - Using default value: DataPermissionManager\"     ]    },    " +
            "{     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.623 TRACE:: Config.Get(): Configuration key not " +
            "found. Key: results-destination-folder - Using default value: C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    },    {" +
            "     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20230217 23:38:01.657 TRACE:: Config.Get(): Configuration key not f" +
            "ound. Key: object-store-root - Using default value: ./storage\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text" +
            "\": [      \"20230217 23:38:01.668 TRACE:: Config.Get(): Configuration key not found. Key: results-destination-folder - Using default value: C:\\\\Use" +
            "rs\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    }   ],   \"source\": [    \"// Initialize Lean Engine.\",    \"#load \\\"./Qua" +
            "ntConnect.csx\\\"\",    \"\",    \"using System.Globalization;\",    \"using System.Linq;\",    \"using QuantConnect;\",    \"using QuantConnect.Data;" +
            "\",    \"using QuantConnect.Algorithm;\",    \"using QuantConnect.Research;\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 3,   \"id\"" +
            ": \"83870958\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2023-02-17T23:38:01.737374Z\",     \"iopub.status.busy\": \"2023-" +
            "02-17T23:38:01.735874Z\",     \"iopub.status.idle\": \"2023-02-17T23:38:02.793265Z\",     \"shell.execute_reply\": \"2023-02-17T23:38:02.792263Z\"    " +
            "},    \"papermill\": {     \"duration\": 1.067389,     \"end_time\": \"2023-02-17T23:38:02.793764\",     \"exception\": false,     \"start_time\": \"2" +
            "023-02-17T23:38:01.726375\",     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"output" +
            "s\": [],   \"source\": [    \"class CustomDataType : DynamicData\",    \"{\",    \"    public decimal Open;\",    \"    public decimal High;\",    \" " +
            "   public decimal Low;\",    \"    public decimal Close;\",    \"\",    \"    public override SubscriptionDataSource GetSource(SubscriptionDataConfig " +
            "config, DateTime date, bool isLiveMode)\",    \"    {\",    \"        var source = \\\"https://www.dl.dropboxusercontent.com/s/d83xvd7mm9fzpk0/path_to" +
            "_my_csv_data.csv?dl=0\\\";\",    \"        return new SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);\",    \"    }\",    \"\"" +
            ",    \"    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)\",    \"    {\",    \"        i" +
            "f (string.IsNullOrWhiteSpace(line.Trim()))\",    \"        {\",    \"            return null;\",    \"        }\",    \"\",    \"        try\",    \" " +
            "       {\",    \"            var csv = line.Split(\\\",\\\");\",    \"            var data = new CustomDataType()\",    \"            {\",    \"      " +
            "          Symbol = config.Symbol,\",    \"                Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureInfo.InvariantCulture).AddHours(20)" +
            ",\",    \"                Value = csv[4].ToDecimal(),\",    \"                Open = csv[1].ToDecimal(),\",    \"                High = csv[2].ToDecim" +
            "al(),\",    \"                Low = csv[3].ToDecimal(),\",    \"                Close = csv[4].ToDecimal()\",    \"            };\",    \"\",    \"   " +
            "         return data;\",    \"        }\",    \"        catch\",    \"        {\",    \"            return null;\",    \"        }\",    \"    }\",   " +
            " \"}\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"ed9ea0a3\",   \"metadata\": {    \"execution\": {     \"iopub.execu" +
            "te_input\": \"2023-02-17T23:38:02.815268Z\",     \"iopub.status.busy\": \"2023-02-17T23:38:02.814265Z\",     \"iopub.status.idle\": \"2023-02-17T23:38" +
            ":10.436355Z\",     \"shell.execute_reply\": \"2023-02-17T23:38:10.435324Z\"    },    \"papermill\": {     \"duration\": 7.633591,     \"end_time\": \"" +
            "2023-02-17T23:38:10.436355\",     \"exception\": false,     \"start_time\": \"2023-02-17T23:38:02.802764\",     \"status\": \"completed\"    },    \"t" +
            "ags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [    {     \"name\": \"stdout\",     \"output_type\": \"stream\", " +
            "    \"text\": [      \"PythonEngine.Initialize(): clr GetManifestResourceStream...\"     ]    }   ],   \"source\": [    \"var qb = new QuantBook();\"," +
            "    \"var symbol = qb.AddData<CustomDataType>(\\\"CustomDataType\\\", Resolution.Hour).Symbol;\",    \"\",    \"var start = new DateTime(2017, 8, 20);" +
            "\",    \"var end = start.AddHours(48);\",    \"var history = qb.History<CustomDataType>(symbol, start, end, Resolution.Hour).ToList();\",    \"\",    " +
            "\"if (history.Count == 0)\",    \"{\",    \"    throw new Exception(\\\"No history data returned\\\");\",    \"}\"   ]  } ], \"metadata\": {  \"kernel" +
            "spec\": {   \"display_name\": \".NET (C#)\",   \"language\": \"C#\",   \"name\": \".net-csharp\"  },  \"language_info\": {   \"file_extension\": \".cs" +
            "\",   \"mimetype\": \"text/x-csharp\",   \"name\": \"C#\",   \"pygments_lexer\": \"csharp\",   \"version\": \"10.0\"  },  \"papermill\": {   \"default" +
            "_parameters\": {},   \"duration\": 25.341314,   \"end_time\": \"2023-02-17T23:38:13.053129\",   \"environment_variables\": {},   \"exception\": null, " +
            "  \"input_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomDat" +
            "aTypeHistoryResearchCSharp.ipynb\",   \"output_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\Regressi" +
            "onTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchCSharp-output.ipynb\",   \"parameters\": {},   \"start_time\": \"2023-02-17T23:37:47.711815\"" +
            ",   \"version\": \"2.4.0\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
