using ConcesionaroCarros.Properties;

namespace ConcesionaroCarros.Services
{
    internal static class LocalizedText
    {
        public static string Get(string key, string fallback = null)
        {
            return LocalizationService.Instance.GetString(key, fallback);
        }

        public static string GetRoleDisplay(string roleCode)
        {
            switch (roleCode)
            {
                case RolesSistema.Administrador:
                    return Get("Role_Administrador", roleCode);
                case RolesSistema.RRHH:
                    return Get("Role_RRHH", roleCode);
                case RolesSistema.Produccion:
                    return Get("Role_Produccion", roleCode);
                case RolesSistema.Industrial:
                    return Get("Role_Industrial", roleCode);
                case RolesSistema.Almacen:
                    return Get("Role_Almacen", roleCode);
                case RolesSistema.PCP:
                    return Get("Role_PCP", roleCode);
                case RolesSistema.Ventas:
                    return Get("Role_Ventas", roleCode);
                case RolesSistema.Despachos:
                    return Get("Role_Despachos", roleCode);
                case RolesSistema.Ingenieria:
                    return Get("Role_Ingenieria", roleCode);
                case RolesSistema.Gerencia:
                    return Get("Role_Gerencia", roleCode);
                case RolesSistema.Compras:
                    return Get("Role_Compras", roleCode);
                case RolesSistema.Contratos:
                    return Get("Role_Contratos", roleCode);
                case RolesSistema.SST:
                    return Get("Role_SST", roleCode);
                case RolesSistema.Marketing:
                    return Get("Role_Marketing", roleCode);
                case RolesSistema.SistemasTI:
                    return Get("Role_SistemasTI", roleCode);
                case RolesSistema.Calidad:
                    return Get("Role_Calidad", roleCode);
                default:
                    return roleCode ?? string.Empty;
            }
        }

        public static string GetFolderDisplay(string folderCode)
        {
            switch (folderCode)
            {
                case ViewModels.FormularioInstaladorViewModel.CarpetaPuntoLocal:
                    return Get("InstallerForm_LocalFolderOption", folderCode);
                case ViewModels.FormularioInstaladorViewModel.CarpetaDesarrolloGlobal:
                    return Get("InstallerForm_GlobalFolderOption", folderCode);
                default:
                    return folderCode ?? string.Empty;
            }
        }
    }
}
