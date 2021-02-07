using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GOSkinInstaller
{
    public class Program
    {
        private static readonly List<string> _skins = new List<string>();

        public static void Main(string[] args)
        {
            RunInstaller();

            Console.ResetColor();
            Console.WriteLine("\nPress any key to finish the installation.");
            Console.ReadKey();
        }

        public static void RunInstaller()
        {
            while (true)
            {
                Console.ResetColor();

                if (!Directory.Exists(GetDirectoryFromCurrent("/resources/app/desktop/student/")))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Grande Omega not found.\nPlease make sure the skin installer is located in your Grande Omega root directory.");
                    return;
                }

                if (!Directory.Exists(GetDirectoryFromCurrent("/skins/")))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not find a 'skins' folder in your Grande Omega directory.\nPlease make sure it exists before continuing.\n");
                    return;
                }

                foreach (var skin in Directory.GetDirectories(GetDirectoryFromCurrent("/skins/")).OrderBy(x => x))
                    _skins.Add(Path.GetFileName(skin));

                if (_skins.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Skins folder found, but you don't have any skins installed.\nInstall skins at https://github.com/Grande-Omega-Skins/Grande-Omega-Skins");
                    return;
                }

                if (!Directory.Exists(GetDirectoryFromCurrent("/skins/default/wwwroot/")))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("You don't have a default skin folder. Please makes sure your skins folder contains a valid skin called 'default' and try again.");
                    return;
                }

                Console.WriteLine(
                    $"Welcome to the Grande Omega skin installer.\n" +
                    $"Please select the skin you would like to install:");

                do
                {
                    Console.ResetColor();

                    for (int i = 0; i < _skins.Count; i++)
                    {
                        Console.Write($"\n[{i + 1}] {_skins[i]}");
                        if (!Directory.Exists(GetDirectoryFromCurrent($"/skins/{_skins[i]}/wwwroot/")))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(" [INVALID]");
                            Console.ResetColor();
                        }
                        if (i + 1 == _skins.Count)
                            Console.WriteLine("\n");
                    }

                    if (!int.TryParse(Console.ReadLine(), out int input) || input < 1 || input > _skins.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Skin not found. Please enter a valid number.");
                        continue;
                    }

                    var skin = _skins[input - 1];

                    if (!Directory.Exists(GetDirectoryFromCurrent($"/skins/{skin}/wwwroot/")))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"This skin is invalid.\nPlease makes sure /skins/{skin}/ contains a /wwwroot/ folder and try again.");
                        continue;
                    }

                    var result = CopyFiles(skin);

                    if (result.IsCompletedSuccessfully)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("\nSkin installed successfully!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\nSkin installation failed!");
                    }

                    return;
                }
                while (true);
            }
        }

        public static Task CopyFiles(string skin, bool restoreDefaults = true)
        {
            try
            {
                var skinPath = GetDirectoryFromCurrent($"/skins/{skin}/");
                var destinationPath = GetDirectoryFromCurrent("/resources/app/desktop/Student/");

                if (restoreDefaults)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Selected skin: {skin}\n");
                    RestoreDefaultFiles(destinationPath + "wwwroot/");
                }

                foreach (string dir in Directory.GetDirectories(skinPath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dir.Replace(skinPath, destinationPath));  

                foreach (string file in Directory.GetFiles(skinPath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(file, file.Replace(skinPath, destinationPath), true);

                    if (restoreDefaults)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"File copied: {Path.GetFileName(file)}");
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {e.Message}");

                return Task.FromException(e);
            }                
        }

        public static void RestoreDefaultFiles(string destinationPath)
        {
            var defaultDestinationPath = GetDirectoryFromCurrent("/resources/app/desktop/Student/wwwroot/");

            if (Directory.Exists(defaultDestinationPath))
                Directory.Delete(defaultDestinationPath, recursive: true);
            Directory.CreateDirectory(defaultDestinationPath);

            CopyFiles("default", restoreDefaults: false);
        }

        public static string GetDirectoryFromCurrent(string path)
        {
            return Directory.GetCurrentDirectory() + path;
        }
    }
}
