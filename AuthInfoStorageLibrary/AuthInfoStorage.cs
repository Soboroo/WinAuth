using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Windows.Storage;
using Windows.Security.Credentials;
using System.Diagnostics;
using OtpNet;
using System.Collections.Generic;

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
                                        "accountname TEXT NOT NULL," +
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
            if (otp.AccountName == null || otp.AccountName.Length == 0)
                throw new ArgumentNullException("Label cannot be null");
            if (otp.Type == null || otp.Type.Length == 0)
                throw new ArgumentNullException("Type cannot be null");
            if (otp.Secret == null || otp.Secret.Length == 0)
                throw new ArgumentNullException("Secret cannot be null");

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteTransaction transaction = connection.BeginTransaction();

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.Connection = connection;
                
                command.CommandText = "INSERT INTO otp_list (accountname, type, secret, issuer, algorithm, digits, counter, period) " +
                                      "VALUES (@accountname, @type, @secret, @issuer, @algorithm, @digits, @counter, @period);";
                command.Parameters.AddWithValue("@accountname", otp.AccountName);
                command.Parameters.AddWithValue("@type", otp.Type);
                command.Parameters.AddWithValue("@secret", otp.Secret);
                command.Parameters.AddWithValue("@issuer", otp.Issuer);
                command.Parameters.AddWithValue("@algorithm", otp.Algorithm);
                command.Parameters.AddWithValue("@digits", String.IsNullOrEmpty(otp.Digits) ? "6" : otp.Digits);
                command.Parameters.AddWithValue("@counter", String.IsNullOrEmpty(otp.Counter) ? "0" : otp.Counter);
                command.Parameters.AddWithValue("@period", String.IsNullOrEmpty(otp.Period) ? "30" : otp.Period);

                command.ExecuteNonQuery();
                transaction.Commit();
                connection.Close();
                /*

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = connection;

                insertCommand.CommandText = "INSERT INTO otp_list (label, type, secret, issuer, algorithm, digits, counter, period) " +
                                            "VALUES (@label, @type, @secret, @issuer, @algorithm, @digits, @counter, @period);";
                insertCommand.Parameters.AddWithValue("@label", otp.Label);
                insertCommand.Parameters.AddWithValue("@type", otp.Type);
                insertCommand.Parameters.AddWithValue("@secret", otp.Secret);
                insertCommand.Parameters.AddWithValue("@issuer", otp.Issuer ?? otp.Label.Split(':')[0]);
                insertCommand.Parameters.AddWithValue("@algorithm", otp.Algorithm);
                insertCommand.Parameters.AddWithValue("@digits", String.IsNullOrEmpty(otp.Digits) ? "6" : otp.Digits);
                insertCommand.Parameters.AddWithValue("@counter", String.IsNullOrEmpty(otp.Counter) ? "0" : otp.Counter);
                insertCommand.Parameters.AddWithValue("@period", String.IsNullOrEmpty(otp.Period) ? "30" : otp.Period);

                insertCommand.ExecuteNonQuery();
                connection.Close();
                */
            }
        }

        public static void RemoveOTPInfoFromStorage(string accountName)
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteTransaction transaction = connection.BeginTransaction();

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.Connection = connection;

                command.CommandText = "DELETE FROM otp_list WHERE accountname = @accountname;";
                command.Parameters.AddWithValue("@accountname", accountName);

                command.ExecuteNonQuery();
                transaction.Commit();
                connection.Close();
            }
        }

        public static void UpdateOTPInfoInStorage(OTPInfo otp)
        {
            if (otp.AccountName == null || otp.AccountName.Length == 0)
                throw new ArgumentNullException("Label cannot be null");
            if (otp.Type == null || otp.Type.Length == 0)
                throw new ArgumentNullException("Type cannot be null");
            if (otp.Secret == null || otp.Secret.Length == 0)
                throw new ArgumentNullException("Secret cannot be null");

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteTransaction transaction = connection.BeginTransaction();

                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                command.Connection = connection;

                command.CommandText = "UPDATE otp_list SET accountname = @accountname, type = @type, secret = @secret, issuer = @issuer, algorithm = @algorithm, digits = @digits, counter = @counter, period = @period WHERE accountname = @accountname;";
                command.Parameters.AddWithValue("@accountname", otp.AccountName);
                command.Parameters.AddWithValue("@type", otp.Type);
                command.Parameters.AddWithValue("@secret", otp.Secret);
                command.Parameters.AddWithValue("@issuer", otp.Issuer);
                command.Parameters.AddWithValue("@algorithm", otp.Algorithm);
                command.Parameters.AddWithValue("@digits", String.IsNullOrEmpty(otp.Digits) ? "6" : otp.Digits);
                command.Parameters.AddWithValue("@counter", String.IsNullOrEmpty(otp.Counter) ? "0" : otp.Counter);
                command.Parameters.AddWithValue("@period", String.IsNullOrEmpty(otp.Period) ? "30" : otp.Period);

                command.ExecuteNonQuery();
                transaction.Commit();
                connection.Close();
            }
        }

        public static List<OTPInfo> GetOTPInfoFromStorage()
        {
            List<OTPInfo> otpList = new List<OTPInfo>();

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;

                command.CommandText = "SELECT * FROM otp_list;";

                SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    OTPInfo otp = new OTPInfo();
                    otp.AccountName = reader["accountname"].ToString();
                    otp.Type = reader["type"].ToString();
                    otp.Secret = reader["secret"].ToString();
                    otp.Issuer = reader["issuer"].ToString();
                    otp.Algorithm = reader["algorithm"].ToString();
                    otp.Digits = reader["digits"].ToString();
                    otp.Counter = reader["counter"].ToString();
                    otp.Period = reader["period"].ToString();

                    otpList.Add(otp);
                }
                connection.Close();
            }

            return otpList;
        }
    }
}
