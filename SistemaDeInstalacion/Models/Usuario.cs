using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ConcesionaroCarros.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }

        public string PasswordHash { get; set; }
        public string Rol { get; set; }
        public string RolDisplay => ResolverRolDisplay(Rol);

        public DateTime FechaRegistro { get; set; }
        public string FotoPerfil { get; set; }

        public string AplicativosJson { get; set; } = "[]";

        public string AplicativosResumen
        {
            get
            {
                var rutas = ObtenerAplicativosAsignados();
                if (rutas.Count == 0)
                    return "-";

                var nombres = rutas
                    .Select(r => Path.GetFileNameWithoutExtension(r))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList();

                return nombres.Count == 0 ? "-" : string.Join(", ", nombres);
            }
        }

        public List<string> ObtenerAplicativosAsignados()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AplicativosJson))
                    return new List<string>();

                var serializer = new JavaScriptSerializer();
                var lista = serializer.Deserialize<List<string>>(AplicativosJson);
                return lista ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void EstablecerAplicativosAsignados(IEnumerable<string> rutas)
        {
            var serializer = new JavaScriptSerializer();
            var lista = (rutas ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            AplicativosJson = serializer.Serialize(lista);
        }

        private static string ResolverRolDisplay(string rol)
        {
            var localizedTextType = typeof(Usuario).Assembly.GetType("ConcesionaroCarros.Services.LocalizedText");
            var getRoleDisplayMethod = localizedTextType?.GetMethod("GetRoleDisplay");
            if (getRoleDisplayMethod == null)
                return rol ?? string.Empty;

            return Convert.ToString(getRoleDisplayMethod.Invoke(null, new object[] { rol }))
                ?? string.Empty;
        }
    }
}
