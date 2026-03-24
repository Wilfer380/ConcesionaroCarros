using System;
using System.Windows.Forms;

namespace LauncherSistema
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LauncherService.Ejecutar(args);
        }
    }
}
