using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PomodoroPlant.Data;

namespace PomodoroPlant.Models
{
    public class UserRepository
    {
        private readonly string _connectionString = "Data Source=PomodoroPlant.db";

        // ---- CREATE USER ---------------------------------------
        public async Task CreateAsync(User user)
        {
            const string sql =
                @"
                INSERT INTO Users (Name, Email, HashedPassword, ArduinoId, CreatedAt, Role)
                VALUES ($Name, $Email, $HashedPassword, $ArduinoId, $CreatedAt, $Role);
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);

            cmd.Parameters.AddWithValue("$Name", user.Name);
            cmd.Parameters.AddWithValue("$Email", user.Email);
            cmd.Parameters.AddWithValue("$HashedPassword", user.PasswordHash);
            cmd.Parameters.AddWithValue("$ArduinoId", 0); // Default value
            cmd.Parameters.AddWithValue("$CreatedAt", user.CreatedAt); // Let SQLite handle DateTime directly
            cmd.Parameters.AddWithValue("$Role", "user"); // Default role

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ---- UPDATE USER ---------------------------------------
        public async Task UpdateAsync(User user)
        {
            const string sql =
                @"
                UPDATE Users 
                SET Name = $Name,
                    Email = $Email,
                    HashedPassword = $HashedPassword
                WHERE UserId = $UserId;
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);

            cmd.Parameters.AddWithValue("$UserId", user.UserId);
            cmd.Parameters.AddWithValue("$Name", user.Name);
            cmd.Parameters.AddWithValue("$Email", user.Email);
            cmd.Parameters.AddWithValue("$HashedPassword", user.PasswordHash);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            using var connection = new SqliteConnection(DbInitializer.ConnectionString);
            await connection.OpenAsync();

            var sql =
                @"SELECT UserId, Name, Email, HashedPassword, CreatedAt, 
                        FocusDuration, ShortBreak, LongBreak, SessionsUntilLongBreak, AutoStartBreaks 
                        FROM Users WHERE UserId = @userId";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new User { UserId = reader.GetInt32(0) };

                user.Name = reader.GetString(1);
                user.Email = reader.GetString(2);
                user.PasswordHash = reader.GetString(3);

                // Handle CreatedAt safely
                try
                {
                    user.CreatedAt = reader.GetDateTime(4);
                }
                catch
                {
                    user.CreatedAt = DateTime.UtcNow;
                }

                // Timer settings
                user.FocusDuration = reader.IsDBNull(5) ? 25 : reader.GetInt32(5);
                user.ShortBreak = reader.IsDBNull(6) ? 5 : reader.GetInt32(6);
                user.LongBreak = reader.IsDBNull(7) ? 15 : reader.GetInt32(7);
                user.SessionsUntilLongBreak = reader.IsDBNull(8) ? 4 : reader.GetInt32(8);
                user.AutoStartBreaks = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1; // Convert int to bool

                return user;
            }

            return null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = new SqliteConnection(DbInitializer.ConnectionString);
            await connection.OpenAsync();

            var sql =
                @"SELECT UserId, Name, Email, HashedPassword, CreatedAt, 
                        FocusDuration, ShortBreak, LongBreak, SessionsUntilLongBreak, AutoStartBreaks 
                        FROM Users WHERE Email = @email";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new User { UserId = reader.GetInt32(0) };

                user.Name = reader.GetString(1);
                user.Email = reader.GetString(2);
                user.PasswordHash = reader.GetString(3);

                // Handle CreatedAt safely
                try
                {
                    user.CreatedAt = reader.GetDateTime(4);
                }
                catch
                {
                    user.CreatedAt = DateTime.UtcNow;
                }

                // Timer settings
                user.FocusDuration = reader.IsDBNull(5) ? 25 : reader.GetInt32(5);
                user.ShortBreak = reader.IsDBNull(6) ? 5 : reader.GetInt32(6);
                user.LongBreak = reader.IsDBNull(7) ? 15 : reader.GetInt32(7);
                user.SessionsUntilLongBreak = reader.IsDBNull(8) ? 4 : reader.GetInt32(8);
                user.AutoStartBreaks = reader.IsDBNull(9) ? false : reader.GetInt32(9) == 1; // Convert int to bool

                return user;
            }

            return null;
        }

        public async Task UpdateTimerSettingsAsync(
            int userId,
            int focusDuration,
            int shortBreak,
            int longBreak,
            int sessionsUntilLongBreak,
            bool autoStartBreaks
        )
        {
            using var connection = new SqliteConnection(DbInitializer.ConnectionString);
            await connection.OpenAsync();

            var sql =
                @"UPDATE Users 
                        SET FocusDuration = @focusDuration,
                            ShortBreak = @shortBreak,
                            LongBreak = @longBreak,
                            SessionsUntilLongBreak = @sessionsUntilLongBreak,
                            AutoStartBreaks = @autoStartBreaks
                        WHERE UserId = @userId";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@focusDuration", focusDuration);
            command.Parameters.AddWithValue("@shortBreak", shortBreak);
            command.Parameters.AddWithValue("@longBreak", longBreak);
            command.Parameters.AddWithValue("@sessionsUntilLongBreak", sessionsUntilLongBreak);
            command.Parameters.AddWithValue("@autoStartBreaks", autoStartBreaks ? 1 : 0);

            await command.ExecuteNonQueryAsync();
        }
    }
}
