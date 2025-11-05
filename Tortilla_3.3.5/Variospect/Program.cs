using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Variospect
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);

            //var exists = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;

            //if (!exists)
            //{

            //   Application.Run(new POYRAI_splitScreen());
               Application.Run(new Form1());

            //}
            //else
            //    MessageBox.Show("Uygulama Halihazırda Çalışıyor. Tekrar Çalıştırılamaz!", Application.ProductName + " Program Aktif", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
