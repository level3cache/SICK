using Gameloop.Vdf;
using SteamKit2;

struct GameInfo
{                                               // example data:
    public string GameName;                     // Baldur's Gate 3
    public string InstallPath;                  // C:\Program Files (x86)\Steam\steamapps\common\Baldurs Gate 3
    public uint AppId;                          // 1086940
    public Dictionary<uint, ulong> Depots;      // [1086941, 1086944, 1419652, 1419653]
}

namespace sick
{
    internal static class Program
    {
        private static List<GameInfo> _gameList = [];
        
        private static void Main()
        {
            dynamic libraryfolders = VdfConvert.Deserialize( File.ReadAllText(@"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"));
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (var drive in libraryfolders.Value)
            {
                foreach (var app in drive.Value.apps)
                {
                    var currentGameInfo = new GameInfo();
                    dynamic appmanifest = VdfConvert.Deserialize( File.ReadAllText(@$"{drive.Value.path}\steamapps\appmanifest_{app.Key}.acf"));
                    var gameName = appmanifest.Value.name;
                    var installPath = @$"{drive.Value.path}\steamapps\common\{appmanifest.Value.installdir}";
                    var appId = uint.Parse($"{appmanifest.Value.appid}");
                    var depots = new Dictionary<uint, ulong>();
                    foreach (var depot in appmanifest.Value.InstalledDepots)
                    {
                        depots.Add( uint.Parse(depot.Key.ToString()), ulong.Parse(depot.Value.manifest.ToString()) );
                    }

                    currentGameInfo.GameName = gameName.ToString();
                    currentGameInfo.InstallPath = installPath;
                    currentGameInfo.AppId = appId;
                    currentGameInfo.Depots = depots;

                    _gameList.Add(currentGameInfo);
                }
            }
            
            _gameList.Sort((x, y) => x.GameName.CompareTo(y.GameName, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < _gameList.Count; i++)
            {
                Console.WriteLine($"[{i + 1}] {_gameList[i].GameName}");
            }
            
            var gameToCheck = 0;
            var IsValid = false;
            while (!IsValid)
            {
                Console.Write("Enter ID of Game to check and press Enter: ");
                string input = Console.ReadLine();
                if (input.Length == 0)
                {
                    Console.WriteLine("No input provided.");
                    continue;
                }
                if (int.TryParse(input, out gameToCheck))
                {
                    IsValid = true;
                }
                else
                {
                    try
                    {
                        int.Parse(input);
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("Try again using reasonable numbers.");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Try again using only numbers.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something went wrong, report on Github with the Error Message included:");
                        Console.WriteLine(e.Message);
                    }
                }
                Console.WriteLine();
            }
            
            Console.Write($"Game to check: ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{_gameList[gameToCheck-1].GameName}");
            Console.ResetColor();

            CheckFiles(gameToCheck);
            
            Console.WriteLine();
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }
        
        static void CheckFiles(int gameToCheck)
        {
            var expectedFiles = new List<string>();
            var foundFiles = new List<string>();
            var appId = _gameList[gameToCheck-1].AppId;
            foreach (var depot in _gameList[gameToCheck-1].Depots)
            {
                byte[] encryptedManifest = [];
                try
                {
                    encryptedManifest =
                        File.ReadAllBytes(
                            $@"C:\Program Files (x86)\Steam\depotcache\{depot.Key}_{depot.Value}.manifest");
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine("Error encountered. Is this Game's drive connected?");
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("Manifest File not found. Try to verify files using Steam first!");
                }
                
                var manifest = DepotManifest.Deserialize(encryptedManifest);
                var decryptedFiles = manifest.Files;
                foreach (var file in decryptedFiles)
                {
                    if (file.TotalSize == 0) continue;
                    expectedFiles.Add(file.FileName.ToString());
                }
            }
            
            expectedFiles.Sort((x, y) => x.CompareTo(y, StringComparison.OrdinalIgnoreCase));
            expectedFiles = expectedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foundFiles = GetRelativeFilePaths(_gameList[gameToCheck - 1].InstallPath);
            foundFiles.Sort((x, y) => x.CompareTo(y, StringComparison.OrdinalIgnoreCase));
            foundFiles = foundFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            
            var (extraFiles, missingFiles) = CompareLists(expectedFiles, foundFiles);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Out.WriteLine("Extra Files:");
            Console.ResetColor();
            foreach (var extraFile in extraFiles)
            {
                Console.Out.WriteLine($"    {extraFile}");
            }

            if (extraFiles.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Out.WriteLine("    No extra files found. Yay! \\( ﾟヮﾟ)/");
                Console.ResetColor();
            }

            Console.Out.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Out.WriteLine("Missing Files:");
            Console.ResetColor();
            foreach (var missingFile in missingFiles)
            {
                Console.Out.WriteLine($"    {missingFile}");
            }

            if (missingFiles.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("    No missing files found. Yay! \\( ﾟヮﾟ)/");
                Console.ResetColor();
            }
        }

        public static List<string> GetRelativeFilePaths(string baseDirectory)
        {
            if (!Directory.Exists(baseDirectory))
            {
                throw new DirectoryNotFoundException($"Could not find directory {baseDirectory}");
            }

            return Directory.EnumerateFiles(baseDirectory, "*", SearchOption.AllDirectories)
                            .Select(fullPath => Path.GetRelativePath(baseDirectory, fullPath))
                            .ToList();
        }

        public static (List<string> extraFiles, List<string> missingFiles) CompareLists(List<string> expected, List<string> found)
        {
            var expectedIndex = 0;
            var foundIndex = 0;

            List<string> extraFiles = [];
            List<string> missingFiles = [];

            while (expectedIndex <= expected.Count - 1 && foundIndex <= found.Count - 1)
            {
                if (expected[expectedIndex] == found[foundIndex])
                {
                    expectedIndex++;
                    foundIndex++;
                } else if (expected[expectedIndex].CompareTo(found[foundIndex], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    missingFiles.Add(expected[expectedIndex]);
                    expectedIndex++;
                } else if (expected[expectedIndex].CompareTo(found[foundIndex], StringComparison.OrdinalIgnoreCase) > 0)
                {
                    extraFiles.Add(found[foundIndex]);
                    foundIndex++;
                }
            }

            return (extraFiles, missingFiles);
        }
    }
}