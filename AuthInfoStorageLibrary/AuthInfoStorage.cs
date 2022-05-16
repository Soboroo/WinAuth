using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Windows.Storage;
using Windows.Security.Credentials;
using System.Diagnostics;

namespace AuthInfoStorageLibrary
{
    public static class AuthInfoStorage
    {
        public static void InitializeDatabase()
        {
            PasswordVault vault = new PasswordVault();
            var credential = vault.Retrieve("WinAuth", "Access Token");
            credential.RetrievePassword();

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(ApplicationData.Current.LocalFolder.Path, "AuthInfoDatabase.db"),
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = credential.Password
            }.ToString();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE if not EXISTS otp_list (" +
                                        "Primary_Key INTEGER PRIMARY KEY AUTOINCREMENT," +
                                        "label TEXT NOT NULL," +
                                        "type TEXT NOT NULL," +
                                        "secret TEXT NOT NULL," +
                                        "issuer TEXT," +
                                        "algorithm TEXT DEFAULT SHA1," +
                                        "digits INTEGER DEFAULT 6," +
                                        "counter INTEGER," +
                                        "period INTEGER DEFAULT 30," +
                                        "added_time DATETIME DEFAULT CURRENT_TIMESTAMP" +
                                      "); ";
                command.ExecuteNonQuery();
            }
        }
    }
}
