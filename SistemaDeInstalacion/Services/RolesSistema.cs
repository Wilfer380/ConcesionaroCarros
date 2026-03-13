using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcesionaroCarros.Services
{
    public static class RolesSistema
    {
        public const string Administrador = "ADMINISTRADOR";
        public const string RRHH = "RRHH";
        public const string Produccion = "PRODUCCION";
        public const string Industrial = "INDUSTRIAL";
        public const string Almacen = "ALMACEN";
        public const string PCP = "PCP";
        public const string Ventas = "VENTAS";
        public const string Despachos = "DESPACHOS";
        public const string Ingenieria = "INGENIERIA";
        public const string Gerencia = "GERENCIA";
        public const string Compras = "COMPRAS";
        public const string Contratos = "CONTRATOS";
        public const string SST = "SST";
        public const string Marketing = "MARKETING";
        public const string SistemasTI = "SISTEMAS (TI)";
        public const string Calidad = "CALIDAD";

        public static readonly IReadOnlyList<string> Todos = new[]
        {
            Administrador,
            RRHH,
            Produccion,
            Industrial,
            Almacen,
            PCP,
            Ventas,
            Despachos,
            Ingenieria,
            Gerencia,
            Compras,
            Contratos,
            SST,
            Marketing,
            SistemasTI,
            Calidad
        };

        public static readonly IReadOnlyList<string> AsignablesSinAdmin =
            Todos.Where(r => !EsAdministrador(r)).ToList();

        public static bool EsAdministrador(string rol)
        {
            return string.Equals(rol, Administrador, StringComparison.OrdinalIgnoreCase);
        }
    }
}
