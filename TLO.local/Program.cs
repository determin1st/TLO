using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TLO.local
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                var settings = Settings.Current;
                var categories = ClientLocalDB.Current.GetCategoriesEnable();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }
    }
}
