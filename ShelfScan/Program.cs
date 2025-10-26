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
using System.Reflection;
using System.Text.RegularExpressions;

namespace ShelfScan
{
    internal sealed class Program
    {
        /// <summary>Folder to scan for content</summary>
        public static string FolderToScan { get; set; } = "";

        /// <summary>If set to true then files that pass testing will be shown</summary>
        public static bool ShowPasses { get; set; }

        /// <summary>If set to true, then perform self-test mode (for development only)</summary>
        public static bool SelfTest { get; set; }

        /// <summary>Type of media being scanned: "movie" or "tv"</summary>
        public static string MediaType { get; set; } = "";

        /// <summary>Version of this application.</summary>
        public static Version AppVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

        static void Main(string[] args)
        {
            // Parse command line arguments
            CommandLineParser.ParseArguments(args);

            // Show the header with GPL notice
            ShowHeader(true);

            if (SelfTest)
            {
                SelfTestRunner.RunSelfTests();
                Environment.Exit(0);
            }

            // Get all .mkv, .mp4 and .avi files recursively. In the future we may add music.
            Console.WriteLine();
            Console.WriteLine($"Searching for content in {FolderToScan}...");

            List<string> files = [];
            try
            {
                AddMediaFiles(FolderToScan, files, [".mkv", ".mp4", ".avi"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning files: {ex.Message}");
                Environment.Exit(-1);
            }

            // If no override type defined, then try to auto-detect based on folder structure
            if (string.IsNullOrEmpty(MediaType))
                MediaType = GuessMediaType(files);

            // Now process each file
            int total = files.Count;
            int validCount = 0;
            int invalidCount = 0;
            bool isValid;

            Console.WriteLine();
            Console.WriteLine($"========= BEGIN {MediaType.ToUpper(CultureInfo.CurrentCulture)} REPORT =========");

            // Show some disclaimer text

            Console.WriteLine();
            Console.WriteLine("Beta notice:");
            Console.WriteLine();
            Console.WriteLine($"This tool is an early beta (v{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Revision}). There may be mistakes in the logic.");
            Console.WriteLine("If you encounter any issues or incorrect results, please report them on GitHub.");
            Console.WriteLine();
            Console.WriteLine("Strict file format checking:");
            Console.WriteLine();
            Console.WriteLine("File format checks are very strict. A file marked as invalid in this report");
            Console.WriteLine("does not necessarily mean there is a problem with it in Plex.");
            Console.WriteLine();
            Console.WriteLine("Resources:");
            Console.WriteLine();
            Console.WriteLine("- https://support.plex.tv/articles/naming-and-organizing-your-tv-show-files/");
            Console.WriteLine("- https://support.plex.tv/articles/naming-and-organizing-your-movie-files/");
            Console.WriteLine();
            Console.WriteLine("Scan results:");

            // Loop through each file and verify
            foreach (var file in files)
            {
                if (MediaType == "movie")
                    isValid = PlexMovieVerifier.VerifyMovies(file, FolderToScan);
                else
                    isValid = PlexShowVerifier.VerifyShow(file);

                // Tally results
                if (isValid)
                {
                    validCount++;
                    if (ShowPasses)
                        Console.WriteLine($"\n{file}\n  PASSED verification.");
                }
                else
                    invalidCount++;
            }

            // Show summary
            Console.WriteLine();
            Console.WriteLine($"Summary:");
            Console.WriteLine();
            Console.WriteLine($"Valid files:          {validCount,6:N0}");
            Console.WriteLine($"Invalid files:        {invalidCount,6:N0}");
            Console.WriteLine($"Total files checked:  {total,6:N0}");

            float pc = (float)(validCount * 100.0) / (float)total;
            string niceMessage = pc switch
            {
                >= 100.0f => "(perfect score!)",
                >= 95.0f => "(excellent!)",
                >= 90.0f => "(great job!)",
                >= 85.0f => "(good effort)",
                _ => ""
            };
            Console.WriteLine($"Correctness:          {pc,6:N2}% {niceMessage}");

            Console.WriteLine();
            Console.WriteLine($"========== END {MediaType.ToUpper(CultureInfo.CurrentCulture)} REPORT ==========");
            Environment.Exit(0);
        }

        /// <summary>
        /// Simple heuristic to guess if the folder contains movies or TV shows. The
        /// user can ovverride this with a command line argument.
        /// </summary>
        /// <param name="folder">Path to content</param>
        /// <param name="files">List</param>
        /// <returns></returns>
        private static string GuessMediaType(List<string> files)
        {
            // Look for SxxExx patterns in filenames to indicate TV shows
            Regex tvPattern = new(@"S\d{1,2}E\d{1,2}", RegexOptions.IgnoreCase);

            foreach (var file in files)
                if (tvPattern.IsMatch(file))
                    return "tv";

            // Default to movie
            return "movie";
        }

        /// <summary>
        /// Display the application header. If showGPL is true, also show the GPL license notice.
        /// </summary>
        /// <param name="showGPL"></param>
        public static void ShowHeader(bool showGPL = false)
        {
            Console.WriteLine($"ShelfScan v{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Revision} - Scans a media library for Plex naming compliance.");
            if (showGPL)
            {
                Console.WriteLine("http://github.com/mrsilver76/shelfscan/");
                Console.WriteLine();
                Console.WriteLine("This program is free software: you can redistribute it and/or modify");
                Console.WriteLine("it under the terms of the GNU General Public License as published by");
                Console.WriteLine("the Free Software Foundation, either version 2 of the License, or");
                Console.WriteLine("at your option) any later version.");
            }
        }

        /// <summary>
        /// Add all media files in the specified folder and its subfolders to the list. Ignores
        /// any "Plex Versions" folders.
        /// </summary>
        /// <param name="folder">Root folder</param>
        /// <param name="files">List to add files to</param>
        /// <param name="extensions">Extensions to include</param>
        public static void AddMediaFiles(string folder, List<string> files, IEnumerable<string> extensions)
        {
            foreach (var dir in Directory.EnumerateDirectories(folder))
            {
                // Skip Plex Versions and all subfolders
                if (Path.GetFileName(dir).Equals("Plex Versions", StringComparison.OrdinalIgnoreCase))
                    continue;

                AddMediaFiles(dir, files, extensions);
            }

            // Collect files in this folder
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                var ext = Path.GetExtension(file);
                if (!string.IsNullOrEmpty(ext) && extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    files.Add(file);
            }
        }
    }
}
