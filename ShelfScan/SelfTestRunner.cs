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
    internal sealed class SelfTestRunner
    {
        /// <summary>
        /// Runs self-tests for both movie and TV show verifiers.
        /// </summary>
        public static void RunSelfTests()
        {
            Console.WriteLine();
            Console.WriteLine("Running self-tests...");

            // Define test folders relative to the base folder
            string[] testFolders =
            [
                    "pass/movies",
                    "pass/tv",
                    "fail/movies",
                    "fail/tv"
            ];

            // Keep a summary of results per folder
            var summary = new List<(string Folder, int Total, int PassedAsExpected, int Unexpected)>();

            foreach (var relativeFolder in testFolders)
            {
                string fullPath = Path.Combine(Program.FolderToScan, relativeFolder);
                if (!Directory.Exists(fullPath))
                {
                    Console.WriteLine($"Skipping missing folder: {relativeFolder}");
                    continue;
                }

                Console.WriteLine();
                Console.WriteLine($"========== Testing folder: {relativeFolder} ==========");
                
                List<string> files = [];
                try
                {
                    Program.AddMediaFiles(fullPath, files, [".mp4", ".mkv", ".avi"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning {relativeFolder}: {ex.Message}");
                    continue;
                }

                bool shouldPass = relativeFolder.StartsWith("pass/", StringComparison.OrdinalIgnoreCase);
                int passed = 0, failed = 0;

                foreach (var file in files)
                {
                    bool isValid = relativeFolder.EndsWith("movies", StringComparison.OrdinalIgnoreCase)
                        ? PlexMovieVerifier.VerifyMovies(file, fullPath)
                        : PlexShowVerifier.VerifyShow(file);

                    if (isValid == shouldPass)
                        passed++;
                    else
                        failed++;

                    // Extra check: file passed but it is in a fail folder
                    if (!shouldPass && isValid)
                        Console.WriteLine($"\n{file}\n  Incorrectly passed verification (in 'fail' folder).");
                }

                summary.Add((relativeFolder, files.Count, passed, failed));
            }

            // Show final summary
            Console.WriteLine();
            Console.WriteLine("========== SELF-TEST SUMMARY ==========");
            Console.WriteLine();
            Console.WriteLine($"{"Folder",-15} {"Checked",15} {"Passed",15} {"Unexpected",15}");

            foreach (var (Folder, Total, PassedAsExpected, Unexpected) in summary)
                Console.WriteLine($"{Folder,-15} {Total,15} {PassedAsExpected,15} {Unexpected,15}");

            bool allOk = summary.All(s => s.Unexpected == 0);
            Console.WriteLine(allOk
                ? "\nAll self-tests passed as expected!"
                : "\nSome self-tests did not behave as expected.");
            Environment.Exit(allOk ? 0 : -1);
        }
    }
}

