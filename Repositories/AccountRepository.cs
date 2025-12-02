using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PomodoroPlant.Models
{
    public class UserRepository
    {
        private readonly string _connectionString = "Data Source=PomodoroPlant.db";

        public UserRepository()
        {
            EnsureDatabase();
        }

        // ---- CREATE USER ---------------------------------------
        public async Task CreateAsync(UserModel user)
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
            cmd.Parameters.AddWithValue("$HashedPassword", user.HashedPassword);
            cmd.Parameters.AddWithValue("$ArduinoId", user.ArduinoId);
            cmd.Parameters.AddWithValue("$CreatedAt", user.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$Role", user.Role);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // ---- GET USER BY EMAIL ---------------------------------
        public async Task<UserModel?> GetByEmailAsync(string email)
        {
            const string sql =
                @"
                SELECT UserId, Name, Email, HashedPassword, ArduinoId, CreatedAt, Role
                FROM Users
                WHERE Email = $Email;
            ";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("$Email", email);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UserModel
                {
                    UserId = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    HashedPassword = reader.GetString(3),
                    ArduinoId = reader.GetInt32(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    Role = reader.GetString(6),
                };
            }

            return null;
        }

        // ---- DB CREATION ---------------------------------------
        private void EnsureDatabase()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            string sql =
                @"
                CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    HashedPassword TEXT NOT NULL,
                    ArduinoId INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'User'
                );
            ";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
