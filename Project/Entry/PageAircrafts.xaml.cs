using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace sprAirport.Project.Entry
{
    public partial class PageAircrafts : Page
    {
        private ObservableCollection<AircraftViewModel> Aircrafts = new ObservableCollection<AircraftViewModel>();
        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=sprAirport;Integrated Security=True";

        public PageAircrafts()
        {
            InitializeComponent();
            LoadAircrafts();

            AircraftsDataGrid.ItemsSource = Aircrafts;

            AddAircraftButton.Click += AddAircraftButton_Click;
            UpdateAircraftButton.Click += UpdateAircraftButton_Click;
            DeleteAircraftButton.Click += DeleteAircraftButton_Click;
            AircraftsDataGrid.SelectionChanged += AircraftsDataGrid_SelectionChanged;
        }

        private void LoadAircrafts()
        {
            Aircrafts.Clear();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Number, Model, Seats, Speed FROM Aircrafts ORDER BY Id", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Aircrafts.Add(new AircraftViewModel
                    {
                        Id = reader.GetInt32(0),
                        Number = reader.GetString(1),
                        Model = reader.GetString(2),
                        Seats = reader.GetInt32(3),
                        Speed = reader.GetInt32(4)
                    });
                }
            }
        }

        private void AddAircraftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NumberTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ModelTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SeatsTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SpeedTextBox.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля.");
                    return;
                }

                if (!int.TryParse(SeatsTextBox.Text, out int seats))
                {
                    MessageBox.Show("Неверный формат для Кол-во мест.");
                    return;
                }

                if (!int.TryParse(SpeedTextBox.Text, out int speed))
                {
                    MessageBox.Show("Неверный формат для Скорости.");
                    return;
                }

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"INSERT INTO Aircrafts (Number, Model, Seats, Speed) 
                                               VALUES (@number, @model, @seats, @speed)", conn);
                    cmd.Parameters.AddWithValue("@number", NumberTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@model", ModelTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@seats", seats);
                    cmd.Parameters.AddWithValue("@speed", speed);
                    cmd.ExecuteNonQuery();
                }

                LoadAircrafts();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении самолёта: " + ex.Message);
            }
        }

        private void UpdateAircraftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AircraftsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите самолёт для изменения.");
                    return;
                }

                if (!int.TryParse(SeatsTextBox.Text, out int seats))
                {
                    MessageBox.Show("Неверный формат для Кол-во мест.");
                    return;
                }

                if (!int.TryParse(SpeedTextBox.Text, out int speed))
                {
                    MessageBox.Show("Неверный формат для Скорости.");
                    return;
                }

                var aircraft = (AircraftViewModel)AircraftsDataGrid.SelectedItem;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"UPDATE Aircrafts SET Number = @number, Model = @model, Seats = @seats, Speed = @speed WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@number", NumberTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@model", ModelTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@seats", seats);
                    cmd.Parameters.AddWithValue("@speed", speed);
                    cmd.Parameters.AddWithValue("@id", aircraft.Id);
                    cmd.ExecuteNonQuery();
                }

                LoadAircrafts();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении самолёта: " + ex.Message);
            }
        }

        private void DeleteAircraftButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AircraftsDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите самолёт для удаления.");
                    return;
                }

                var aircraft = (AircraftViewModel)AircraftsDataGrid.SelectedItem;

                if (MessageBox.Show($"Удалить самолёт {aircraft.Number}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM Aircrafts WHERE Id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", aircraft.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LoadAircrafts();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении самолёта: " + ex.Message);
            }
        }

        private void AircraftsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var aircraft = (AircraftViewModel)AircraftsDataGrid.SelectedItem;
            if (aircraft == null) return;

            NumberTextBox.Text = aircraft.Number;
            ModelTextBox.Text = aircraft.Model;
            SeatsTextBox.Text = aircraft.Seats.ToString();
            SpeedTextBox.Text = aircraft.Speed.ToString();
        }

        private void ClearForm()
        {
            NumberTextBox.Text = "";
            ModelTextBox.Text = "";
            SeatsTextBox.Text = "";
            SpeedTextBox.Text = "";
            AircraftsDataGrid.SelectedItem = null;
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

    public class AircraftViewModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Model { get; set; }
        public int Seats { get; set; }
        public int Speed { get; set; }
    }
}
