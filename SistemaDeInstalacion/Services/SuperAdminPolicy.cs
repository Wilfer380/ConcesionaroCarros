using System;

namespace ConcesionaroCarros.Services
{
    public static class SuperAdminPolicy
    {
        public const string SuperAdminEmail = "superadmin@weg.net";

        public static bool IsSuperAdminEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   string.Equals(email.Trim(), SuperAdminEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
