using sprAirport.Project.PagesAccs;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace sprAirport.Project.Entry
{
    public partial class PageUsersManagement : Page
    {
        private ObservableCollection<UserViewModel> Users = new ObservableCollection<UserViewModel>();
        private ObservableCollection<RoleModel> Roles = new ObservableCollection<RoleModel>();

        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        private bool ShowPasswords = false;
        public UserViewModel CurrentUser { get; set; }

        public PageUsersManagement()
        {
            InitializeComponent();

            CurrentUser = Session.CurrentUser ?? throw new InvalidOperationException("Текущий пользователь не определён. Перезайдите.");

            LoadRoles();
            LoadUsers();

            UsersDataGrid.ItemsSource = Users;
            RolesComboBox.ItemsSource = Roles;

            AddUserButton.Click += AddUserButton_Click;
            UpdateUserButton.Click += UpdateUserButton_Click;
            DeleteUserButton.Click += DeleteUserButton_Click;
            UsersDataGrid.SelectionChanged += UsersDataGrid_SelectionChanged;
        }

        private void LoadRoles()
        {
            Roles.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Name FROM Roles", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Roles.Add(new RoleModel
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        private void LoadUsers()
        {
            Users.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT u.Id, u.Username, u.FullName, r.Name AS RoleName, u.RoleId
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleId = r.Id
                    ORDER BY u.Id", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Users.Add(new UserViewModel
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        FullName = reader.GetString(2),
                        RoleName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        RoleId = reader.GetInt32(4)
                    });
                }
            }
        }

        private int GetAdminCount()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE RoleId = 1", conn);
                return (int)cmd.ExecuteScalar();
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                    RolesComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все обязательные поля.");
                    return;
                }

                var username = UsernameTextBox.Text.Trim();
                var passwordHash = HashPassword(PasswordBox.Password);
                var fullName = FullNameTextBox.Text.Trim();
                var roleId = (int)RolesComboBox.SelectedValue;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"INSERT INTO Users (Username, [Password], FullName, RoleId) 
                                               VALUES (@username, @password, @fullname, @roleId)", conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", passwordHash);
                    cmd.Parameters.AddWithValue("@fullname", fullName);
                    cmd.Parameters.AddWithValue("@roleId", roleId);
                    cmd.ExecuteNonQuery();
                }

                LoadUsers();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении пользователя: " + ex.Message);
            }
        }

        private void UpdateUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UsersDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите пользователя для изменения.");
                    return;
                }

                var user = (UserViewModel)UsersDataGrid.SelectedItem;
                var username = UsernameTextBox.Text.Trim();
                var fullName = FullNameTextBox.Text.Trim();
                var roleId = (int)RolesComboBox.SelectedValue;

                if (user.RoleId == 1 && GetAdminCount() == 1 && roleId != 1)
                {
                    MessageBox.Show("Нельзя снять роль администратора у единственного администратора.");
                    return;
                }

                string passwordHash = null;
                if (!string.IsNullOrEmpty(PasswordBox.Password))
                {
                    passwordHash = HashPassword(PasswordBox.Password);
                }

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = passwordHash == null
                        ? "UPDATE Users SET Username = @username, FullName = @fullname, RoleId = @roleId WHERE Id = @id"
                        : "UPDATE Users SET Username = @username, [Password] = @password, FullName = @fullname, RoleId = @roleId WHERE Id = @id";

                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@fullname", fullName);
                    cmd.Parameters.AddWithValue("@roleId", roleId);
                    cmd.Parameters.AddWithValue("@id", user.Id);

                    if (passwordHash != null)
                        cmd.Parameters.AddWithValue("@password", passwordHash);

                    cmd.ExecuteNonQuery();
                }

                LoadUsers();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении пользователя: " + ex.Message);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UsersDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите пользователя для удаления.");
                    return;
                }
                var user = (UserViewModel)UsersDataGrid.SelectedItem;

                // Запрет удалить единственного администратора
                if (user.RoleId == 1 && GetAdminCount() == 1)
                {
                    MessageBox.Show("Нельзя удалить единственного администратора.");
                    return;
                }


                if (CurrentUser != null && user.Id == CurrentUser.Id)
                {
                    MessageBox.Show("Вы не можете удалить собственный аккаунт.");
                    return;
                }


                if (MessageBox.Show($"Удалить пользователя {user.Username}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM Users WHERE Id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", user.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LoadUsers();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пользователя: " + ex.Message);
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var user = (UserViewModel)UsersDataGrid.SelectedItem;
            if (user == null) return;

            UsernameTextBox.Text = user.Username;
            PasswordBox.Password = "";
            FullNameTextBox.Text = user.FullName;
            RolesComboBox.SelectedValue = user.RoleId;
        }

        private void ClearForm()
        {
            UsernameTextBox.Text = "";
            PasswordBox.Password = "";
            FullNameTextBox.Text = "";
            RolesComboBox.SelectedIndex = -1;
            UsersDataGrid.SelectedItem = null;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                this.NavigationService?.Navigate(new Uri("/sprAirport/Project/Admin/pageAdmin.xaml", UriKind.Relative));
            }
        }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public int RoleId { get; set; }
    }

    public class RoleModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
