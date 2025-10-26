/*
 * ShelfScan - Scans a media library for Plex naming compliance.
 * http://github.com/mrsilver76/shelfscan/
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *  
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this Options.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Globalization;

namespace ShelfScan
{
    /// <summary>
    /// Handle command line parsing for ShelfScan.
    /// </summary>
    internal sealed class CommandLineParser
    {
        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="args"></param>
        public static void ParseArguments(string[] args)
        {
            if (args.Length == 0)
                DisplayUsage();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower(CultureInfo.InvariantCulture);

                if (arg == "/?" || arg == "--help" || arg == "-h")
                    DisplayUsage();

                else if (arg == "/t" || arg == "-t" || arg == "--type")
                {
                    if (i + 1 < args.Length)
                    {
                        string key = args[i + 1].ToLower(CultureInfo.InvariantCulture);
                        if (key.Length > 2) key = key[..2];  // Normalize to first 2 letters
                        switch (key)
                        {
                            case "mo":  // movie(s)
                            case "fi":  // film(s)
                                Program.MediaType = "movie";
                                break;
                            case "tv":  // tv
                            case "sh":  // show(s)
                            case "te":  // television
                                Program.MediaType = "tv";
                                break;
                            default:    // unknown
                                DisplayUsage($"Unknown media type override '{args[i + 1]}'. Use 'movie' or 'tv'.");
                                break;  // Not required, but keeps analyzer happy
                        }
                        i++;  // Skip next argument as it has been processed
                    }
                    else
                    {
                        DisplayUsage("Missing media type after type switch.");
                    }
                }

                else if (arg == "/p" || arg == "-p" || arg == "--pass")
                    Program.ShowPasses = true;

                else if (arg == "/s" || arg == "-s" || arg == "--selftest" || arg == "--self-test")
                    Program.SelfTest = true;

                else if (arg.StartsWith('/') || arg.StartsWith('-'))
                    DisplayUsage($"Unknown option '{args[i]}'.");
                else
                {
                    // Assume it's a folder path
                    if (!Directory.Exists(args[i]))
                        DisplayUsage($"Folder '{args[i]}' does not exist.");
                    Program.FolderToScan = args[i];
                }
            }
        }

        /// <summary>
        /// Display usage information and exit. If an error message is provided, show that too
        /// and exit with error code -1.
        /// </summary>
        /// <param name="errorMessage"></param>
        private static void DisplayUsage(string errorMessage = "")
        {
            Console.WriteLine("Usage: ShelfScan <folder> [options]");
            if (errorMessage == "")
                Program.ShowHeader(true);
            else
                Program.ShowHeader();
            Console.WriteLine();
            Console.WriteLine("Mandatory:");
            Console.WriteLine("  <folder>             Folder of content to scan.");
            Console.WriteLine();
            Console.WriteLine("Optional:");
            Console.WriteLine("  -t, --type <type>    Override auto-detection to content type.");
            Console.WriteLine("                        Type can be 'movie' or 'tv'");
            Console.WriteLine("  -p, --pass           Show files that pass verification.");
            Console.WriteLine("  -s, --selftest       Run self-tests (for development only).");
            Console.WriteLine("                        Folder should point to the base test folder.");
            Console.WriteLine("  /?, -h, --help       Show this help message.");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }

            Environment.Exit(0);
        }

    }
}
