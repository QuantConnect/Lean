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
    public class BasicTemplateResearchCSharp : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"a4652bd4\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.005036,     \"end_t" +
            "ime\": \"2022-03-02T20:44:55.508287\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:44:55.503251\",     \"status\": \"completed\"    " +
            "},    \"tags\": []   },   \"source\": [    \"![QuantConnect Logo](https://cdn.quantconnect.com/web/i/qc_notebook_logo_rev0.png)\",    \"## Welcome to " +
            "The QuantConnect Research Page\",    \"#### Refer to this page for documentation https://www.quantconnect.com/docs/research/overview#\",    \"#### Con" +
            "tribute to this template file https://github.com/QuantConnect/Lean/blob/master/Research/BasicQuantBookTemplate.ipynb\"   ]  },  {   \"cell_type\": \"m" +
            "arkdown\",   \"id\": \"acb5aec0\",   \"metadata\": {    \"papermill\": {     \"duration\": 0.002024,     \"end_time\": \"2022-03-02T20:44:55.513310\"," +
            "     \"exception\": false,     \"start_time\": \"2022-03-02T20:44:55.511286\",     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": " +
            "[    \"## QuantBook Basics\",    \"\",    \"### Start QuantBook\",    \"- Add the references and imports\",    \"- Create a QuantBook instance\"   ]  " +
            "},  {   \"cell_type\": \"code\",   \"execution_count\": 1,   \"id\": \"39de525f\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": " +
            "\"2022-03-02T20:44:55.529252Z\",     \"iopub.status.busy\": \"2022-03-02T20:44:55.522252Z\",     \"iopub.status.idle\": \"2022-03-02T20:44:57.058250Z\"" +
            ",     \"shell.execute_reply\": \"2022-03-02T20:44:57.052865Z\"    },    \"papermill\": {     \"duration\": 1.542006,     \"end_time\": \"2022-03-02T20" +
            ":44:57.058250\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:44:55.516244\",     \"status\": \"completed\"    },    \"tags\": []   }" +
            ",   \"outputs\": [    {     \"data\": {      \"text/html\": [       \"\",       \"<div>\",       \"    <div id='dotnet-interactive-this-cell-4972.Micr" +
            "osoft.DotNet.Interactive.Http.HttpPort' style='display: none'>\",       \"        The below script needs to be able to find the current output cell; t" +
            "his is an easy method to get it.\",       \"    </div>\",       \"    <script type='text/javascript'>\",       \"async function probeAddresses(probing" +
            "Addresses) {\",       \"    function timeout(ms, promise) {\",       \"        return new Promise(function (resolve, reject) {\",       \"            " +
            "setTimeout(function () {\",       \"                reject(new Error('timeout'))\",       \"            }, ms)\",       \"            promise.then(res" +
            "olve, reject)\",       \"        })\",       \"    }\",       \"\",       \"    if (Array.isArray(probingAddresses)) {\",       \"        for (let i =" +
            " 0; i < probingAddresses.length; i++) {\",       \"\",       \"            let rootUrl = probingAddresses[i];\",       \"\",       \"            if (!" +
            "rootUrl.endsWith('/')) {\",       \"                rootUrl = `${rootUrl}/`;\",       \"            }\",       \"\",       \"            try {\",     " +
            "  \"                let response = await timeout(1000, fetch(`${rootUrl}discovery`, {\",       \"                    method: 'POST',\",       \"      " +
            "              cache: 'no-cache',\",       \"                    mode: 'cors',\",       \"                    timeout: 1000,\",       \"               " +
            "     headers: {\",       \"                        'Content-Type': 'text/plain'\",       \"                    },\",       \"                    body:" +
            " probingAddresses[i]\",       \"                }));\",       \"\",       \"                if (response.status == 200) {\",       \"                 " +
            "   return rootUrl;\",       \"                }\",       \"            }\",       \"            catch (e) { }\",       \"        }\",       \"    }\"," +
            "       \"}\",       \"\",       \"function loadDotnetInteractiveApi() {\",       \"    probeAddresses([\\\"http://192.168.29.151:1000/\\\", \\\"http:/" +
            "/127.0.0.1:1000/\\\"])\",       \"        .then((root) => {\",       \"        // use probing to find host url and api resources\",       \"        //" +
            " load interactive helpers and language services\",       \"        let dotnetInteractiveRequire = require.config({\",       \"        context: '4972.M" +
            "icrosoft.DotNet.Interactive.Http.HttpPort',\",       \"                paths:\",       \"            {\",       \"                'dotnet-interactive'" +
            ": `${root}resources`\",       \"                }\",       \"        }) || require;\",       \"\",       \"            window.dotnetInteractiveRequire" +
            " = dotnetInteractiveRequire;\",       \"\",       \"            window.configureRequireFromExtension = function(extensionName, extensionCacheBuster) {" +
            "\",       \"                let paths = {};\",       \"                paths[extensionName] = `${root}extensions/${extensionName}/resources/`;\",     " +
            "  \"                \",       \"                let internalRequire = require.config({\",       \"                    context: extensionCacheBuster,\"" +
            ",       \"                    paths: paths,\",       \"                    urlArgs: `cacheBuster=${extensionCacheBuster}`\",       \"                 " +
            "   }) || require;\",       \"\",       \"                return internalRequire\",       \"            };\",       \"        \",       \"            d" +
            "otnetInteractiveRequire([\",       \"                    'dotnet-interactive/dotnet-interactive'\",       \"                ],\",       \"            " +
            "    function (dotnet) {\",       \"                    dotnet.init(window);\",       \"                },\",       \"                function (error) " +
            "{\",       \"                    console.log(error);\",       \"                }\",       \"            );\",       \"        })\",       \"        ." +
            "catch(error => {console.log(error);});\",       \"    }\",       \"\",       \"// ensure `require` is available globally\",       \"if ((typeof(requir" +
            "e) !==  typeof(Function)) || (typeof(require.config) !== typeof(Function))) {\",       \"    let require_script = document.createElement('script');\"," +
            "       \"    require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');\",       \"    require_scri" +
            "pt.setAttribute('type', 'text/javascript');\",       \"    \",       \"    \",       \"    require_script.onload = function() {\",       \"        loa" +
            "dDotnetInteractiveApi();\",       \"    };\",       \"\",       \"    document.getElementsByTagName('head')[0].appendChild(require_script);\",       \"" +
            "}\",       \"else {\",       \"    loadDotnetInteractiveApi();\",       \"}\",       \"\",       \"    </script>\",       \"</div>\"      ]     },    " +
            " \"metadata\": {},     \"output_type\": \"display_data\"    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"" +
            "Initialize.csx: Loading assemblies from D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    }   ],   \"source\": [    \"// QuantBook C# Res" +
            "earch Environment\",    \"// For more information see https://www.quantconnect.com/docs/research/overview\",    \"#load \\\"./Initialize.csx\\\"\"   ]" +
            "  },  {   \"cell_type\": \"code\",   \"execution_count\": 2,   \"id\": \"ceb0015a\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\"" +
            ": \"2022-03-02T20:44:57.066259Z\",     \"iopub.status.busy\": \"2022-03-02T20:44:57.066259Z\",     \"iopub.status.idle\": \"2022-03-02T20:44:57.188021" +
            "Z\",     \"shell.execute_reply\": \"2022-03-02T20:44:57.187022Z\"    },    \"papermill\": {     \"duration\": 0.126763,     \"end_time\": \"2022-03-02" +
            "T20:44:57.188021\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:44:57.061258\",     \"status\": \"completed\"    },    \"tags\": [] " +
            "  },   \"outputs\": [    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.140 TRACE:: Config.GetV" +
            "alue(): debug-mode - Using default value: False\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"2" +
            "0220302 20:44:57.141 TRACE:: Config.Get(): Configuration key not found. Key: results-destination-folder - Using default value: \"     ]    },    {    " +
            " \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.141 TRACE:: Config.Get(): Configuration key not found" +
            ". Key: plugin-directory - Using default value: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"2" +
            "0220302 20:44:57.142 TRACE:: Config.Get(): Configuration key not found. Key: composer-dll-directory - Using default value: \"     ]    },    {     \"n" +
            "ame\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.142 TRACE:: Composer(): Loading Assemblies from D:\\\\qua" +
            "ntconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    },    {     \"" +
            "name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.168 TRACE:: Config.Get(): Configuration key not found. K" +
            "ey: version-id - Using default value: \"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 2" +
            "0:44:57.169 TRACE:: Config.Get(): Configuration key not found. Key: cache-location - Using default value: ../../../Data/\"     ]    },    {     \"name" +
            "\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.169 TRACE:: Engine.Main(): LEAN ALGORITHMIC TRADING ENGINE v" +
            "2.5.0.0 Mode: DEBUG (64bit) Host: P3561-70DPBL3\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"2" +
            "0220302 20:44:57.171 TRACE:: Engine.Main(): Started 2:14 AM\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\"" +
            ": [      \"20220302 20:44:57.173 TRACE:: Config.Get(): Configuration key not found. Key: lean-manager-type - Using default value: LocalLeanManager\"  " +
            "   ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.179 TRACE:: Config.Get(): Configur" +
            "ation key not found. Key: data-permission-manager - Using default value: DataPermissionManager\"     ]    },    {     \"name\": \"stdout\",     \"outp" +
            "ut_type\": \"stream\",     \"text\": [      \"20220302 20:44:57.180 TRACE:: Config.Get(): Configuration key not found. Key: results-destination-folder" +
            " - Using default value: D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream" +
            "\",     \"text\": [      \"20220302 20:44:57.185 TRACE:: Config.Get(): Configuration key not found. Key: object-store-root - Using default value: ./st" +
            "orage\"     ]    }   ],   \"source\": [    \"#load \\\"./QuantConnect.csx\\\"\",    \"\",    \"using QuantConnect;\",    \"using QuantConnect.Data;\"," +
            "    \"using QuantConnect.Algorithm;\",    \"using QuantConnect.Research;\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 3,   \"id\": \"" +
            "e81ef855\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:44:57.200095Z\",     \"iopub.status.busy\": \"2022-03-0" +
            "2T20:44:57.199026Z\",     \"iopub.status.idle\": \"2022-03-02T20:44:58.393456Z\",     \"shell.execute_reply\": \"2022-03-02T20:44:58.392457Z\"    },  " +
            "  \"papermill\": {     \"duration\": 1.200436,     \"end_time\": \"2022-03-02T20:44:58.393456\",     \"exception\": false,     \"start_time\": \"2022-" +
            "03-02T20:44:57.193020\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [    {     \"name\": \"stdout\",     \"output_type\":" +
            " \"stream\",     \"text\": [      \"PythonEngine.Initialize(): Runtime.Initialize()...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\"" +
            ": \"stream\",     \"text\": [      \"Runtime.Initialize(): Py_Initialize...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream" +
            "\",     \"text\": [      \"Runtime.Initialize(): PyEval_InitThreads...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",  " +
            "   \"text\": [      \"Runtime.Initialize(): Initialize types...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"te" +
            "xt\": [      \"Runtime.Initialize(): Initialize types end.\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\":" +
            " [      \"Runtime.Initialize(): AssemblyManager.Initialize()...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"te" +
            "xt\": [      \"Runtime.Initialize(): AssemblyManager.UpdatePath()...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",    " +
            " \"text\": [      \"PythonEngine.Initialize(): GetCLRModule()...\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"t" +
            "ext\": [      \"PythonEngine.Initialize(): clr GetManifestResourceStream...\"     ]    }   ],   \"source\": [    \"var qb = new QuantBook();\",    \"v" +
            "ar spy = qb.AddEquity(\\\"SPY\\\");\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": 4,   \"id\": \"06cd5f47\",   \"metadata\": {    \"e" +
            "xecution\": {     \"iopub.execute_input\": \"2022-03-02T20:44:58.408454Z\",     \"iopub.status.busy\": \"2022-03-02T20:44:58.408454Z\",     \"iopub.st" +
            "atus.idle\": \"2022-03-02T20:44:58.449457Z\",     \"shell.execute_reply\": \"2022-03-02T20:44:58.449457Z\"    },    \"papermill\": {     \"duration\":" +
            " 0.048992,     \"end_time\": \"2022-03-02T20:44:58.449457\",     \"exception\": false,     \"start_time\": \"2022-03-02T20:44:58.400465\",     \"statu" +
            "s\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": [    \"var startDate = new DateTime(2021,1,1);\",    \"var endDate = ne" +
            "w DateTime(2021,12,31);\",    \"var history = qb.History(qb.Securities.Keys, startDate, endDate, Resolution.Daily);\"   ]  },  {   \"cell_type\": \"co" +
            "de\",   \"execution_count\": 5,   \"id\": \"37b045b2\",   \"metadata\": {    \"execution\": {     \"iopub.execute_input\": \"2022-03-02T20:44:58.46348" +
            "5Z\",     \"iopub.status.busy\": \"2022-03-02T20:44:58.463485Z\",     \"iopub.status.idle\": \"2022-03-02T20:44:58.611572Z\",     \"shell.execute_repl" +
            "y\": \"2022-03-02T20:44:58.611572Z\"    },    \"papermill\": {     \"duration\": 0.156117,     \"end_time\": \"2022-03-02T20:44:58.611572\",     \"exc" +
            "eption\": false,     \"start_time\": \"2022-03-02T20:44:58.455455\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [    {   " +
            "  \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"SPY: O: 374.075 H: 374.2245 L: 363.6392 C: 367.5863 V: 94685703\"     " +
            "]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"SPY: O: 366.8487 H: 371.2642 L: 366.8487 C: 370.118 V: " +
            "54790079\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"SPY: O: 368.4634 H: 375.7495 L: 367.9152" +
            " C: 372.3307 V: 93254510\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"SPY: O: 374.9521 H: 378." +
            "65 L: 374.693 C: 377.8626 V: 62474792\"     ]    },    {     \"name\": \"stdout\",     \"output_type\": \"stream\",     \"text\": [      \"SPY: O: 379" +
            ".3876 H: 380.2448 L: 375.8791 C: 380.0156 V: 63370827\"     ]    }   ],   \"source\": [    \"foreach(var slice in history.Take(5)) {\",    \"    Conso" +
            "le.WriteLine(slice.Bars[spy.Symbol].ToString());\",    \"}\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"bd2ab8d7\"" +
            ",   \"metadata\": {    \"papermill\": {     \"duration\": 0.006983,     \"end_time\": \"2022-03-02T20:44:58.626572\",     \"exception\": false,     \"" +
            "start_time\": \"2022-03-02T20:44:58.619589\",     \"status\": \"completed\"    },    \"tags\": []   },   \"outputs\": [],   \"source\": []  } ], \"met" +
            "adata\": {  \"kernelspec\": {   \"display_name\": \".NET (C#)\",   \"language\": \"C#\",   \"name\": \".net-csharp\"  },  \"language_info\": {   \"fil" +
            "e_extension\": \".cs\",   \"mimetype\": \"text/x-csharp\",   \"name\": \"C#\",   \"pygments_lexer\": \"csharp\",   \"version\": \"9.0\"  },  \"papermi" +
            "ll\": {   \"default_parameters\": {},   \"duration\": 7.291395,   \"end_time\": \"2022-03-02T20:45:01.252292\",   \"environment_variables\": {},   \"e" +
            "xception\": null,   \"input_path\": \"D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\BasicTemplateResearchCSharp.ipynb\",   \"output_path\": \"" +
            "D:\\\\quantconnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\BasicTemplateResearchCSharp-output.ipynb\",   \"parameters\": {},   \"start_time\": \"2022-03-0" +
            "2T20:44:53.960897\",   \"version\": \"2.3.4\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
