using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace sprAirport.Project.PagesAccs
{
    public partial class PageUser : Page
    {
        private string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageUser()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllFlights();
        }

        private void LoadAllFlights()
        {
            string sql = @"
SELECT f.Id AS FlightId, r.Number AS RouteNumber, r.FromCity, r.ToCity,
       f.DepartureDate, f.ArrivalDate, a.Model AS AircraftModel, a.Seats, f.TicketsSold,
       (a.Seats - f.TicketsSold) AS FreeSeats
FROM Flights f
JOIN Routes r ON f.RouteId = r.Id
JOIN Aircrafts a ON f.AircraftId = a.Id
ORDER BY f.DepartureDate DESC;";

            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                dgFlights.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки рейсов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnShowFlights_Click(object sender, RoutedEventArgs e)
        {
            string fromCity = tbFromCity.Text.Trim();
            string toCity = tbToCity.Text.Trim();

            if (string.IsNullOrEmpty(fromCity) || string.IsNullOrEmpty(toCity))
            {
                MessageBox.Show("Введите оба поля: От и До.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string sql = @"
SELECT f.Id AS FlightId, r.Number AS RouteNumber, r.FromCity, r.ToCity,
       f.DepartureDate, f.ArrivalDate, a.Model AS AircraftModel, a.Seats, f.TicketsSold,
       (a.Seats - f.TicketsSold) AS FreeSeats
FROM Flights f
JOIN Routes r ON f.RouteId = r.Id
JOIN Aircrafts a ON f.AircraftId = a.Id
WHERE r.FromCity = @fromCity AND r.ToCity = @toCity
ORDER BY f.DepartureDate;";

            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@fromCity", fromCity);
                    cmd.Parameters.AddWithValue("@toCity", toCity);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                dgFlights.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка фильтрации рейсов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCheckFlight_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(tbFlightId.Text.Trim(), out int flightId))
            {
                MessageBox.Show("Неверный Id рейса. Введите целое число Id рейса из таблицы.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpFlightDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату рейса.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime date = dpFlightDate.SelectedDate.Value.Date;

            string sql = @"
SELECT f.Id AS FlightId, r.Number AS RouteNumber, r.FromCity, r.ToCity,
       f.DepartureDate, f.ArrivalDate, a.Model AS AircraftModel, a.Seats, f.TicketsSold,
       (a.Seats - f.TicketsSold) AS FreeSeats
FROM Flights f
JOIN Aircrafts a ON f.AircraftId = a.Id
JOIN Routes r ON f.RouteId = r.Id
WHERE f.Id = @id AND CAST(f.DepartureDate AS DATE) = @d;";

            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", flightId);
                    cmd.Parameters.AddWithValue("@d", date);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Рейс не найден по указанному Id и дате.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                dgFlights.ItemsSource = dt.DefaultView;

                int freeSeats = Convert.ToInt32(dt.Rows[0]["FreeSeats"]);
                if (freeSeats > 0)
                {
                    MessageBox.Show($"Рейс Id={flightId} на {date:dd.MM.yyyy} — свободных мест: {freeSeats}.", "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Свободных мест нет на рейс Id={flightId} на {date:dd.MM.yyyy}.", "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке рейса: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewRoutes_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewFlights_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchSeats_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                {
                    this.NavigationService.GoBack();
                }
                else
                {
                    Application.Current.MainWindow?.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выходе: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
