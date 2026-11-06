using Microsoft.EntityFrameworkCore;
using Pracrice.Models;
using IniParser;
using IniParser.Model;
using System.IO;
using System;

namespace Pracrice
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public AppDbContext()
        {
            // Метод создает БД и таблицы. Если они создаются с нуля, 
            // автоматически запишется администратор из OnModelCreating.
            // Больше никаких SeedInitialUser() здесь нет во избежание StackOverflow!
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string filePath = "config.ini";
            var parser = new FileIniDataParser();
            IniData data;

            if (!File.Exists(filePath))
            {
                data = new IniData();
                data["Database"]["Host"] = "localhost";
                data["Database"]["Port"] = "3306";
                data["Database"]["User"] = "root";
                data["Database"]["Password"] = "";
                data["Database"]["DB"] = "RosPiv";

                parser.WriteFile(filePath, data);
            }
            else
            {
                try
                {
                    data = parser.ReadFile(filePath);
                }
                catch (IniParser.Exceptions.ParsingException)
                {
                    data = new IniData();
                }
            }

            string host = data["Database"]["Host"] ?? "localhost";
            string port = data["Database"]["Port"] ?? "3306";
            string user = data["Database"]["User"] ?? "root";
            string password = data["Database"]["Password"] ?? "";
            string database = data["Database"]["DB"] ?? "RosPiv";

            // Собираем правильную строку для MySQL
            string connectionString = $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};";

            try
            {
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

                optionsBuilder.UseMySql(connectionString, serverVersion);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось настроить подключение к MySQL через config.ini: {ex.Message}", ex);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1, 
                    Login = "admin",
                    Pass = defaultPasswordHash,
                    Role = 0,
                    FullName = "Первичный пользователь"
                }
            );
        }
    }
}
