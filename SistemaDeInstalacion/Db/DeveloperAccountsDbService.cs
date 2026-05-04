using Microsoft.Data.Sqlite;

namespace ConcesionaroCarros.Db
{
    public sealed class DeveloperAccountsDbService
    {
        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        public bool IsReservedEmail(string email)
        {
            var safeEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(safeEmail))
                return false;

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
SELECT 1
FROM DeveloperAccount
WHERE UPPER(TRIM(Email)) = UPPER(TRIM($e))
LIMIT 1;";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        public bool IsDeveloperEmail(string email)
        {
            var safeEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(safeEmail))
                return false;

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
SELECT 1
FROM DeveloperAccount
WHERE Enabled = 1 AND UPPER(TRIM(Email)) = UPPER(TRIM($e))
LIMIT 1;";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        public bool IsDisabledDeveloper(string email)
        {
            var safeEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(safeEmail))
                return false;

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
SELECT 1
FROM DeveloperAccount
WHERE Enabled = 0 AND UPPER(TRIM(Email)) = UPPER(TRIM($e))
LIMIT 1;";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}
