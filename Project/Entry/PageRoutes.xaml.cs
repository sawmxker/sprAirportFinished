using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace sprAirport.Project.Entry
{
    public partial class PageRoutes : Page
    {
        private ObservableCollection<RouteViewModel> Routes = new ObservableCollection<RouteViewModel>();

        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageRoutes()
        {
            InitializeComponent();
            LoadRoutes();

            RoutesDataGrid.ItemsSource = Routes;

            AddRouteButton.Click += AddRouteButton_Click;
            UpdateRouteButton.Click += UpdateRouteButton_Click;
            DeleteRouteButton.Click += DeleteRouteButton_Click;
            RoutesDataGrid.SelectionChanged += RoutesDataGrid_SelectionChanged;
            BackButton.Click += BackButton_Click;
        }

        private void LoadRoutes()
        {
            Routes.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Number, Distance, FromCity, ToCity FROM Routes ORDER BY Id", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Routes.Add(new RouteViewModel
                    {
                        Id = reader.GetInt32(0),
                        Number = reader.GetString(1),
                        Distance = reader.GetInt32(2),
                        FromCity = reader.GetString(3),
                        ToCity = reader.GetString(4)
                    });
                }
            }
        }

        private void AddRouteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out string number, out int distance, out string fromCity, out string toCity))
                return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("INSERT INTO Routes (Number, Distance, FromCity, ToCity) VALUES (@number, @distance, @fromCity, @toCity)", conn);
                    cmd.Parameters.AddWithValue("@number", number);
                    cmd.Parameters.AddWithValue("@distance", distance);
                    cmd.Parameters.AddWithValue("@fromCity", fromCity);
                    cmd.Parameters.AddWithValue("@toCity", toCity);
                    cmd.ExecuteNonQuery();
                }

                LoadRoutes();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении маршрута: {ex.Message}");
            }
        }

        private void UpdateRouteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoutesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите маршрут для изменения.");
                return;
            }

            if (!ValidateInputs(out string number, out int distance, out string fromCity, out string toCity))
                return;

            var route = (RouteViewModel)RoutesDataGrid.SelectedItem;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("UPDATE Routes SET Number=@number, Distance=@distance, FromCity=@fromCity, ToCity=@toCity WHERE Id=@id", conn);
                    cmd.Parameters.AddWithValue("@number", number);
                    cmd.Parameters.AddWithValue("@distance", distance);
                    cmd.Parameters.AddWithValue("@fromCity", fromCity);
                    cmd.Parameters.AddWithValue("@toCity", toCity);
                    cmd.Parameters.AddWithValue("@id", route.Id);
                    cmd.ExecuteNonQuery();
                }

                LoadRoutes();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении маршрута: {ex.Message}");
            }
        }

        private void DeleteRouteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoutesDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите маршрут для удаления.");
                return;
            }

            var route = (RouteViewModel)RoutesDataGrid.SelectedItem;

            if (MessageBox.Show($"Удалить маршрут {route.Number}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM Routes WHERE Id=@id", conn);
                        cmd.Parameters.AddWithValue("@id", route.Id);
                        cmd.ExecuteNonQuery();
                    }
                    LoadRoutes();
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении маршрута: {ex.Message}");
                }
            }
        }

        private void RoutesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var route = (RouteViewModel)RoutesDataGrid.SelectedItem;
            if (route == null) return;

            NumberTextBox.Text = route.Number;
            DistanceTextBox.Text = route.Distance.ToString();
            FromCityTextBox.Text = route.FromCity;
            ToCityTextBox.Text = route.ToCity;
        }

        private void ClearForm()
        {
            NumberTextBox.Text = "";
            DistanceTextBox.Text = "";
            FromCityTextBox.Text = "";
            ToCityTextBox.Text = "";
            RoutesDataGrid.SelectedItem = null;
        }

        private bool ValidateInputs(out string number, out int distance, out string fromCity, out string toCity)
        {
            number = NumberTextBox.Text.Trim();
            fromCity = FromCityTextBox.Text.Trim();
            toCity = ToCityTextBox.Text.Trim();
            distance = 0;

            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Введите номер маршрута.");
                NumberTextBox.Focus();
                return false;
            }

            if (!int.TryParse(DistanceTextBox.Text.Trim(), out distance))
            {
                MessageBox.Show("Неверный формат поля 'Дистанция'.");
                DistanceTextBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(fromCity))
            {
                MessageBox.Show("Введите город отправления.");
                FromCityTextBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(toCity))
            {
                MessageBox.Show("Введите город назначения.");
                ToCityTextBox.Focus();
                return false;
            }

            return true;
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

    public class RouteViewModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public int Distance { get; set; }
        public string FromCity { get; set; }
        public string ToCity { get; set; }
    }
}
