using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Windows.Storage;
using Windows.Security.Credentials;
using System.Diagnostics;
using OtpNet;

namespace AuthInfoStorageLibrary
{
    public static class AuthInfoStorage
    {
        static string connectionString;
        public static void InitializeDatabase()
        {
            PasswordVault vault = new PasswordVault();
            var credential = vault.Retrieve("WinAuth", "Access Token");
            credential.RetrievePassword();

            connectionString = new SqliteConnectionStringBuilder
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
                                        "algorithm TEXT DEFAULT 'SHA1'," +
                                        "digits INTEGER DEFAULT 6," +
                                        "counter INTEGER DEFAULT 0," +
                                        "period INTEGER DEFAULT 30," +
                                        "added_time DATETIME DEFAULT CURRENT_TIMESTAMP" +
                                      "); ";
                command.ExecuteNonQuery();

                connection.Close();
            }
        }
        
        public static void AddOTPInfoToStorage(OTPInfo otp)
        {
            if (otp.Label == null || otp.Label.Length == 0)
                throw new ArgumentNullException("Label cannot be null");
            if (otp.Type == null || otp.Type.Length == 0)
                throw new ArgumentNullException("Type cannot be null");
            if (otp.Secret == null || otp.Secret.Length == 0)
                throw new ArgumentNullException("Secret cannot be null");

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = connection;

                insertCommand.CommandText = "INSERT INTO otp_list (label, type, secret, issuer, algorithm, digits, counter, period) " +
                                            "VALUES (@label, @type, @secret, @issuer, @algorithm, @digits, @counter, @period);";
                insertCommand.Parameters.AddWithValue("@label", otp.Label);
                insertCommand.Parameters.AddWithValue("@type", otp.Type);
                insertCommand.Parameters.AddWithValue("@secret", otp.Secret);
                insertCommand.Parameters.AddWithValue("@issuer", otp.Issuer ?? otp.Label.Split(':')[0]);
                insertCommand.Parameters.AddWithValue("@algorithm", otp.Algorithm);
                insertCommand.Parameters.AddWithValue("@digits", otp.Digits);
                insertCommand.Parameters.AddWithValue("@counter", otp.Counter);
                insertCommand.Parameters.AddWithValue("@period", otp.Period);

                insertCommand.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
