using McMaster.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Auxiliary class to keep information about a specific command line option
    /// </summary>
    public class CommandLineOption
    {
        /// <summary>
        /// Command line option type
        /// </summary>
        public CommandOptionType Type { get; }

        /// <summary>
        /// Command line option description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Command line option name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Command line option contructor
        /// </summary>
        public CommandLineOption(string name, CommandOptionType type, string description = "")
        {
            Type = type;
            Description = description;
            Name = name;
        }
    }
}
