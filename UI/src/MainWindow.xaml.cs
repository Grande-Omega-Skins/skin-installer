using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private static readonly string _skinsJsonUrl = "https://gist.github.com/VACEfron/5a8f0b36104492b24818f2f464e00431/raw";

        public MainWindow()
        {
            InitializeComponent();
        }        
        
        private static async Task CopyFiles(Dispatcher dispatcher, Button button, Button downloadSkinsButton, ProgressBar progressBar, Label progressText, ComboBox dropdown = null, string skin = "", bool restoreDefaults = true)
        {
            skin = dropdown != null ? dropdown.Text : skin;
            var skinPath = $"skins/{skin}/";
            var destinationPath = "resources/app/desktop/Student/";

            GrandeOmegaDirCheck();

            var defaultFolderExists = DefaultFolderCheck();

            if (Directory.Exists(skinPath + "wwwroot/") && defaultFolderExists)
            {
                button.IsEnabled = false;
                downloadSkinsButton.IsEnabled = false;
                button.Content = "Installing...";

                if (dropdown != null)
                    dropdown.IsEnabled = false;

                if (restoreDefaults)
                {
                    await RestoreDefaultFiles(dispatcher, button, downloadSkinsButton, progressBar, progressText, skin: "default");
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
                MessageBox.Show("The skin you selected is not a valid skin. Please make sure to use a skin downloaded with the 'download skins' button", "Invalid skin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            button.IsEnabled = true;
            downloadSkinsButton.IsEnabled = true;
            button.Content = "Install";
            if (dropdown != null)
                dropdown.IsEnabled = true;
        }
        
        private static async Task RestoreDefaultFiles(Dispatcher dispatcher, Button button, Button downloadSkinsButton, ProgressBar progressBar, Label progressText, string skin = "")
        {
            var defaultDestinationPath = "resources/app/desktop/Student/wwwroot/";

            if (Directory.Exists(defaultDestinationPath))
                await Task.Run(() => Directory.Delete(defaultDestinationPath, recursive: true));            
            Directory.CreateDirectory(defaultDestinationPath);

            await CopyFiles(dispatcher, button, downloadSkinsButton, progressBar, progressText, skin: skin, restoreDefaults: false);
        }

        private async void Dropdown_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(250);

            GrandeOmegaDirCheck();

            await Task.Delay(1);

            ReloadDropdownItems();

            await Task.Delay(1);

            DefaultFolderCheck();
        }        

        private void Dropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((string)Dropdown.SelectedValue))
                InstallButton.IsEnabled = true;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            _ = await Dispatcher.InvokeAsync(async () => await CopyFiles(Dispatcher, InstallButton, DownloadSkinsButton, ProgressBar, ProgressText, Dropdown));
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadDropdownItems();
        }

        private void DownloadSkinsButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.InvokeAsync(async () =>
            {
                if (!IsConnectedToInternet())
                {
                    MessageBox.Show("Can't download the skins from GitHub since no internet connection is established.", "No internet connection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                var count = 0;

                InstallButton.IsEnabled = false;
                Dropdown.IsEnabled = false;
                RefreshButton.IsEnabled = false;
                DownloadSkinsButton.IsEnabled = false;
                DownloadSkinsButton.Content = "Downloading";

                Directory.CreateDirectory("skins/");

                var client = new HttpClient();
                string[] skins = ((JObject)JsonConvert.DeserializeObject(await client.GetStringAsync(_skinsJsonUrl)))["skins"].ToArray().Select(x => (string)x).ToArray();

                foreach (var skin in skins)
                {
                    try
                    {
                        var zipPath = $"skins/{skin}.zip";
                        var skinPath = $"skins/{skin}";

                        var byteArray = await GetByteArrayFromUrl(skin);

                        using (var fileStream = File.Create(zipPath))
                            await fileStream.WriteAsync(byteArray);

                        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, "skins/", true));

                        if (Directory.Exists(skinPath))
                            await Task.Run(() => new DirectoryInfo(skinPath).Delete(true));

                        await Task.Run(() => Directory.Move($"{skinPath}-latest/{(skin == "default" ? "default" : "dist")}", skinPath));
                        Directory.Delete($"{skinPath}-latest", recursive: true);

                        await Task.Run(() => File.Delete(zipPath));

                        count++;
                        ProgressText.Content = count < skins.Length ? $"Skin downloaded: {skin}" : "All skins downloaded!";
                        ProgressBar.Value = (float)count / skins.Length * 100;
                    }
                    catch
                    { 
                        continue;
                    }
                }

                ReloadDropdownItems();

                InstallButton.IsEnabled = true;
                Dropdown.IsEnabled = true;
                RefreshButton.IsEnabled = true;
                DownloadSkinsButton.IsEnabled = true;
                DownloadSkinsButton.Content = "Download skins";
            });

        }

        private async Task<byte[]> GetByteArrayFromUrl(string skin)
        {
            using var client = new HttpClient();

            var response = await client.GetAsync($"https://github.com/Grande-Omega-Skins/{skin}/archive/latest.zip");
            return await response.Content.ReadAsByteArrayAsync();
        }

        private void ReloadDropdownItems()
        {
            Dropdown.Items.Clear();

            var skinsDirectory = "skins/";

            Directory.CreateDirectory(skinsDirectory);

            foreach (var skin in Directory.GetDirectories(skinsDirectory).OrderBy(x => x))
                Dropdown.Items.Add(Path.GetFileName(skin));

            var dropdownItems = Dropdown.Items.OfType<string>();

            Dropdown.SelectedValue = Dropdown.Items.Contains("default") ? "default" : dropdownItems.FirstOrDefault();

            var hasItems = dropdownItems.Count() > 0;

            Dropdown.IsEnabled = hasItems;
            InstallButton.IsEnabled = hasItems;
        }

        private static void GrandeOmegaDirCheck()
        {
            if (!Directory.Exists("resources/app/desktop/student/"))
            {
                MessageBox.Show("The installer is not located in a Grande Omega directory. Please make sure to locate your Grande Omega root directory and move this installer inside of it.", "Grande Omega not found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
            }
        }

        private static bool DefaultFolderCheck()
        {
            if (!Directory.GetDirectories("skins/").Any(x => Path.GetFileName(x) == "default"))
            {
                MessageBox.Show("Default folder missing. Click the 'download skins' button to install the default skin.", "Missing default skin", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            return true;
        }

        private static bool IsConnectedToInternet()
        {
            try
            {
                using var client = new HttpClient();
                client.GetAsync("http://google.com/generate_204");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
