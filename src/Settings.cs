using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SongFinder
{
    public partial class Settings
    {
        public Settings()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Checks if a given file extension has a valid format
        /// </summary>
        /// <param name="fileExtension">File extension of a file including preceding dot</param>
        /// <returns>Boolean if file extension is valid</returns>
        private static bool IsFileExtensionValid(string fileExtension)
        {
            Regex regex = new Regex(@"^.{0}[.][a-zA-Z0-9]{2,}$");
            Match match = regex.Match(fileExtension);

            return match.Success;
        }

        /// <summary>
        /// Checks if the required folders and files for the configuration parameters exist and creates them if not
        /// </summary>
        public static void ConfigPreparation()
        {
            var configFileTemplateLocation = Path.Combine(Directory.GetCurrentDirectory(), "SongFinder.exe.Config");
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var glatecFolder = Path.Combine(appdata, "GlaTEC");
            var configFile = Path.Combine(glatecFolder, "songfinder.app.config");

            if (!File.Exists(configFileTemplateLocation))
            {
                MessageBox.Show(
                    "Eine erforderliche Konfigurationsdatei existiert nicht. Bitte installieren sie das Programm erneut um diesen Fehler zu beheben!",
                    "Fataler Fehler!", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            if (!Directory.Exists(glatecFolder))
            {
                Directory.CreateDirectory(glatecFolder);
            }


            if (!File.Exists(configFile))
            {
                File.Copy(configFileTemplateLocation, configFile, false);
            }
        }

        /// <summary>
        /// Checks if an csv file has a given format for checking forbidden songs
        /// </summary>
        /// <param name="filename">Path of the CSV File</param>
        /// <returns>Bolean if File is valid</returns>
        public static bool CheckForbiddenCsv(string filename)
        {
            try
            {
                var csvPath = filename;


                string csvContent = File.ReadAllText(csvPath);

                string[] numbers = csvContent.Split(',');
                foreach (var str in numbers)
                {
                    int temp;
                    if (!int.TryParse(str, out temp) || str.Length != 3 || temp > 694)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Get a String from a key in App.config
        /// </summary>
        /// <param name="key">Name of the Setting</param>
        /// <returns>Value of Setting</returns>
        public static string GetConfigValue(string key)
        {
            var configFileLocation =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GlaTEC",
                    "songfinder.app.config");
            ExeConfigurationFileMap configMap =
                new ExeConfigurationFileMap {ExeConfigFilename = configFileLocation};
            Configuration configFile =
                ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;


            return settings[key].Value;
        }

        /// <summary>
        /// Insert a String into a key in App.config
        /// </summary>
        /// <param name="key">Name of the Setting</param>
        /// <param name="value">Value of Setting</param>
        public static void SettingsUpdater(string key, string value)
        {
            try
            {
                var configFileLocation =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GlaTEC",
                        "songfinder.app.config");
                ExeConfigurationFileMap configMap =
                    new ExeConfigurationFileMap {ExeConfigFilename = configFileLocation};
                Configuration configFile =
                    ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);


                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// Get Data from App.config
        /// </summary>
        private void GetData()
        {
            try
            {
                bool checkForbidden;
                bool.TryParse(GetConfigValue("CheckForbidden"), out checkForbidden);

                bool preserveNull;
                bool.TryParse(GetConfigValue("PreserveNull"), out preserveNull);

                bool stayOnTop;
                bool.TryParse(GetConfigValue("StayOnTop"), out stayOnTop);

                string nullOption = "5";

                if (checkForbidden)
                {
                    CheckForbiddenBox.IsChecked = true;
                }
                else
                {
                    ForbiddenFileBox.IsEnabled = false;
                    ForbiddenFileOpen.IsEnabled = false;
                }

                ForbiddenFileBox.Text = GetConfigValue("ForbiddenCSV");
                SongFolder.Text = GetConfigValue("SongFolder");
                Prefix.Text = GetConfigValue("Prefix");
                Extension.Text = GetConfigValue("FileExtension");
                if (preserveNull)
                {
                    PresNull.IsChecked = true;
                    nullOption = "005";
                }

                string spaceSign;
                switch (GetConfigValue("SpaceSign"))
                {
                    case "0":
                        spaceSign = " ";
                        Leerzeichen.IsSelected = true;
                        break;
                    case "1":
                        spaceSign = "_";
                        Unterstrich.IsSelected = true;
                        break;
                    case "2":
                        spaceSign = "-";
                        Bindestrich.IsSelected = true;
                        break;
                    default:
                        spaceSign = null;
                        Nichts.IsSelected = true;
                        break;
                }

                Preview.Text = GetConfigValue("Prefix") + spaceSign + nullOption +
                               GetConfigValue("FileExtension");

                if (stayOnTop)
                {
                    StayOnTopBox.IsChecked = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Insert Data into App.config from UI
        /// </summary>
        private async void InsertData()
        {
            try
            {
                bool checkLicense = CheckForbiddenBox.IsChecked ?? false;
                bool preserveNull = PresNull.IsChecked ?? false;
                bool stayOnTop = StayOnTopBox.IsChecked ?? false;

                SettingsUpdater("CheckForbidden", checkLicense.ToString());

                if (checkLicense)
                {
                    if (File.Exists(ForbiddenFileBox.Text))
                    {
                        if (!CheckForbiddenCsv(ForbiddenFileBox.Text))
                        {
                            MessageBox.Show("Die Lizenzdatei ist fehlerhaft", "Fehler!", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }

                        SettingsUpdater("ForbiddenCSV", ForbiddenFileBox.Text);
                    }
                    else
                    {
                        this.ShowModalMessageExternal("Fehler!", "Die Liste der unlizensierten Lieder existiert nicht");
                        SettingsUpdater("CheckForbidden", false.ToString());
                        return;
                    }
                }


                if (Directory.Exists(SongFolder.Text))
                {
                    SettingsUpdater("SongFolder", SongFolder.Text);
                }
                else
                {
                    await this.ShowMessageAsync("Fehler", "Der angegebene Ordner ist fehlerhaft!");
                    return;
                }

                SettingsUpdater("Prefix", Prefix.Text);
                SettingsUpdater("SpaceSign", SpaceSign.SelectedIndex.ToString());

                if (IsFileExtensionValid(Extension.Text))
                {
                    SettingsUpdater("FileExtension", Extension.Text);
                }
                else
                {
                    await this.ShowMessageAsync("Fehler", "Die Angegebene Dateierweiterung ist fehlerhaft");
                    return;
                }


                SettingsUpdater("PreserveNull", preserveNull.ToString());
                SettingsUpdater("StayOnTop", stayOnTop.ToString());


                await this.ShowMessageAsync("Einstellungen gespeichert", "");
                Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Generates an filename example with given user input
        /// </summary>
        /// <returns>The generated Example as String</returns>
        private string UpdateExample()
        {
            string nullOption = "5";
            if (PresNull.IsChecked ?? false)
            {
                nullOption = "005";
            }

            string spaceSign;
            switch (SpaceSign.SelectedIndex.ToString())
            {
                case "0":
                    spaceSign = " ";
                    Leerzeichen.IsSelected = true;
                    break;
                case "1":
                    spaceSign = "_";
                    Unterstrich.IsSelected = true;
                    break;
                case "2":
                    spaceSign = "-";
                    Bindestrich.IsSelected = true;
                    break;
                default:
                    spaceSign = null;
                    Nichts.IsSelected = true;
                    break;
            }

            return Prefix.Text + spaceSign + nullOption + Extension.Text;
        }

        private void Settings_OnContentRenderedOnLoaded(object sender, EventArgs e)
        {
            GetData();
        }

        private void Folder_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            if (Convert.ToBoolean(folderDialog.ShowDialog()))
                SongFolder.Text = folderDialog.SelectedPath;
        }

        private void Forbidden_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                ForbiddenFileBox.Text = fileDialog.FileName;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            InsertData();
        }

        private void Prefix_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Preview.Text = UpdateExample();
        }

        private void SpaceSign_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Preview.Text = UpdateExample();
        }

        private void PresNull_OnChecked(object sender, RoutedEventArgs e)
        {
            Preview.Text = UpdateExample();
        }

        private void PresNull_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Preview.Text = UpdateExample();
        }

        private void Extension_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Preview.Text = UpdateExample();
        }

        private void CheckForbiddenBox_OnChecked(object sender, RoutedEventArgs e)
        {
            ForbiddenFileBox.IsEnabled = true;
            ForbiddenFileOpen.IsEnabled = true;
        }

        private void CheckForbiddenBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ForbiddenFileBox.IsEnabled = false;
            ForbiddenFileOpen.IsEnabled = false;
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            About about = new About {Owner = this};
            about.ShowDialog();
        }
    }
}