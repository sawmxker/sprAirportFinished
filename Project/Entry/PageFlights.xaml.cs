using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace sprAirport.Project.Entry
{
    public partial class PageFlights : Page
    {
        private ObservableCollection<FlightViewModel> Flights = new ObservableCollection<FlightViewModel>();

        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageFlights()
        {
            InitializeComponent();
            LoadFlights();

            FlightsDataGrid.ItemsSource = Flights;

            AddFlightButton.Click += AddFlightButton_Click;
            UpdateFlightButton.Click += UpdateFlightButton_Click;
            DeleteFlightButton.Click += DeleteFlightButton_Click;
            FlightsDataGrid.SelectionChanged += FlightsDataGrid_SelectionChanged;
        }

        private void LoadFlights()
        {
            Flights.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT f.Id, r.Number as Route, a.Number as Aircraft, f.DepartureDate, f.ArrivalDate, f.TicketsSold
                    FROM Flights f
                    INNER JOIN Routes r ON f.RouteId = r.Id
                    INNER JOIN Aircrafts a ON f.AircraftId = a.Id
                    ORDER BY f.Id", conn);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Flights.Add(new FlightViewModel
                    {
                        Id = reader.GetInt32(0),
                        Route = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Aircraft = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DepartureDate = reader.GetDateTime(3),
                        ArrivalDate = reader.GetDateTime(4),
                        TicketsSold = reader.GetInt32(5)
                    });
                }
            }
        }

        private void AddFlightButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out int ticketsSold,
                                out DateTime departureDateTime,
                                out DateTime arrivalDateTime))
                return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"INSERT INTO Flights (RouteId, AircraftId, DepartureDate, ArrivalDate, TicketsSold) 
                                               VALUES (@routeId, @aircraftId, @depDate, @arrDate, @ticketsSold)", conn);

                    int routeId = GetRouteId(RouteTextBox.Text.Trim(), conn);
                    int aircraftId = GetAircraftId(AircraftTextBox.Text.Trim(), conn);

                    cmd.Parameters.AddWithValue("@routeId", routeId);
                    cmd.Parameters.AddWithValue("@aircraftId", aircraftId);
                    cmd.Parameters.AddWithValue("@depDate", departureDateTime);
                    cmd.Parameters.AddWithValue("@arrDate", arrivalDateTime);
                    cmd.Parameters.AddWithValue("@ticketsSold", ticketsSold);
                    cmd.ExecuteNonQuery();
                }

                LoadFlights();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении рейса: {ex.Message}");
            }
        }

        private void UpdateFlightButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlightsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите рейс для изменения.");
                return;
            }

            if (!ValidateInputs(out int ticketsSold,
                                out DateTime departureDateTime,
                                out DateTime arrivalDateTime))
                return;

            var flight = (FlightViewModel)FlightsDataGrid.SelectedItem;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"UPDATE Flights SET RouteId = @routeId, AircraftId = @aircraftId, DepartureDate = @depDate, ArrivalDate = @arrDate, TicketsSold = @ticketsSold
                                              WHERE Id = @id", conn);

                    int routeId = GetRouteId(RouteTextBox.Text.Trim(), conn);
                    int aircraftId = GetAircraftId(AircraftTextBox.Text.Trim(), conn);

                    cmd.Parameters.AddWithValue("@routeId", routeId);
                    cmd.Parameters.AddWithValue("@aircraftId", aircraftId);
                    cmd.Parameters.AddWithValue("@depDate", departureDateTime);
                    cmd.Parameters.AddWithValue("@arrDate", arrivalDateTime);
                    cmd.Parameters.AddWithValue("@ticketsSold", ticketsSold);
                    cmd.Parameters.AddWithValue("@id", flight.Id);
                    cmd.ExecuteNonQuery();
                }

                LoadFlights();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении рейса: {ex.Message}");
            }
        }

        private void DeleteFlightButton_Click(object sender, RoutedEventArgs e)
        {
            if (FlightsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите рейс для удаления.");
                return;
            }

            var flight = (FlightViewModel)FlightsDataGrid.SelectedItem;

            if (MessageBox.Show($"Удалить рейс с ID {flight.Id}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM Flights WHERE Id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", flight.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LoadFlights();
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении рейса: {ex.Message}");
                }
            }
        }

        private void FlightsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var flight = (FlightViewModel)FlightsDataGrid.SelectedItem;
            if (flight == null) return;

            RouteTextBox.Text = flight.Route;
            AircraftTextBox.Text = flight.Aircraft;
            DepartureDatePicker.SelectedDate = flight.DepartureDate.Date;
            DepartureTimeTextBox.Text = flight.DepartureDate.ToString("HH:mm");
            ArrivalDatePicker.SelectedDate = flight.ArrivalDate.Date;
            ArrivalTimeTextBox.Text = flight.ArrivalDate.ToString("HH:mm");
            TicketsSoldTextBox.Text = flight.TicketsSold.ToString();
        }

        private void ClearForm()
        {
            RouteTextBox.Text = "";
            AircraftTextBox.Text = "";
            DepartureDatePicker.SelectedDate = null;
            DepartureTimeTextBox.Text = "00:00";
            ArrivalDatePicker.SelectedDate = null;
            ArrivalTimeTextBox.Text = "00:00";
            TicketsSoldTextBox.Text = "";
            FlightsDataGrid.SelectedItem = null;
        }

        private bool ValidateInputs(out int ticketsSold, out DateTime departureDateTime, out DateTime arrivalDateTime)
        {
            ticketsSold = 0;
            departureDateTime = DateTime.MinValue;
            arrivalDateTime = DateTime.MinValue;

            if (!int.TryParse(TicketsSoldTextBox.Text.Trim(), out ticketsSold))
            {
                MessageBox.Show("Неверный формат поля 'Билетов продано'.");
                TicketsSoldTextBox.Focus();
                return false;
            }

            if (DepartureDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату вылета.");
                DepartureDatePicker.Focus();
                return false;
            }

            if (ArrivalDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату прибытия.");
                ArrivalDatePicker.Focus();
                return false;
            }

            if (!TimeSpan.TryParse(DepartureTimeTextBox.Text.Trim(), out var depTime))
            {
                MessageBox.Show("Неверный формат времени вылета. Используйте HH:mm.");
                DepartureTimeTextBox.Focus();
                return false;
            }

            if (!TimeSpan.TryParse(ArrivalTimeTextBox.Text.Trim(), out var arrTime))
            {
                MessageBox.Show("Неверный формат времени прибытия. Используйте HH:mm.");
                ArrivalTimeTextBox.Focus();
                return false;
            }

            departureDateTime = DepartureDatePicker.SelectedDate.Value.Date + depTime;
            arrivalDateTime = ArrivalDatePicker.SelectedDate.Value.Date + arrTime;

            return true;
        }

        private int GetRouteId(string routeNumber, SqlConnection connection)
        {
            var cmd = new SqlCommand("SELECT Id FROM Routes WHERE Number = @number", connection);
            cmd.Parameters.AddWithValue("@number", routeNumber);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private int GetAircraftId(string aircraftNumber, SqlConnection connection)
        {
            var cmd = new SqlCommand("SELECT Id FROM Aircrafts WHERE Number = @number", connection);
            cmd.Parameters.AddWithValue("@number", aircraftNumber);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.FrmMain != null)
            {
                var frame = mainWindow.FrmMain;
                if (frame.CanGoBack)
                    frame.GoBack();
                else
                {
                    frame.Navigate(new Uri("/sprAirport/Project/Admin/pageAdmin.xaml", UriKind.Relative));
                    frame.Refresh();  
                }
            }
            else
            {
                NavigationService?.Navigate(new Uri("/sprAirport/Project/Admin/pageAdmin.xaml", UriKind.Relative));
            }
        }

    }

    public class FlightViewModel
    {
        public int Id { get; set; }
        public string Route { get; set; }
        public string Aircraft { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int TicketsSold { get; set; }
    }
}
