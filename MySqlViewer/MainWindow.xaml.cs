using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EZX.MySql.Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string QueryTableName = "results";

        public const string ConnectionString = "Server={0};Port={1};Database={2};Uid={3};Pwd={4};Allow Zero Datetime=true;";
        private DataTable dataTable = new DataTable(QueryTableName);
        private string lastQuery;
        private readonly List<View> openViewWindows = new List<View>();

        public DataView QueryResults
        {
            get { return dataTable.DefaultView; }
        }

        public string WindowTitle
        {
            get { return Application.Current.FindResource("WindowTitle") + " v" + AppVersion; }
        }

        public string AppVersion
        {
            get
            {
                String version = Assembly.GetExecutingAssembly().GetName().Version.Major + "."
                    + Assembly.GetExecutingAssembly().GetName().Version.Minor + "."
                 + Assembly.GetExecutingAssembly().GetName().Version.Build;
                return version;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            GrdQuery.ItemsSource = QueryResults;
            LoadFromConfig();
        }

        private void LoadFromConfig()
        {
            TxtHost.Text = ConfigurationManager.AppSettings["host"];
            TxtPort.Text = ConfigurationManager.AppSettings["port"];
            TxtUser.Text = ConfigurationManager.AppSettings["user"];
            TxtPassword.Password = ConfigurationManager.AppSettings["password"];
            TxtDatabase.Text = ConfigurationManager.AppSettings["schema"];
            Height = int.Parse(ConfigurationManager.AppSettings["height"]);
            Width = int.Parse(ConfigurationManager.AppSettings["width"]);
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //            GrdQuery.ItemsSource = null;
                ResetForNewQuery();
                StatusText.Text = "running query....";
                GrdQuery.ItemsSource = null;
                using (var cn = new MySqlConnection(GetConnectionString()))
                {
                    cn.Open();
                    var cmd = cn.CreateCommand();
                    cmd.CommandText = TxtQuery.Text;

                    var adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dataTable);
                    GrdQuery.ItemsSource = dataTable.DefaultView;
                    DisplayRowCount(dataTable.Rows.Count);
                    lastQuery = TxtQuery.Text;
                    UpdateWindowTitle(lastQuery);
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("Sorry charlie, but you had an Exception!" + e);

                DisplayError(ex);
                lastQuery = null;
                dataTable = new DataTable(QueryTableName); // assume table is now hosed
            }
        }

        private void UpdateWindowTitle(string additionalText)
        {
            Title = WindowTitle + ": " + additionalText;
        } 

        private void DisplayError(Exception e)
        {
            StatusText.Text = "Sorry Charlie, but you had an Exception!\r\n" + e;
            Debug.Print("Exception=" + e);
            Console.WriteLine(@"(Console) Exception=" + e);
        }

        private string GetConnectionString()
        {
            var conString = string.Format(ConnectionString, TxtHost.Text, TxtPort.Text, TxtDatabase.Text, TxtUser.Text,
                TxtPassword.Password);
            return conString;
        }

        private void ResetForNewQuery()
        {
            // we changed the query - so assume that we can't refresh open views and need to clear the columns
            if (lastQuery != null && !lastQuery.Equals(TxtQuery.Text))
            {
                // close any open view windows
                openViewWindows.ForEach(v => v.Close());
                openViewWindows.Clear();
                dataTable.Columns.Clear();
                dataTable.DefaultView.RowFilter = null;
                DimFilterText();
            }
            dataTable.Clear();
        }

        private void DisplayRowCount(int count)
        {
            StatusText.Text = "record count = " + count;
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            TxtFilter.Foreground = new SolidColorBrush(Colors.Black);
            var filter = TxtFilter.Text;
            if (string.IsNullOrEmpty(filter))
            {
                return;
            }

            try
            {
                dataTable.DefaultView.RowFilter = filter;
                DisplayRowCount(dataTable.DefaultView.Count);
            }
            catch (Exception ex)
            {
                StatusText.Text = ex.Message;
            }
        }

        private void BtnNewView_Click(object sender, RoutedEventArgs e)
        {
            TxtFilter.Foreground = new SolidColorBrush(Colors.Black);
            var filter = TxtFilter.Text;
            if (string.IsNullOrEmpty(filter))
            {
                return;
            }
            View view = null;
            try
            {

                DimFilterText();
                var dv = new DataView(dataTable);
                dv.RowFilter = filter;
                view = new View(dv, filter);
                openViewWindows.Add(view);
                view.Show();
            }
            catch (Exception ex)
            {
                DisplayError(ex);
                if (view != null)
                {
                    view.Close();
                }
            }

        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            dataTable.DefaultView.RowFilter = null;
            DimFilterText();
            DisplayRowCount(dataTable.Rows.Count);
        }

        private void DimFilterText()
        {
            TxtFilter.Foreground = new SolidColorBrush(Colors.Gray);
        }


        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            var config =
                     ConfigurationManager.OpenExeConfiguration
                                (ConfigurationUserLevel.None);

            config.AppSettings.Settings["host"].Value = TxtHost.Text;
            config.AppSettings.Settings["port"].Value = TxtPort.Text;
            config.AppSettings.Settings["user"].Value = TxtUser.Text;
            config.AppSettings.Settings["password"].Value = TxtPassword.Password;
            config.AppSettings.Settings["schema"].Value = TxtDatabase.Text;
            config.AppSettings.Settings["height"].Value = Height.ToString(CultureInfo.CurrentCulture);
            config.AppSettings.Settings["width"].Value = Width.ToString(CultureInfo.CurrentCulture);

            config.Save();
        }
    }
}
