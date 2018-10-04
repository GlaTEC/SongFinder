using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using AutoUpdaterDotNET;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;


namespace SongFinder
{
    public class Functions
    {
        public event EventHandler ResetSongNumber;

        public delegate void DCheckSongNumber(string songNumber);

        public event DCheckSongNumber CheckSongNumber;

        private static Functions _instance;

        private Functions()
        {
        }

        public static Functions Instance
        {
            get { return _instance ?? (_instance = new Functions()); }
        }

        /// <summary>
        /// Checks if a pressed key is valid
        /// </summary>
        /// <param name="text">The key</param>
        /// <returns>Boolean if pressed key is valid</returns>
        public static bool IsTextAllowed(Key text)
        {
            try
            {
                return (text >= Key.D0 && text <= Key.D9) ||
                       (text >= Key.NumPad0 && text <= Key.NumPad9) ||
                       (text == Key.Back) ||
                       (text == Key.Delete) ||
                       (text == Key.Left) ||
                       (text == Key.Right) ||
                       (text == Key.Return) ||
                       (text == Key.System);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }


        /// <summary>
        /// Do operations on giving number according to the Songbook
        /// </summary>
        /// <param name="songNumber">The number of the song</param>
        /// <param name="fill">Bool if fill SongNumber with leading zeros if the length of SongNumber has less than 3 digits</param>
        /// <returns>String with 3 digits or if an error occurs an empty string</returns>
        public string NumberOperations(string songNumber, bool fill)
        {
            try
            {
                int songNumberInt;
                bool isNumber = int.TryParse(songNumber, out songNumberInt);

                if (fill)
                {
                    switch (songNumber.Length)
                    {
                        case 2:
                            return "0" + songNumber;
                        case 1:
                            return "00" + songNumber;
                        default:
                            return songNumber;
                    }
                }

                if (!isNumber)
                {
                    if (CheckSongNumber != null)
                    {
                        CheckSongNumber(songNumber);
                        return "";
                    }
                }

                if (songNumberInt <= 694) return songNumber;

                MessageBox.Show(songNumber + " ist keine gültige Liednummer", "Fehler!", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                if (ResetSongNumber != null)
                {
                    ResetSongNumber(this, new EventArgs());
                }

                return "";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Checks if SongNumber appears in the ForbiddenCSV file
        /// </summary>
        /// <param name="songNumber">The number of the song</param>
        /// <returns>Boolean whether song is allowed or not</returns>
        public static bool Forbidden(string songNumber)
        {
            try
            {
                string songNumberProcessed = _instance.NumberOperations(songNumber, true);
                var csvPath = Settings.GetConfigValue("ForbiddenCSV");
                if (!File.Exists(csvPath))
                {
                    return false;
                }

                string csvContent = File.ReadAllText(csvPath);
                return csvContent.Split(',').Any(x => x == songNumberProcessed);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Generates a file name with the parameters set in the settings
        /// </summary>
        /// <param name="songNumber">The number of the song</param>
        /// <param name="approved">Currently for internal use only</param>
        /// <returns>The generated filename as string</returns>
        public static string GenerateFilename(string songNumber, bool approved = false)
        {
            try
            {
                bool preserveNull;
                bool.TryParse(Settings.GetConfigValue("PreserveNull"), out preserveNull);


                string songNumberProcessed = preserveNull
                    ? Instance.NumberOperations(songNumber, true)
                    : Instance.NumberOperations(songNumber, false);


                string spaceSign;
                switch (Settings.GetConfigValue("SpaceSign"))
                {
                    case "0":
                        spaceSign = " ";
                        break;
                    case "1":
                        spaceSign = "_";
                        break;
                    case "2":
                        spaceSign = "-";
                        break;
                    default:
                        spaceSign = null;
                        break;
                }

                if (approved)
                {
                    return "GHS" + spaceSign + songNumberProcessed + "_approved" +
                           Settings.GetConfigValue("FileExtension");
                }

                return "GHS" + spaceSign + songNumberProcessed +
                       Settings.GetConfigValue("FileExtension");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Opens the song presentation file
        /// </summary>
        /// <param name="songNumber">The number of the song</param>
        public static void OpenFile(string songNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(songNumber))
                {
                    return;
                }

                string songFolder = Settings.GetConfigValue("SongFolder");

                string songData = GenerateFilename(songNumber);
                string songDataApproved = GenerateFilename(songNumber, true);

                if (File.Exists(Path.Combine(songFolder, songData)))
                {
                    Process.Start(Path.Combine(songFolder, songData));
                }
                else if (File.Exists(Path.Combine(songFolder, songDataApproved)))
                {
                    Process.Start(Path.Combine(songFolder, songDataApproved));
                }
                else
                {
                    Tuple<string, string> rawConv = Converter.ConvertSong(songNumber, true, SongBook.Ghs);
                    string convMessage;
                    if (string.IsNullOrWhiteSpace(rawConv.Item1))
                    {
                        convMessage = " Außerdem ist es nicht im WLG oder LQ zu finden.";
                    }
                    else
                    {
                        convMessage = " Jedoch entspricht der Text des Liedes " + rawConv.Item1 +
                                      " aus dem Liederbuch " +
                                      rawConv.Item2 + " diesem.";
                    }

                    MessageBox.Show("Das Lied " + songNumber + " wurde noch nicht erstellt." + convMessage,
                        "Lied nicht vorhanden", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }
    }

    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            try
            {
                Settings.ConfigPreparation();
                InitializeComponent();

                Functions.Instance.ResetSongNumber += (sender, args) => ResetSongNumber();
                Functions.Instance.CheckSongNumber += CheckSongNumber;
                
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.Mandatory = true;
                AutoUpdater.OpenDownloadPage = true;
                AutoUpdater.Start("https://raw.githubusercontent.com/GlaTEC/SongFinder/master/updates.xml");
                AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Function to implement a custom message for a update
        /// </summary>
        /// <param name="args">Arguments of the event</param>
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null && args.IsUpdateAvailable)
            {
                MetroDialogOptions.AffirmativeButtonText = "Ja";
                MetroDialogOptions.NegativeButtonText = "Nein";
                var result = this.ShowModalMessageExternal(
                    @"Update",
                    @"Sie benutzen die Version " + args.InstalledVersion.Major + "." +
                    args.InstalledVersion.Minor +
                    " Möchten Sie auf Version " + args.CurrentVersion.Major + "." + args.CurrentVersion.Minor +
                    " updaten?",
                    MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Affirmative)
                {
                    Process.Start("https://github.com/GlaTEC/SongFinder/releases");
                    Application.Current.Shutdown();
                }
                else
                {
                    MetroDialogOptions.AffirmativeButtonText = "Ok";
                }
            }
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            bool stayOnTop;
            bool.TryParse(Settings.GetConfigValue("StayOnTop"), out stayOnTop);

            if (stayOnTop)
            {
                Window window = (Window) sender;
                window.Topmost = true;
            }
        }

        private void ResetSongNumber()
        {
            SongBox.Text = "694";
        }

        private void CheckSongNumber(string songNumber)
        {
            if (!string.IsNullOrEmpty(SongBox.Text))
            {
                MessageBox.Show(songNumber + " ist keine Lied Nummer");
            }
        }

        private void SongBox_Changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                bool checkForbidden;
                bool.TryParse(Settings.GetConfigValue("CheckForbidden"), out checkForbidden);
                string songFolder = Settings.GetConfigValue("SongFolder");
                string songNumberProcessed = Functions.Instance.NumberOperations(SongBox.Text, false);
                string songData = Functions.GenerateFilename(songNumberProcessed);
                string songDataApproved = Functions.GenerateFilename(songNumberProcessed, true);

                if (checkForbidden)
                {
                    if (!Functions.Forbidden(SongBox.Text))
                    {
                        SongCheck1.Content = "Erlaubt";
                        SongCheck1.Foreground = Brushes.Green;
                    }
                    else
                    {
                        SongCheck1.Content = "Verboten";
                        SongCheck1.Foreground = Brushes.Red;
                    }
                }

                if (File.Exists(Path.Combine(songFolder, songData)) ||
                    File.Exists(Path.Combine(songFolder, songDataApproved)))
                {
                    SongExists.Content = "Ja";
                    SongExists.Foreground = Brushes.Green;
                }
                else
                {
                    SongExists.Content = "Nein";
                    SongExists.Foreground = Brushes.Red;
                }

                if (string.IsNullOrWhiteSpace(SongBox.Text))
                {
                    SongCheck1.Content = "?";
                    SongCheck1.Foreground = Brushes.Black;
                    SongExists.Content = "?";
                    SongExists.Foreground = Brushes.Black;
                    SongBookPreview.Content = "";
                }
                else
                {
                    Tuple<string, string> rawConv = Converter.ConvertSong(SongBox.Text, false, SongBook.Ghs);
                    SongBookPreview.Content = rawConv.Item1;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                throw;
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Functions.OpenFile(SongBox.Text);
                SongBox.Text = "";
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                throw;
            }
        }

        private void SongBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!Functions.IsTextAllowed(e.Key))
                {
                    e.Handled = true;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
                throw;
            }
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings {Owner = this};
            settings.ShowDialog();
        }

        private void SongBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Functions.OpenFile(SongBox.Text);
                SongBox.Text = "";
            }
        }

        private void Converter_OnClick(object sender, RoutedEventArgs e)
        {
            Converter converter = new Converter {Owner = this};
            converter.ShowDialog();
        }

        private void MainWindow_OnContentRendered(object sender, EventArgs e)
        {
            bool checkForbidden;
            bool.TryParse(Settings.GetConfigValue("CheckForbidden"), out checkForbidden);
            if (checkForbidden)
            {
                if (File.Exists(Settings.GetConfigValue("ForbiddenCSV")))
                {
                    if (!Settings.CheckForbiddenCsv(Settings.GetConfigValue("ForbiddenCSV")))
                    {
                        this.ShowModalMessageExternal("Fehler",
                            "Ihre Liste für unlizensierte Lieder ist fehlerhaft. Liedüberprüfung wird ausgesetzt!");
                        Settings.SettingsUpdater("CheckForbidden", "false");
                    }
                }
                else
                {
                    this.ShowModalMessageExternal("Fehler",
                        "Ihre Liste für unlizensierte Lieder ist nicht vorhanden. Liedüberprüfung wird ausgesetzt!");
                    Settings.SettingsUpdater("CheckForbidden", "false");
                }
            }

            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "songs.db")))
            {
                MessageBox.Show(
                    "Die Datei 'songs.db' existiert nicht. Bitte installieren sie das Programm erneut um diesen Fehler zu beheben!",
                    "Fataler Fehler!", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            if (!Directory.Exists(Settings.GetConfigValue("SongFolder")))
            {
                this.ShowModalMessageExternal("Fehler",
                    "Der Ordner mit den Lieddateien existiert nicht! Bitte passen Sie ihn in den Einstellungen an");
            }
        }
    }
}