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

namespace ShelfScan
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    public sealed class PlexShowVerifier
    {
        /// <summary>
        /// Main entry point to verify a TV episode file.
        /// Returns true if all naming rules pass, false otherwise.
        /// </summary>
        public static bool VerifyShow(string filePath)
        {
            // Ignore featurettes
            if (Path.GetFileNameWithoutExtension(filePath).EndsWith("-featurette", StringComparison.OrdinalIgnoreCase))
                return true;

            bool allGood = true;
            string? seasonFolder = null;
            string? showFolder = null;

            var parent = Directory.GetParent(filePath);
            if (parent != null)
            {
                seasonFolder = parent.Name;

                var grandParent = parent.Parent;
                if (grandParent != null)
                {
                    showFolder = grandParent.FullName;
                }
            }

            // If either folder is null, report an issue immediately
            if (string.IsNullOrEmpty(showFolder) || string.IsNullOrEmpty(seasonFolder))
            {
                Console.WriteLine($"\n{filePath}\n  File is not in a valid TV folder structure (Show/Season).");
                return false;
            }

            bool isDateBasedShow = DetectShowType(filePath);

            // 1. Verify show folder
            if (!VerifyShowFolder(showFolder, filePath))
                allGood = false;

            // 2. Verify season folder
            if (!VerifySeasonFolder(seasonFolder, filePath))
                allGood = false;

            // 3. Verify episode filename
            if (!VerifyEpisodeFilename(filePath, isDateBasedShow))
                allGood = false;

            // 3b. Verify if the tags in square brackets aren't supposed to be in curly braces
            if (!PlexCommonVerifier.ValidateSquareBracketTags(filePath))
                allGood = false;

            // 4. Verify that season number in the folder matches the season number in the filename (for season-based shows)
            if (!isDateBasedShow)
            {
                var match = Regex.Match(Path.GetFileNameWithoutExtension(filePath), @"[sS](\d{1,4})[eE](\d{1,4})");
                if (match.Success)
                {
                    int seasonFromFile = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                    // Special folder check
                    if (seasonFolder.Equals("Specials", StringComparison.OrdinalIgnoreCase))
                    {
                        if (seasonFromFile != 0)
                        {
                            Console.WriteLine($"\n{filePath}\n  Mismatched season number. Folder is 'Specials', but filename says 'S{seasonFromFile:D2}'.");
                            allGood = false;
                        }
                    }
                    else
                    {
                        var folderMatch = Regex.Match(seasonFolder, @"Season (\d+)", RegexOptions.IgnoreCase);
                        if (folderMatch.Success)
                        {
                            int seasonFromFolder = int.Parse(folderMatch.Groups[1].Value, CultureInfo.InvariantCulture);

                            if (seasonFromFile == 0)
                            {
                                // Allow zero-season folders
                                if (!(seasonFolder.Equals("Season 0", StringComparison.OrdinalIgnoreCase) ||
                                      seasonFolder.Equals("Season 00", StringComparison.OrdinalIgnoreCase)))
                                {
                                    Console.WriteLine($"\n{filePath}\n  Filename has S00, but folder '{seasonFolder}' is not a valid zero-season folder.");
                                    allGood = false;
                                }
                            }
                            else if (seasonFromFile != seasonFromFolder)
                            {
                                Console.WriteLine($"\n{filePath}\n  Mismatched season number. Folder says '{seasonFolder}', but filename says 'S{seasonFromFile:D2}'.");
                                allGood = false;
                            }
                        }
                    }
                }
            }

            return allGood;
        }

        /// <summary>
        /// Detects if the show is date-based by inspecting files in the season folder.
        /// </summary>
        private static bool DetectShowType(string filePath)
        {
            string? seasonFolderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(seasonFolderPath) || !Directory.Exists(seasonFolderPath))
            {
                Console.WriteLine($"\n{filePath}\n  Cannot detect season folder or folder does not exist.");
                return false; // Early exit
            }

            var files = Directory.GetFiles(seasonFolderPath);

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (Regex.IsMatch(fileName, @"\d{4}-\d{2}-\d{2}") ||
                    Regex.IsMatch(fileName, @"\d{2}-\d{2}-\d{4}"))
                {
                    return true; // Date-based
                }
            }

            return false; // Season-based
        }

        /// <summary>
        /// Verifies the show folder name (ShowName (YYYY) {OptionalID}).
        /// </summary>
        private static bool VerifyShowFolder(string showFolder, string filePath)
        {
            string folderName = Path.GetFileName(showFolder);
            string pattern = @"^.+( \(\d{4}\))?( \{(tmdb|tvdb|imdb)-\d+\})?$";

            if (!Regex.IsMatch(folderName, pattern))
            {
                Console.WriteLine($"\n{filePath}\n  Show folder name '{folderName}' is invalid. Expected format: 'Show Name (YYYY)' optionally with ' {{tmdb | tvdb | imdb - ID}}'. Year is optional.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies the season folder name (Season XX or Specials).
        /// </summary>
        private static bool VerifySeasonFolder(string seasonFolder, string filePath)
        {
            string pattern = @"^(Season \d+|Specials)$";

            if (!Regex.IsMatch(seasonFolder, pattern))
            {
                Console.WriteLine($"\n{filePath}\n  Season folder name '{seasonFolder}' is invalid. Expected: 'Season X' (any number of digits) or 'Specials'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies episode filename: season-based, multi-episode, date-based, and split files.
        /// </summary>
        /// 
        private static bool VerifyEpisodeFilename(string filePath, bool isDateBasedShow)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (isDateBasedShow)
            {
                // Date-based format: Show Name - YYYY-MM-DD - Optional Info (dashes periods or spaces)
                string datePattern = @"^.+ - (\d{4}[-\. ]\d{2}[-\. ]\d{2}|\d{2}[-\. ]\d{2}[-\. ]\d{4})( - .+)?$";
                if (!Regex.IsMatch(fileName, datePattern))
                {
                    Console.WriteLine($"\n{filePath}\n  Invalid date-based episode filename. Expected format: 'Show Name - YYYY-MM-DD - Optional Info.ext' or 'Show Name - DD-MM-YYYY - Optional Info.ext'.");
                    return false;
                }
            }
            else
            {
                // Season-based: check parts individually

                // 1. Check main episode SXXEYY
                var mainEpisodeMatch = Regex.Match(fileName, @"[sS](\d{1,4})[eE](\d{1,4})");
                if (!mainEpisodeMatch.Success)
                {
                    Console.WriteLine($"\n{filePath}\n  Invalid main episode number. Expected format 'SXXEYY'.");
                    return false;
                }

                // 2. Check multi-episode (optional)
                var multiEpisodeMatch = Regex.Match(fileName, @"-[eE](\d{2})");
                if (fileName.Contains('-') && !multiEpisodeMatch.Success)
                {
                    // If a dash exists after the main episode but does not match '-eZZ', it's invalid
                    string remaining = fileName[(mainEpisodeMatch.Index + mainEpisodeMatch.Length)..];
                    if (remaining.StartsWith('-') && !Regex.IsMatch(remaining, @"^-[eE]\d{4}"))
                    {
                        Console.WriteLine($"\n{filePath}\n  Invalid multi-episode format. Expected '-eZZ' after main episode number.");
                        return false;
                    }
                }

                // Optional info is ignored, no validation needed
                // Note: we cannot reliably validate split parts without risking false positives.
                // This is because the split part could be part of a legitimate title or info segment.
            }

            return true;
        }
    }
}
