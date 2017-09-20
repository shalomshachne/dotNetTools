using System;
using System.Data;
using System.Windows;

namespace EZX.MySql.Viewer
{
    /// <summary>
    /// Interaction logic for View.xaml
    /// </summary>
    public partial class View : Window
    {
        private DataView dv;

        public View(DataView dv, string filter)
        {
            InitializeComponent();
            this.dv = dv;
            this.gridData.ItemsSource = dv;
            this.Title = filter;
            DisplayRowCount(dv.Count);
        }

        private void DisplayRowCount(int count)
        {
            StatusText.Text = "record count = " + count;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            dv.Dispose();
        }
    }
}
