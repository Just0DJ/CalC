using System;
using System.Windows.Forms;
using TitanCalc.UI;

namespace TitanCalc
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Cold start working set trim
            Win32Dwm.TrimProcessMemory();

            Application.Run(new MainForm());
        }
    }
}
