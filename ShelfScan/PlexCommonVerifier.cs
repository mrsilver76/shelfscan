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
    /// <summary>
    /// Plex verification methods that are common to movies and TV shows.
    /// </summary>
    internal sealed class PlexCommonVerifier
    {
        /// <summary>
        /// Check for square-bracket tags that should probably be in curly braces.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool ValidateSquareBracketTags(string path)
        {
            string name = Path.GetFileName(path);
            bool isFile = File.Exists(path) || Path.HasExtension(name);

            // Pattern to detect tags that should probably be in {..}
            string pattern = @"\[(tmdb-|tvdb-|imdb-|edition-).*?\]";
            var matches = Regex.Matches(name, pattern, RegexOptions.IgnoreCase);

            if (matches.Count > 0)
            {
                string type = isFile ? "file" : "folder";
                foreach (Match match in matches)
                    Console.WriteLine($"\n{path}\n  Tag '{match.Value}' in {type} should probably be in {{...}}");

                return false;
            }

            return true;
        }

    }
}
