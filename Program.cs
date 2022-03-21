using Konsole;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace BinderToolTester
{
    internal class Program
    {
        private enum Game
        {
            DarkSouls,
            DarkSoulsRemastered,
            DarkSoulsII,
            Bloodborne,
            DarkSoulsIII,
            Sekiro
        }

        private struct TestCase
        {
            public Game game;
            public string file;
        }

        private static readonly List<TestCase> testCases = new() {
            new TestCase { game = Game.DarkSouls, file = "dvdbnd0.bdt" },
            new TestCase { game = Game.DarkSouls, file = "dvdbnd1.bdt" },
            new TestCase { game = Game.DarkSouls, file = "dvdbnd2.bdt" },
            new TestCase { game = Game.DarkSouls, file = "dvdbnd3.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "GameDataEbl.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "LqChrEbl.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "LqMapEbl.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "LqObjEbl.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "LqPartsEbl.bdt" },
            new TestCase { game = Game.DarkSoulsII, file = "enc_regulation.bnd.dcx" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data0.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data1.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data2.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data3.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data4.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "Data5.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "DLC1.bdt" },
            new TestCase { game = Game.DarkSoulsIII, file = "DLC2.bdt" },
            new TestCase { game = Game.Sekiro, file = "Data1.bdt" },
            new TestCase { game = Game.Sekiro, file = "Data2.bdt" },
            new TestCase { game = Game.Sekiro, file = "Data3.bdt" },
            new TestCase { game = Game.Sekiro, file = "Data4.bdt" },
            new TestCase { game = Game.Sekiro, file = "Data5.bdt" },
            new TestCase { game = Game.Sekiro, file = "Artwork_MiniSoundtrack\\Data.bdt" },
        };

        public static void Main(string[] args)
        {
            var configStr = File.ReadAllText("config.toml");
            var model = Toml.ToModel(configStr.Replace("\\", "\\\\"));
            var dsPath = (string)model["DarkSoulsPath"];
            var dsToSkip = ((Tomlyn.Model.TomlArray)model["DarkSoulsTestsToSkip"]).ToHashSet();
            var ds2Path = (string)model["DarkSoulsIIPath"];
            var ds2ToSkip = ((Tomlyn.Model.TomlArray)model["DarkSoulsIITestsToSkip"]).ToHashSet();
            var ds3Path = (string)model["DarkSoulsIIIPath"];
            var ds3ToSkip = ((Tomlyn.Model.TomlArray)model["DarkSoulsIIITestsToSkip"]).ToHashSet();
            var sekiroPath = (string)model["SekiroPath"];
            var sekiroToSkip = ((Tomlyn.Model.TomlArray)model["SekiroTestsToSkip"]).ToHashSet();
            var oldBinderToolPath = (string)model["OldBinderToolPath"];
            var newBinderToolPath = (string)model["NewBinderToolPath"];
            var outputPath = (string)model["OutputPath"];
            int dsTestsFailed = args.Contains("DarkSouls") ? RunTests(Game.DarkSouls, dsPath, dsToSkip, oldBinderToolPath, newBinderToolPath, outputPath) : 0;
            int ds2TestsFailed = args.Contains("DarkSoulsII") ? RunTests(Game.DarkSoulsII, ds2Path, ds2ToSkip, oldBinderToolPath, newBinderToolPath, outputPath) : 0;
            int ds3TestsFailed = args.Contains("DarkSoulsIII") ? RunTests(Game.DarkSoulsIII, ds3Path, ds3ToSkip, oldBinderToolPath, newBinderToolPath, outputPath) : 0;
            int sekiroTestsFailed = args.Contains("Sekiro") ? RunTests(Game.Sekiro, sekiroPath, sekiroToSkip, oldBinderToolPath, newBinderToolPath, outputPath) : 0;
            Console.WriteLine("Finished running tests.");
            Console.WriteLine("Tests failed:");
            Console.WriteLine($"    Dark Souls: {dsTestsFailed}");
            Console.WriteLine($" Dark Souls II: {ds2TestsFailed}");
            Console.WriteLine($"Dark Souls III: {ds3TestsFailed}");
            Console.WriteLine($"        Sekiro: {sekiroTestsFailed}");
        }

        private static int RunTests(Game game, string gamePath, HashSet<object?> toSkip, string oldBTPath, string newBTPath, string outputPath)
        {
            if (gamePath == "") {
                Console.WriteLine($"{game} path is empty, skipping tests.");
                return 0;
            }
            Console.WriteLine($"Running {game} tests...");
            int failed = 0;
            foreach (var test in testCases.Where(t => t.game == game)) {
                if (toSkip.Contains(test.file)) {
                    Console.WriteLine($"Skipping test for {test.file}");
                    continue;
                }
                Console.WriteLine($"Running test for {game} {test.file}");
                var success = RunTest(
                    new string[] { 
                        oldBTPath, newBTPath,
                        Path.Join(outputPath, game.ToString(), test.file.Replace(".bdt", "")+"Old"),
                        Path.Join(outputPath, game.ToString(), test.file.Replace(".bdt", "")+"New"),
                        Path.Join(gamePath, test.file)
                    }, 
                    MakeOnDiff(game, test.file)
                );
                if (success) failed++;
            }
            Console.WriteLine($"Done with {game} tests.");
            return failed;
        }

        private static Action<string> MakeOnDiff(Game game, string file)
        {
            FileStream? stream = null;
            return (string message) => {
                if (stream == null) {
                    if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
                    var fileSafe = file.Replace("\\", "_").Replace("/", "_");
                    var date = DateTime.Now.ToString("s").Replace(':', '.');
                    stream = new FileStream($@"logs\{game}_{fileSafe}_{date}.txt", FileMode.Create);
                }
                stream.Write(Encoding.UTF8.GetBytes(message + "\n"));
            };
        }

        public static bool RunTest(string[] args, Action<string> onDiff) {
            string bt1path = args[0];
            string bt2path = args[1];
            string bt1output = args[2];
            string bt2output = args[3];
            if (bt1path != "skip") {
                if (Directory.Exists(bt1output) && Directory.EnumerateFileSystemEntries(bt1output).FirstOrDefault((string?)null) != null) {
                    Console.WriteLine("Warning! Output directory of old binder tool was not empty!");
                }
                List<string> bt1args = new() {
                    args[4],
                    bt1output
                };
                bt1args.AddRange(args.Skip(5));
                ProcessStartInfo si1 = new(bt1path);
                bt1args.ForEach(si1.ArgumentList.Add);
                si1.WorkingDirectory = Path.GetDirectoryName(bt1path);
                Console.WriteLine("Running old Binder Tool...");
                var p = Process.Start(si1);
                p.WaitForExit();
                Console.WriteLine("Old binder tool exited.");
            } else Console.WriteLine("Skipping old Binder Tool.");
            if (bt2path != "skip") {
                if (Directory.Exists(bt2output) && Directory.EnumerateFileSystemEntries(bt2output).FirstOrDefault((string?)null) != null) {
                    Console.WriteLine("Warning! Output directory of new binder tool was not empty!");
                }
                List<string> bt2args = new() {
                    args[4],
                    bt2output
                };
                bt2args.AddRange(args.Skip(5));
                ProcessStartInfo si2 = new(bt2path);
                bt2args.ForEach(si2.ArgumentList.Add);
                si2.WorkingDirectory = Path.GetDirectoryName(bt2path);
                Console.WriteLine("Running new Binder Tool...");
                var p = Process.Start(si2);
                p.WaitForExit();
                Console.WriteLine("New Binder Tool exited.");
            } else Console.WriteLine("Skipping new Binder Tool.");
            Console.WriteLine("Comparing Binder Tool outputs...");
            HashSet<string> extantPaths = new();
            var paths = IterateDirRecursive(bt1output);
            int i = 1;
            int total = paths.Count;
            var progWindow = new Window(Console.WindowWidth - 1, 1);
            var diffWindow = Window.OpenBox("Differences", Console.WindowWidth, 10);
            bool ok = true;
            
            foreach (var (kind, path) in paths) {
                progWindow.CursorLeft = 0;
                progWindow.Write($"Comparing file {i}/{total} ({((float)i / total * 100.0):F2}%)");
                i++;
                extantPaths.Add(path);
                if (kind == FileKind.Directory) {
                    if (!Directory.Exists(Path.Combine(bt2output, path))) {
                        ok = false;
                        if (File.Exists(Path.Combine(bt2output, path))) Difference(diffWindow, onDiff, $"Directory {path} was a file instead");
                        else Difference(diffWindow, onDiff, $"Directory {path} did not exist");
                    }
                } else {
                    if (!File.Exists(Path.Combine(bt2output, path))) {
                        ok = false;
                        if (Directory.Exists(Path.Combine(bt2output, path))) Difference(diffWindow, onDiff, $"File {path} was a directory instead");
                        else Difference(diffWindow, onDiff, $"File {path} did not exist");
                    } else {
                        FileStream f1 = new(Path.Combine(bt1output, path), FileMode.Open, FileAccess.Read);
                        FileStream f2 = new(Path.Combine(bt2output, path), FileMode.Open, FileAccess.Read);
                        while (true) {
                            var b = f1.ReadByte();
                            var b2 = f2.ReadByte();
                            if (b == -1) {
                                if (b2 != -1) {
                                    Difference(diffWindow, onDiff, $"File {path} was too long");
                                    ok = false;
                                }
                                break;
                            }
                            if (b2 == -1) {
                                ok = false;
                                Difference(diffWindow, onDiff, $"File {path} was too short");
                                break;
                            }
                            if (b != b2) {
                                ok = false;
                                Difference(diffWindow, onDiff, $"File {path} had different contents");
                                break;
                            }
                        }
                        f1.Close();
                        f2.Close();
                    }
                }
            }
            foreach (var (kind, path) in IterateDirRecursive(bt2output)) {
                if (!extantPaths.Contains(path)) {
                    Difference(diffWindow, onDiff, $"Extra {kind} {path}");
                    ok = false;
                }
            }
            Console.WriteLine("Done comparing.");
            if (ok) Console.WriteLine("Test passed.");
            else Console.WriteLine("Test failed.");
            return ok;
        }

        private static void Difference(IConsole c, Action<string> onDiff, string message)
        {
            c.WriteLine(message);
            onDiff(message);
        }

        private enum FileKind
        {
            Directory, File
        }

        private static List<(FileKind, string)> IterateDirRecursive(string dirPath)
        {
            List<(FileKind, string)> ans = new();
            Stack<(FileKind, string)> stack = new();
            foreach (var dir in Directory.GetDirectories(dirPath)) stack.Push((FileKind.Directory, dir));
            foreach (var f in Directory.GetFiles(dirPath)) stack.Push((FileKind.File, f));
            while (stack.Count > 0) {
                var (kind, path) = stack.Pop();
                if (kind == FileKind.Directory) {
                    foreach (var dir in Directory.GetDirectories(path)) stack.Push((FileKind.Directory, dir));
                    foreach (var f in Directory.GetFiles(path)) stack.Push((FileKind.File, f));
                }
                ans.Add((kind, Path.GetRelativePath(dirPath, path)));
            }
            return ans;
        }

    }
}
