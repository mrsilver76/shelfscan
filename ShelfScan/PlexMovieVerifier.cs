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

using System.Text.RegularExpressions;

namespace ShelfScan
{
    public sealed class PlexMovieVerifier
    {
        // Extras suffixes for inline local extras
        private static readonly string[] InlineExtraSuffixes =
        [
            "-behindthescenes", "-deleted", "-featurette", "-interview",
            "-scene", "-short", "-trailer", "-other"
        ];

        // Valid subdirectories for extras
        private static readonly string[] ExtraSubdirectories =
        [
            "Behind The Scenes", "Deleted Scenes", "Featurettes", "Interviews",
            "Scenes", "Shorts", "Trailers", "Other"
        ];

        /// <summary>
        /// Verify a movie file against Plex naming rules.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// 
        public static bool VerifyMovies(string filePath, string rootFolder)
        {
            string fileName = Path.GetFileName(filePath);
            string folderPath = Path.GetDirectoryName(filePath)!;
            string parentFolder = Path.GetFileName(folderPath);

            string cleanFolderName = StripBrackets(parentFolder);

            // 1. Check for extras in subdirectory
            if (Array.Exists(ExtraSubdirectories, s => s.Equals(cleanFolderName, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 2. Check for inline extras in filename
            foreach (var extra in InlineExtraSuffixes)
                if (fileName.EndsWith(extra + Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase))
                    return true;

            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);

            // 3. Validate { } blocks (ignore {edition-*} in filename, enforce others only if not in root)
            bool inRoot = string.Equals(Path.GetFullPath(folderPath).TrimEnd('\\'),
                                        Path.GetFullPath(rootFolder).TrimEnd('\\'),
                                        StringComparison.OrdinalIgnoreCase);

            // Tests for when not in root folder
            if (!inRoot)
            {
                var fileBraceMatches = Regex.Matches(fileNameNoExt, @"\{.*?\}");
                foreach (Match match in fileBraceMatches)
                {
                    string content = match.Value;
                    if (content.StartsWith("{edition-", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!parentFolder.Contains(content, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"\n{filePath}\n  Block '{content}' in filename must also exist in folder");
                        return false;
                    }
                }

                var folderBraceMatches = Regex.Matches(parentFolder, @"\{.*?\}");
                foreach (Match match in folderBraceMatches)
                {
                    string content = match.Value;
                    if (content.StartsWith("{edition-", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!fileNameNoExt.Contains(content, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"\n{filePath}\n  Block '{content}' in folder must also exist in filename");
                        return false;
                    }
                }
            }

            // 4. Strip { } and [ ] for core validation only
            string coreName = Regex.Replace(fileNameNoExt, @"(\{.*?\}|\[.*?\])", "").Trim();

            // Step 5: core name validation
            // Capture title, year, and anything after the year
            string corePattern = @"^(?<title>.+?) ?\((?<year>\d{4})\)(?<after>.*)$";
            Regex coreRegex = new(corePattern, RegexOptions.IgnoreCase);
            var coreMatch = coreRegex.Match(coreName);

            if (!coreMatch.Success)
            {
                Console.WriteLine($"\n{filePath}\n  Invalid naming format. Expected 'Movie Name (YYYY){{optional split}}'");
                return false;
            }

            string titlePart = coreMatch.Groups["title"].Value.Trim();
            string yearStr = coreMatch.Groups["year"].Value.Trim();
            string afterYear = coreMatch.Groups["after"].Value.Trim();

            // Step 5a: More specific diagnostics for title/year
            if (string.IsNullOrEmpty(titlePart))
            {
                Console.WriteLine($"\n{filePath}\n  Missing or malformed title before year.");
                return false;
            }
            else if (!Regex.IsMatch(yearStr, @"^\d{4}$"))
            {
                Console.WriteLine($"\n{filePath}\n  Year must be 4 digits. Found '{yearStr}'.");
                return false;
            }

            // Step 5b: validate split / additional text
            if (!string.IsNullOrEmpty(afterYear))
            {
                if (!afterYear.StartsWith("-", StringComparison.CurrentCulture))
                {
                    // Extra text without dash
                    Console.WriteLine($"\n{filePath}\n  Additional text after year is technically invalid: '{afterYear}'");
                    return false;
                }
                else
                {
                    // Dash exists, must match split/part pattern
                    string splitPattern = @"^- (cd\d+|disc\d+|disk\d+|dvd\d+|part\d+|pt\d+)$";
                    if (!Regex.IsMatch(afterYear, splitPattern, RegexOptions.IgnoreCase))
                    {
                        Console.WriteLine($"\n{filePath}\n  Split/part tag format incorrect. Expected ' - pt1', ' - CD2', etc.");
                        return false;
                    }
                }
            }

            // 6. Validate year
            if (!int.TryParse(yearStr, out int year) || year < 1900 || year > DateTime.Now.Year + 1)
            {
                Console.WriteLine($"\n{filePath}\n  Invalid year '{yearStr}'. Must be between 1900 and {DateTime.Now.Year + 1}");
                return false;
            }

            // 7. Validate folder consistency if not in root
            if (!inRoot)
            {
                // Remove [ ] and {edition-*} from both folder and filename for core comparison
                string coreFolderName = Regex.Replace(parentFolder, @"(\[.*?\]|\{edition-.*?\})", "", RegexOptions.IgnoreCase).Trim();
                string fileCoreForFolder = Regex.Replace(fileNameNoExt, @"(\[.*?\]|\{edition-.*?\})", "", RegexOptions.IgnoreCase).Trim();

                // Remove optional split from filename
                fileCoreForFolder = Regex.Replace(fileCoreForFolder,
                                  @" - (cd\d+|disc\d+|disk\d+|dvd\d+|part\d+|pt\d+)$",
                                  "", RegexOptions.IgnoreCase).Trim();

                if (!fileCoreForFolder.Equals(coreFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"\n{filePath}\n  Folder name '{parentFolder}' does not match the filename base (ignoring optional split, {{edition-*}}, and [tags]).");
                    return false;
                }
            }

            // 8. Verify if the tags in square brackets aren't supposed to be in curly braces
            if (!PlexCommonVerifier.ValidateSquareBracketTags(filePath))
                return false;

            return true;
        }

        /// <summary>
        /// Removes [bracketed] sections from a name. These are ignored in Plex naming.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string StripBrackets(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            return Regex.Replace(name, @"\s*\[[^\]]*\]", ""); // remove [ ... ] blocks
        }
    }
}
