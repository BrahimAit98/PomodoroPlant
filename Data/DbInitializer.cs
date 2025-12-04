using Microsoft.Data.Sqlite;

namespace PomodoroPlant.Data
{
    public static class DbInitializer
    {
        private static readonly object _lock = new object();
        private static bool _initialized = false;
        public const string ConnectionString = "Data Source=PomodoroPlant.db";

        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();

                // Set timeout and enable WAL mode
                using (var timeoutCmd = new SqliteCommand("PRAGMA busy_timeout = 5000;", conn))
                {
                    timeoutCmd.ExecuteNonQuery();
                }

                using (var walCmd = new SqliteCommand("PRAGMA journal_mode=WAL;", conn))
                {
                    walCmd.ExecuteNonQuery();
                }

                // Create Users table
                var createUsersTable =
                    @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId         INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name           TEXT NOT NULL UNIQUE,
                        Email          TEXT NOT NULL UNIQUE,
                        HashedPassword TEXT NOT NULL,
                        ArduinoId      INTEGER NOT NULL DEFAULT 0,
                        CreatedAt      TEXT NOT NULL,
                        Role           TEXT NOT NULL DEFAULT 'user'
                    );
                ";
                using (var cmd = new SqliteCommand(createUsersTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Create Sessions table
                var createSessionsTable =
                    @"
                    CREATE TABLE IF NOT EXISTS Sessions (
                        SessionId       INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId          INTEGER NOT NULL,
                        Mode            TEXT NOT NULL,
                        DurationSeconds INTEGER NOT NULL,
                        CompletedAt     TEXT NOT NULL
                    );
                ";
                using (var cmd = new SqliteCommand(createSessionsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Add timer settings columns if they don't exist
                AddColumnIfNotExists(conn, "Users", "FocusDuration", "INTEGER DEFAULT 25");
                AddColumnIfNotExists(conn, "Users", "ShortBreak", "INTEGER DEFAULT 5");
                AddColumnIfNotExists(conn, "Users", "LongBreak", "INTEGER DEFAULT 15");
                AddColumnIfNotExists(conn, "Users", "SessionsUntilLongBreak", "INTEGER DEFAULT 4");
                AddColumnIfNotExists(conn, "Users", "AutoStartBreaks", "INTEGER DEFAULT 0");

                _initialized = true;
                Console.WriteLine("Database initialized successfully!");
            }
        }

        private static void AddColumnIfNotExists(
            SqliteConnection conn,
            string tableName,
            string columnName,
            string columnDefinition
        )
        {
            try
            {
                var sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
                Console.WriteLine($"Added column {columnName} to {tableName}");
            }
            catch (SqliteException)
            {
                // Column already exists, ignore
            }
        }
    }
}
