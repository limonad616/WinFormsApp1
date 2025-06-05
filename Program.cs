using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp1
{
    internal partial class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}