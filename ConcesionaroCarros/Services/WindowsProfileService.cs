using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ConcesionaroCarros.Services
{
    public static class WindowsProfileService
    {
        private const int NameDisplay = 3;
        private const int NameUserPrincipal = 8;

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserNameEx(
            int nameFormat,
            StringBuilder userName,
            ref int userNameSize);

        public static string ObtenerNombreVisible()
        {
            return ObtenerValorUsuario(NameDisplay);
        }

        public static string ObtenerCorreoPrincipal()
        {
            var valor = ObtenerValorUsuario(NameUserPrincipal);
            return string.IsNullOrWhiteSpace(valor) || !valor.Contains("@")
                ? string.Empty
                : valor.Trim();
        }

        private static string ObtenerValorUsuario(int nameFormat)
        {
            try
            {
                var size = 0;
                GetUserNameEx(nameFormat, null, ref size);

                if (size <= 0)
                    return string.Empty;

                var buffer = new StringBuilder(size);
                if (!GetUserNameEx(nameFormat, buffer, ref size))
                    return string.Empty;

                return (buffer.ToString() ?? string.Empty).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
