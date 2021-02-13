using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GOSkinInstallerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            _ = await Dispatcher.InvokeAsync(async () => await CopyFiles(Dispatcher, InstallButton, ProgressBar, ProgressText, Dropdown));
        }
        
        public static async Task CopyFiles(Dispatcher dispatcher, Button button, ProgressBar progressBar, Label progressText, ComboBox dropdown = null, string skin = "", bool restoreDefaults = true)
        {
            skin = dropdown != null ? dropdown.Text : skin;
            var skinPath = GetDirectoryFromCurrent($"/skins/") + $"{skin}/";
            var destinationPath = GetDirectoryFromCurrent("/resources/app/desktop/Student/");

            var defaultFolderExists = DefaultFolderCheck();

            if (Directory.Exists(skinPath + "wwwroot/") && defaultFolderExists)
            {
                button.IsEnabled = false;
                button.Content = "Installing";

                if (dropdown != null)
                    dropdown.IsEnabled = false;

                if (restoreDefaults)
                {
                    await RestoreDefaultFiles(dispatcher, button, progressBar, progressText, skin: "default");
                    if (skin == "default")
                    {
                        dropdown.IsEnabled = true;
                        return;
                    }
                }

                foreach (string dir in Directory.GetDirectories(skinPath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dir.Replace(skinPath, destinationPath));

                var files = Directory.GetFiles(skinPath, "*.*", SearchOption.AllDirectories);
                var count = 0;

                foreach (string file in files)
                {
                    await Task.Run(() => File.Copy(file, file.Replace(skinPath, destinationPath), true));
                    count++;
                    progressText.Content = count < files.Length ? $"File copied: {Path.GetFileName(file)}" : "Skin installed successfully!";
                    progressBar.Value = (float)count / files.Length * 100;
                }
            }
            else if (defaultFolderExists)
            {
                MessageBox.Show("The skin you selected is not a valid skin. Please make sure you select a skin downloaded from the GitHub repository at https://github.com/Grande-Omega-Skins/Grande-Omega-Skins and follow the installation steps.", "Invalid skin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            button.IsEnabled = true;
            button.Content = "Install";
            if (dropdown != null)
                dropdown.IsEnabled = true;
        }
        
        public static async Task RestoreDefaultFiles(Dispatcher dispatcher, Button button, ProgressBar progressBar, Label progressText, string skin = "")
        {
            var defaultDestinationPath = GetDirectoryFromCurrent("/resources/app/desktop/Student/wwwroot/");

            if (Directory.Exists(defaultDestinationPath))
                Directory.Delete(defaultDestinationPath, recursive: true);            
            Directory.CreateDirectory(defaultDestinationPath);

            await CopyFiles(dispatcher, button, progressBar, progressText, skin: skin, restoreDefaults: false);
        }

        private async void Dropdown_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDropdownItems();

            await Task.Delay(500);
            DefaultFolderCheck();
        }

        public static bool DefaultFolderCheck()
        {
            if (!Directory.GetDirectories(GetDirectoryFromCurrent("/skins/")).Any(x => Path.GetFileName(x) == "default"))
            {
                MessageBox.Show("Default folder missing. You need a skin called 'default' in order use this installer.\nPlease follow the instructions at https://github.com/Grande-Omega-Skins/Grande-Omega-Skins", "Missing default skin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            return true;
        }

        public static string GetDirectoryFromCurrent(string path)
        {
            return Directory.GetCurrentDirectory() + path;
        }

        private void Dropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((string)Dropdown.SelectedValue))
                InstallButton.IsEnabled = true;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadDropdownItems();
        }

        private void ReloadDropdownItems()
        {
            Dropdown.Items.Clear();
            
            var skinsDirectory = GetDirectoryFromCurrent("/skins/");

            Directory.CreateDirectory(skinsDirectory);

            foreach (var skin in Directory.GetDirectories(skinsDirectory).OrderBy(x => x))
                Dropdown.Items.Add(Path.GetFileName(skin));

            Dropdown.SelectedValue = Dropdown.Items.Contains("default") ? "default" : Dropdown.Items.OfType<string>().FirstOrDefault();
        }
    }
}
