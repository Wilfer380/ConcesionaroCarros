using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        public IReadOnlyList<DeveloperAccountRow> ListAll()
        {
            var rows = new List<DeveloperAccountRow>();

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
SELECT Id, Email, Enabled, CreatedAt, CreatedBy, Notes
FROM DeveloperAccount
ORDER BY UPPER(TRIM(Email));";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new DeveloperAccountRow
                        {
                            Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                            Email = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Enabled = !reader.IsDBNull(2) && reader.GetInt64(2) != 0,
                            CreatedAt = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            CreatedBy = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Notes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                        });
                    }
                }
            }

            return rows;
        }

        public IReadOnlyCollection<string> ListEnabledEmails()
        {
            return ListAll()
                .Where(x => x != null && x.Enabled && !string.IsNullOrWhiteSpace(x.Email))
                .Select(x => NormalizeEmail(x.Email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public void AddOrEnable(string email, string createdBy, string notes)
        {
            var safeEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(safeEmail))
                throw new InvalidOperationException("Email requerido.");

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
UPDATE DeveloperAccount
SET Email = TRIM($e), Enabled = 1, Notes = COALESCE($notes, Notes)
WHERE UPPER(TRIM(Email)) = UPPER(TRIM($e));";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                cmd.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes.Trim());

                if (cmd.ExecuteNonQuery() > 0)
                    return;

                cmd.Parameters.Clear();
                cmd.CommandText = @"
INSERT INTO DeveloperAccount (Email, Enabled, CreatedAt, CreatedBy, Notes)
VALUES (TRIM($e), 1, $ts, $by, $notes);";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$by", (createdBy ?? string.Empty).Trim());
                cmd.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes.Trim());
                cmd.ExecuteNonQuery();
            }
        }

        public void Disable(string email)
        {
            var safeEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(safeEmail))
                throw new InvalidOperationException("Email requerido.");

            using (var connection = new SqliteConnection(DatabaseInitializer.ConnectionString))
            using (var cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandText = @"
UPDATE DeveloperAccount
SET Enabled = 0
WHERE UPPER(TRIM(Email)) = UPPER(TRIM($e));";
                cmd.Parameters.AddWithValue("$e", safeEmail);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public sealed class DeveloperAccountRow
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public bool Enabled { get; set; }
        public string CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Notes { get; set; }
    }
}
