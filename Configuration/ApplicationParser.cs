using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line application parser
    /// </summary>
    public static class ApplicationParser
    {
        /// <summary>
        /// This function will parse args based on options and will show application name, version, help
        /// </summary>
        /// <param name="applicationName">The application name to be shown</param>
        /// <param name="applicationDescription">The application description to be shown</param>
        /// <param name="applicationHelpText">The application help text</param>
        /// <param name="args">The command line arguments</param>
        /// <param name="options">The applications command line available options</param>
        /// <param name="noArgsShowHelp">To show help when no command line arguments were provided</param>
        /// <returns>The user provided options. Key is option name</returns>
        public static Dictionary<string, object> Parse(string applicationName, string applicationDescription, string applicationHelpText,
                                                       string[] args, List<CommandLineOption> options, bool noArgsShowHelp = false)
        {
            var application = new CommandLineApplication
            {
                Name = applicationName,
                Description = applicationDescription,
                ExtendedHelpText = applicationHelpText
            };

            application.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            application.VersionOption("-v|-V|--version",
                () =>
                    $"Version {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

            var optionsObject = new Dictionary<string, object>();

            var listOfOptions = new List<CommandOption>();

            foreach (var option in options)
            {
                listOfOptions.Add(application.Option($"--{option.Name}", option.Description, option.Type));
            }

            application.OnExecute(() =>
            {
                foreach (var commandOption in listOfOptions.Where(option => option.HasValue()))
                {
                    var optionKey = commandOption.Template.Replace("--", "");
                    var matchingOption = options.Find(o => o.Name == optionKey);
                    switch (matchingOption.Type)
                    {
                        // Booleans, string and numbers
                        case CommandOptionType.NoValue:
                        case CommandOptionType.SingleValue:
                            optionsObject[optionKey] = commandOption.Value();
                            break;

                        // Parsing nested objects
                        case CommandOptionType.MultipleValue:
                            var keyValuePairs = commandOption.Value().Split(',');
                            var subDictionary = new Dictionary<string, string>();
                            foreach (var keyValuePair in keyValuePairs)
                            {
                                var subKeys = keyValuePair.Split(':');
                                subDictionary[subKeys[0]] = subKeys.Length > 1 ? subKeys[1] : "";
                            }

                            optionsObject[optionKey] = subDictionary;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return 0;
            });

            application.Execute(args);
            if (noArgsShowHelp && args.Length == 0)
            {
                application.ShowHelp();
            }
            return optionsObject;
        }
    }
}
