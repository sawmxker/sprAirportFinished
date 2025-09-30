using System;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.Windows.Navigation;
using System.Text;
using sprAirport.Project.Entry;

namespace sprAirport.Project.PagesAccs
{
    public partial class LoginPage : Page
    {
        private string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public static User CurrentUser { get; private set; }

        public class User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public int RoleId { get; set; }
            public string RoleName { get; set; }
        }

        public LoginPage()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    ShowMessage("Введите логин!");
                    txtUsername.Focus();
                    return;
                }

                if (pbPassword.Password.Length == 0)
                {
                    ShowMessage("Введите пароль!");
                    pbPassword.Focus();
                    return;
                }

                User user = AuthenticateUser(txtUsername.Text, pbPassword.Password);

                if (user != null)
                {
                    Session.CurrentUser = new UserViewModel
                    {
                        Id = user.Id,
                        Username = user.Username,
                        FullName = user.FullName,
                        RoleId = user.RoleId,
                        RoleName = user.RoleName
                    };


                    MessageBox.Show($"Добро пожаловать, {user.FullName}!\nРоль: {user.RoleName}",
                        "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigateBasedOnRole(user.RoleId);
                }
                else
                {
                    ShowMessage("Неверный логин или пароль!");
                    pbPassword.Password = "";
                    pbPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка: {ex.Message}");
            }
        }

        private void NavigateBasedOnRole(int roleId)
        {
            if (roleId == 1) 
            {
                NavigationService?.Navigate(new PageAdmin());
            }
            else
            {
                NavigationService?.Navigate(new PageUser());
            }
        }

        private User AuthenticateUser(string username, string password)
        {
            string passwordHash = HashPassword(password);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT u.Id, u.Username, u.FullName, u.RoleId, r.Name as RoleName 
            FROM Users u 
            INNER JOIN Roles r ON u.RoleId = r.Id 
            WHERE u.Username = @Username AND u.Password = @Password";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username.Trim());
                command.Parameters.AddWithValue("@Password", passwordHash); // хэш а не пароль

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        FullName = reader["FullName"].ToString(),
                        RoleId = (int)reader["RoleId"],
                        RoleName = reader["RoleName"].ToString()
                    };
                }

                return null;
            }
        }

        // Функция хэширования
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }


        private void btnRegIn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new PageCreateAcc());
        }

        private void ShowMessage(string message)
        {
            txtMessage.Text = message;
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtMessage.Text = "";
        }

        private void pbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "";
        }

        private void txtUsername_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                pbPassword.Focus();
            }
        }

        private void pbPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }
    }
}