using System;
using System.Windows;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace sprAirport.Project.PagesAccs
{
    public partial class PageCreateAcc : Page
    {
        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageCreateAcc()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    ShowMessage("Введите ФИО пользователя!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLogin.Text))
                {
                    ShowMessage("Введите логин!");
                    return;
                }

                if (pbPassword.Password.Length == 0)
                {
                    ShowMessage("Введите пароль!");
                    return;
                }

                if (pbConfirmPassword.Password.Length == 0)
                {
                    ShowMessage("Подтвердите пароль!");
                    return;
                }

                if (pbPassword.Password != pbConfirmPassword.Password)
                {
                    ShowMessage("Пароли не совпадают!");
                    return;
                }

                if (!IsValidPassword(pbPassword.Password))
                {
                    ShowMessage("Пароль должен содержать минимум 8 символов, включать как минимум одну заглавную букву, одну цифру и один специальный символ.");
                    return;
                }

                // хэш пароля перед регистрацией
                string hashedPassword = HashPassword(pbPassword.Password);

                if (CreateUser(txtFullName.Text, txtLogin.Text, hashedPassword))
                {
                    MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService?.Navigate(new LoginPage());
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                {
                    ShowMessage("Пользователь с таким логином уже существует!");
                }
                else if (ex.Number == 547)
                {
                    if (CreateDefaultRoleAndUser(txtFullName.Text, txtLogin.Text, HashPassword(pbPassword.Password)))
                    {
                        MessageBox.Show("Пользователь успешно зарегистрирован!", "Успех");
                        NavigationService?.Navigate(new LoginPage());
                    }
                    else
                    {
                        ShowMessage("Ошибка при создании пользователя. Проверьте наличие ролей в базе.");
                    }
                }
                else
                {
                    ShowMessage($"Ошибка базы данных: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка: {ex.Message}");
            }
        }

        private bool IsValidPassword(string password)
        {
            // Регулярное выражение: минимум 8 символов, хотя бы одна заглавная, одна цифра, один спецсимвол
            string pattern = @"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).{8,}$";
            return Regex.IsMatch(password, pattern);
        }

        private bool CreateUser(string fullName, string username, string hashedPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Проверка, есть ли уже пользователь с таким логином
                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                SqlCommand checkUserCmd = new SqlCommand(checkUserQuery, connection);
                checkUserCmd.Parameters.AddWithValue("@Username", username.Trim());
                int userExists = (int)checkUserCmd.ExecuteScalar();
                if (userExists > 0)
                {
                    ShowMessage("Пользователь с таким логином уже существует!");
                    return false;
                }

                string checkRolesQuery = "SELECT COUNT(*) FROM Roles WHERE Id = 2";
                SqlCommand checkCmd = new SqlCommand(checkRolesQuery, connection);

                int roleExists = (int)checkCmd.ExecuteScalar();

                if (roleExists == 0)
                {
                    string createRolesQuery = @"
                INSERT INTO Roles (Name) VALUES ('Администратор');
                INSERT INTO Roles (Name) VALUES ('Пользователь');";
                    SqlCommand createCmd = new SqlCommand(createRolesQuery, connection);
                    createCmd.ExecuteNonQuery();
                }

                string query = @"
            INSERT INTO Users (Username, Password, FullName, RoleId) 
            VALUES (@Username, @Password, @FullName, 2)";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username.Trim());
                command.Parameters.AddWithValue("@Password", hashedPassword);
                command.Parameters.AddWithValue("@FullName", fullName.Trim());

                int result = command.ExecuteNonQuery();
                return result > 0;
            }
        }

        private bool CreateDefaultRoleAndUser(string fullName, string username, string hashedPassword)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkRolesQuery = "SELECT COUNT(*) FROM Roles";
                SqlCommand checkCmd = new SqlCommand(checkRolesQuery, connection);
                int rolesCount = (int)checkCmd.ExecuteScalar();

                if (rolesCount == 0)
                {
                    string createRolesQuery = @"
                        INSERT INTO Roles (Name) VALUES ('Администратор');
                        INSERT INTO Roles (Name) VALUES ('Пользователь');";
                    SqlCommand createCmd = new SqlCommand(createRolesQuery, connection);
                    createCmd.ExecuteNonQuery();
                }

                string createUserQuery = @"
                    INSERT INTO Users (Username, Password, FullName, RoleId) 
                    VALUES (@Username, @Password, @FullName, 2)";

                SqlCommand userCmd = new SqlCommand(createUserQuery, connection);
                userCmd.Parameters.AddWithValue("@Username", username.Trim());
                userCmd.Parameters.AddWithValue("@Password", hashedPassword);
                userCmd.Parameters.AddWithValue("@FullName", fullName.Trim());

                int result = userCmd.ExecuteNonQuery();
                return result > 0;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void ShowMessage(string message)
        {
            txtMessage.Text = message;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
            NavigationService?.Navigate(new LoginPage());
        }

        private void ClearFields()
        {
            txtFullName.Text = "";
            txtLogin.Text = "";
            pbPassword.Password = "";
            pbConfirmPassword.Password = "";
            txtMessage.Text = "";
        }

        private void txtFullName_TextChanged(object sender, TextChangedEventArgs e) => txtMessage.Text = "";
        private void txtLogin_TextChanged(object sender, TextChangedEventArgs e) => txtMessage.Text = "";
        private void pbPassword_PasswordChanged(object sender, RoutedEventArgs e) => txtMessage.Text = "";
        private void pbConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e) => txtMessage.Text = "";
    }
}
