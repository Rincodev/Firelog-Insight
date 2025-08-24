using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Security.Cryptography;
using Npgsql;
using BCrypt.Net;
using Serilog;


namespace FireLog
{
    public partial class login : Window
    {
        private static bool HasDbConfig()
{
    var host = Environment.GetEnvironmentVariable("APP_DB_HOST");
    if (!string.IsNullOrWhiteSpace(host))
    {
        Log.Debug("HasDbConfig(): ENV present (APP_DB_HOST).");
        return true;
    }

    var jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_credentials.json");
    bool exists = File.Exists(jsonPath);
    Log.Debug("HasDbConfig(): db_credentials.json exists = {Exists}, path = {Path}", exists, jsonPath);
    return exists;
}


        public static Guid LoggedInUserID { get; set; }
        private const string CREDENTIALS_FILE = "credentials.json";

        private class UserCredentials
        {
            public string Username { get; set; } = string.Empty;
            public string RememberToken { get; set; } = string.Empty;
        }

        public login()
        {
            InitializeComponent();
            Log.Information("Login window created.");

            if (!HasDbConfig())
            {
                Log.Warning("Offline mode: DB config not found. Opening app without login.");
                MessageBox.Show("Offline mode: database features are disabled. Opening app without login.",
                                "Offline", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow main = new MainWindow("Offline");
                Application.Current.MainWindow = main;
                main.Show();
                this.Close();
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.Loaded += Login_Loaded;
        }

        private void Login_Loaded(object? sender, RoutedEventArgs e)
        {
            Log.Debug("Login window loaded. Attempting auto-login via remember-me (if present).");
            LoadSavedCredentials();
        }

        private void btn_LogIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Log.Warning("Login attempt with empty fields. UsernameEmpty={UEmpty}, PasswordEmpty={PEmpty}",
                    string.IsNullOrEmpty(username), string.IsNullOrEmpty(password));
                MessageBox.Show("Username or password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log.Information("Login button clicked for user {User}", username);
            AttemptLogin(username, password);
        }


        private void AttemptLogin(string username, string password)
        {
            if (!HasDbConfig())
            {
                Log.Warning("AttemptLogin called without DB config.");
                MessageBox.Show("Database is not configured. Use Offline mode or set db_credentials.json / APP_DB_*.",
                                "Offline", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Log.Information("Attempting login for user {User}", username);

                using var conn = new NpgsqlConnection(DbHelper.GetConnectionString());
                conn.Open();

                const string query = "SELECT id, password_hash FROM users WHERE username = @username";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    Guid userId = reader.GetGuid(0);
                    string storedHash = reader.GetString(1);

                    if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                    {
                        Log.Information("Login success for {User}", username);
                        LoggedInUserID = userId;
                        AppState.CurrentUserId = LoggedInUserID;

                        AuthState.islogined = true;
                        if (checkbox_remember.IsChecked == true)
                        {
                            string token = GenerateSecureToken();
                            SaveTokenToDatabase(userId, token);
                            SaveTokenLocally(username, token);
                            Log.Information("Remember-me token issued for {User}", username);
                        }
                        else
                        {
                            File.Delete(CREDENTIALS_FILE);
                            Log.Debug("Remember-me unchecked. Removed local credentials file.");
                        }
                        Serilog.Context.LogContext.PushProperty("UserId", LoggedInUserID);
                        Serilog.Context.LogContext.PushProperty("Username", username);
                        Log.Information("User logged in");

                        MessageBox.Show($"Welcome, {username}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                        MainWindow main = new MainWindow(username);
                        Application.Current.MainWindow = main;
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        Log.Warning("Login failed for {User}: incorrect password.", username);
                        MessageBox.Show("Incorrect password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    Log.Warning("Login failed: user {User} not found.", username);
                    MessageBox.Show("User not found.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Connection error during login for {User}", username);
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadSavedCredentials()
        {
            if (!HasDbConfig()) return;
            if (!File.Exists(CREDENTIALS_FILE))
            {
                Log.Debug("credentials.json not found. Skipping auto-login.");
                return;
            }

            try
            {
                string json = File.ReadAllText(CREDENTIALS_FILE);
                var creds = JsonSerializer.Deserialize<UserCredentials>(json);
                if (creds == null || string.IsNullOrEmpty(creds.Username) || string.IsNullOrEmpty(creds.RememberToken))
                {
                    Log.Warning("Invalid credentials.json format. Deleting file.");
                    File.Delete(CREDENTIALS_FILE);
                    return;
                }

                using var conn = new NpgsqlConnection(DbHelper.GetConnectionString());
                conn.Open();

                const string query = "SELECT id FROM users WHERE username = @username AND remember_token = @token";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", creds.Username);
                cmd.Parameters.AddWithValue("@token", creds.RememberToken);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    LoggedInUserID = reader.GetGuid(0);
                    AuthState.islogined = true;
                    AppState.CurrentUserId = LoggedInUserID;

                    Log.Information("Auto-login via remember-me succeeded for {User}", creds.Username);

                    var main = new MainWindow(creds.Username);
                    Application.Current.MainWindow = main;
                    main.Show();
                    this.Close();
                }
                else
                {
                    Log.Warning("Remember-me token invalid for {User}. Deleting credentials.json", creds.Username);
                    File.Delete(CREDENTIALS_FILE);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Credential loading failed. Removing credentials.json");
                File.Delete(CREDENTIALS_FILE);
            }
        }


        private void SaveTokenLocally(string username, string token)
        {
            var creds = new UserCredentials { Username = username, RememberToken = token };
            string json = JsonSerializer.Serialize(creds, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CREDENTIALS_FILE, json);
        }

        private void SaveTokenToDatabase(Guid userId, string token)
        {
            using var conn = new NpgsqlConnection(DbHelper.GetConnectionString());
            conn.Open();

            const string query = "UPDATE users SET remember_token = @token WHERE id = @id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();

            Log.Debug("Remember-me token stored in DB for userId={UserId}", userId);
        }
        private void checkbox_remember_Checked(object sender, RoutedEventArgs e)
        {
            // Save credentials when checkbox is checked and both fields have data
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
        }
        private string GenerateSecureToken()
        {   
            byte[] data = new byte[32];
            RandomNumberGenerator.Fill(data);
            return Convert.ToBase64String(data);
        }

        private void btn_SignIn_Click(object sender, RoutedEventArgs e)
        {
            new register().Show();
            this.Close();
        }

        public static void Logout()
        {
            if (LoggedInUserID == Guid.Empty) return;

            try
            {
                using var conn = new NpgsqlConnection(DbHelper.GetConnectionString());
                conn.Open();

                const string query = "UPDATE users SET remember_token = NULL WHERE id = @id";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", LoggedInUserID);
                cmd.ExecuteNonQuery();

                Log.Information("User {UserId} logged out. Cleared remember-me and local credentials.", LoggedInUserID);

                LoggedInUserID = Guid.Empty;
                AuthState.islogined = false;
                File.Delete(CREDENTIALS_FILE);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Logout error for user {UserId}", LoggedInUserID);
            }
        }
    }
}
