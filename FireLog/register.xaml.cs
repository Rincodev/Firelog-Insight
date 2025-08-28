using System;
using System.Text;
using System.Windows;
using BCrypt.Net;
using Npgsql;

namespace FireLog
{
    public partial class register : Window
    {
        public register()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void btn_SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Username or password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(DbHelper.GetConnectionString()))
                {
                    conn.Open();

                   
                    string checkUserQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    using (var checkCmd = new NpgsqlCommand(checkUserQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        int userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (userCount > 0)
                        {
                            MessageBox.Show("This username is already taken. Please choose another one.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

               
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                   
                   
                    string insertQuery = "INSERT INTO users (id, username, password_hash) VALUES (@id, @username, @password)";
                    using (var insertCmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        Guid userId = Guid.NewGuid(); 
                        AppState.CurrentUserId = userId;

                        insertCmd.Parameters.AddWithValue("@id", userId);
                        insertCmd.Parameters.AddWithValue("@username", username);
                        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                        int rowsAffected = insertCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Registration successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            login loginWindow = new login();
                            loginWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Registration failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Connection failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_LogIn_Click(object sender, RoutedEventArgs e)
        {
            login loginWindow = new login();
            loginWindow.Show();
            this.Close();
        }
    }
}
