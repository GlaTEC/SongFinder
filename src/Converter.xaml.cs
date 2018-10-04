using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace SongFinder
{
    public enum SongBook
    {
        Ghs = 0,
        Wlg = 1,
        Lq = 2
    }

    public partial class Converter
    {
        private bool _updateAllowed;

        public Converter()
        {
            InitializeComponent();
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => { _updateAllowed = true; }));
        }

        /// <summary>
        /// Function to convert GHS SongNumber to WLG/LQ SongNumbers and back
        /// </summary>
        /// <param name="songNumber">The SongNumber</param>
        /// <param name="strippedDown">Returns the Result wihout additional Text</param>
        /// <param name="choosenSongBook"></param>
        /// <returns>Tuple: String1=WLG/LQ SongNumber String2=SongBook</returns>
        public static Tuple<string, string> ConvertSong(string songNumber, bool strippedDown, SongBook choosenSongBook)
        {
            try
            {
                int songNumberInt;
                int.TryParse(songNumber, out songNumberInt);

                if (songNumberInt > 694)
                {
                    MessageBox.Show(songNumber + " ist keine gültige Liednummer", "Fehler!", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                    return new Tuple<string, string>("big", "");
                }


                object result = "";
                string songBook = "";

                var database =
                    new SQLiteConnection("Data Source=songs.db;Version=3;");
                database.Open();

                var cmdGhsWlg = "SELECT wlg FROM wlg WHERE ghs=\"" + songNumber + "\";";
                var cmdGhsLq = "SELECT lq FROM lq WHERE ghs=\"" + songNumber + "\";";
                var cmdWlgGhs = "SELECT ghs FROM wlg WHERE wlg=\"" + songNumber + "\";";
                var cmdLqGhs = "SELECT ghs FROM lq WHERE lq=\"" + songNumber + "\";";


                switch (choosenSongBook)
                {
                    case SongBook.Ghs:
                        SQLiteCommand commandGhsWlg = new SQLiteCommand(cmdGhsWlg, database);
                        SQLiteDataReader readerGhsWlg = commandGhsWlg.ExecuteReader();
                        while (readerGhsWlg.Read())
                        {
                            songBook = "WLG";
                            if (strippedDown)
                            {
                                result = readerGhsWlg["wlg"];
                            }
                            else
                            {
                                result = "WLG: " + readerGhsWlg["wlg"];
                            }
                        }


                        if (String.IsNullOrEmpty(result.ToString()))
                        {
                            SQLiteCommand commandGhsLq = new SQLiteCommand(cmdGhsLq, database);
                            SQLiteDataReader readerGhsLq = commandGhsLq.ExecuteReader();
                            while (readerGhsLq.Read())
                            {
                                songBook = "LQ";
                                if (strippedDown)
                                {
                                    result = readerGhsLq["lq"];
                                }
                                else
                                {
                                    result = "LQ: " + readerGhsLq["lq"];
                                }
                            }
                        }

                        break;

                    case SongBook.Wlg:
                        SQLiteCommand commandWlgGhs = new SQLiteCommand(cmdWlgGhs, database);
                        SQLiteDataReader readerWlgGhs = commandWlgGhs.ExecuteReader();

                        while (readerWlgGhs.Read())
                        {
                            songBook = "WLG";
                            if (strippedDown)
                            {
                                result = readerWlgGhs["ghs"];
                            }
                            else
                            {
                                result = "GHS: " + readerWlgGhs["ghs"];
                            }
                        }

                        break;

                    case SongBook.Lq:
                        SQLiteCommand commandLqGhs = new SQLiteCommand(cmdLqGhs, database);
                        SQLiteDataReader readerLqGhs = commandLqGhs.ExecuteReader();

                        while (readerLqGhs.Read())
                        {
                            songBook = "WLG";
                            if (strippedDown)
                            {
                                result = readerLqGhs["ghs"];
                            }
                            else
                            {
                                result = "GHS: " + readerLqGhs["ghs"];
                            }
                        }

                        break;
                }


                if (String.IsNullOrEmpty(result.ToString()))
                {
                    result = strippedDown ? "" : "Lied nicht in der Liste";
                }

                return new Tuple<string, string>(result.ToString(), songBook);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                throw;
            }
        }

        private void ConvBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var conversion = ConvertSong(SongConvertBox.Text, false, (SongBook) SongBookBox.SelectedIndex);
                if (conversion.Item1 == "big")
                {
                    SongConvertBox.Text = "694";
                }
                else
                {
                    Result.Content = conversion.Item1;
                }
            }
        }

        private void SongConvertBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
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

        private void SongConvertBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SongConvertBox.Text))
            {
                var conversion = ConvertSong(SongConvertBox.Text, false, (SongBook) SongBookBox.SelectedIndex);
                if (conversion.Item1 == "big")
                {
                    SongConvertBox.Text = "694";
                }
                else
                {
                    Result.Content = conversion.Item1;
                }
            }
            else
            {
                Result.Content = "?";
            }
        }

        private void SongBookBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updateAllowed)
            {
                if (!string.IsNullOrWhiteSpace(SongConvertBox.Text))
                {
                    var conversion = ConvertSong(SongConvertBox.Text, false, (SongBook) SongBookBox.SelectedIndex);
                    if (conversion.Item1 == "big")
                    {
                        SongConvertBox.Text = "694";
                    }
                    else
                    {
                        Result.Content = conversion.Item1;
                    }
                }
                else
                {
                    Result.Content = "?";
                }
            }
        }
    }
}