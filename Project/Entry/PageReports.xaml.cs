using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace sprAirport.Project.Entry
{
    public partial class PageReports : Page
    {
        private ObservableCollection<ReportItem> reportItems = new ObservableCollection<ReportItem>();
        private ObservableCollection<RouteViewModel> routes = new ObservableCollection<RouteViewModel>();

        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageReports()
        {
            InitializeComponent();
            LoadRoutes();
            RoutesComboBox.ItemsSource = routes;

            if (routes.Count > 0)
                RoutesComboBox.SelectedIndex = 0;

            ReportsDataGrid.ItemsSource = reportItems;

            RoutesComboBox.SelectionChanged += RoutesComboBox_SelectionChanged;
            LowFillCheckBox.Checked += FilterCheckBox_Changed;
            LowFillCheckBox.Unchecked += FilterCheckBox_Changed;
            BackButton.Click += BackButton_Click;
        }

        private void LoadRoutes()
        {
            routes.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Number, Distance, FromCity, ToCity FROM Routes ORDER BY Number", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    routes.Add(new RouteViewModel
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

        private void LoadReport(int routeId, bool filterLowFill)
        {
            reportItems.Clear();
            if (routeId == 0) return;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = @"
                    SELECT 
                        r.Number AS RouteNumber,
                        r.FromCity,
                        r.ToCity,
                        a.Model AS AircraftModel,
                        a.Number AS AircraftNumber,
                        f.DepartureDate,
                        f.ArrivalDate,
                        a.Seats,
                        f.TicketsSold
                    FROM Flights f
                    INNER JOIN Routes r ON f.RouteId = r.Id
                    INNER JOIN Aircrafts a ON f.AircraftId = a.Id
                    WHERE r.Id = @routeId";

                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@routeId", routeId);

                var reader = cmd.ExecuteReader();

                List<ReportItem> tempList = new List<ReportItem>();

                while (reader.Read())
                {
                    int seats = reader.GetInt32(reader.GetOrdinal("Seats"));
                    int ticketsSold = reader.GetInt32(reader.GetOrdinal("TicketsSold"));
                    int freeSeats = seats - ticketsSold;
                    double occupancyRate = seats != 0 ? (double)ticketsSold / seats : 0;
                    if (filterLowFill && occupancyRate >= 0.7)
                        continue;

                    DateTime departure = reader.GetDateTime(reader.GetOrdinal("DepartureDate"));
                    DateTime arrival = reader.GetDateTime(reader.GetOrdinal("ArrivalDate"));
                    TimeSpan estimatedFlightTime = arrival - departure;

                    tempList.Add(new ReportItem
                    {
                        RouteNumber = reader.GetString(reader.GetOrdinal("RouteNumber")),
                        FromCity = reader.GetString(reader.GetOrdinal("FromCity")),
                        ToCity = reader.GetString(reader.GetOrdinal("ToCity")),
                        AircraftModel = reader.GetString(reader.GetOrdinal("AircraftModel")),
                        AircraftNumber = reader.GetString(reader.GetOrdinal("AircraftNumber")),
                        DepartureDate = departure,
                        ArrivalDate = arrival,
                        FreeSeats = freeSeats,
                        TicketsSold = ticketsSold,
                        EstimatedFlightTime = estimatedFlightTime
                    });
                }
                foreach (var item in tempList)
                    reportItems.Add(item);
                DisplaySummary(tempList);
            }
        }

        private void DisplaySummary(List<ReportItem> reportItems)
        {
            if (reportItems.Count == 0)
            {
                SummaryTextBlock.Text = "Нет данных по выбранным параметрам.";
                return;
            }

            var frequentAircraft = reportItems
                .GroupBy(i => i.AircraftModel)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            var selectedRoute = routes.FirstOrDefault(r => r.Number == reportItems[0].RouteNumber);

            SummaryTextBlock.Text =
                $"Марка самолёта, которая чаще всего летает по маршруту {selectedRoute?.Number} ({selectedRoute?.FromCity} - {selectedRoute?.ToCity}): {frequentAircraft}";
        }

        private void RoutesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoutesComboBox.SelectedItem is RouteViewModel selectedRoute)
            {
                LoadReport(selectedRoute.Id, LowFillCheckBox.IsChecked == true);
            }
        }

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (RoutesComboBox.SelectedItem is RouteViewModel selectedRoute)
            {
                LoadReport(selectedRoute.Id, LowFillCheckBox.IsChecked == true);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService?.Navigate(new Uri("/sprAirport/Project/Admin/pageAdmin.xaml", UriKind.Relative));
            }
        }
    }

    public class ReportItem
    {
        public string RouteNumber { get; set; }
        public string FromCity { get; set; }
        public string ToCity { get; set; }
        public string AircraftModel { get; set; }
        public string AircraftNumber { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int FreeSeats { get; set; }
        public int TicketsSold { get; set; }
        public TimeSpan EstimatedFlightTime { get; set; }

        public string EstimatedFlightTimeDisplay => EstimatedFlightTime.ToString(@"hh\:mm");
    }
}
    