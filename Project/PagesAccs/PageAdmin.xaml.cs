using sprAirport.Project.Entry;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace sprAirport.Project.PagesAccs
{
    public partial class PageAdmin : Page
    {
        public PageAdmin()
        {
            InitializeComponent();
        }

        private void ManagePlanes_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу управления самолетами
            this.NavigationService?.Navigate(new PageAircrafts());
        }

        private void ManageRoutes_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу управления маршрутами
            this.NavigationService?.Navigate(new PageRoutes());
        }

        private void ManageFlights_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу управления рейсами
            this.NavigationService?.Navigate(new PageFlights());
        }

        private void ViewReports_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу отчетов
            this.NavigationService?.Navigate(new PageReports());
        }

        private void ManageUsers_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу управления пользователями
            this.NavigationService?.Navigate(new PageUsersManagement());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Попробуем вернуться назад в навигации, иначе просто закрыть окно/вернуть пользователя
            try
            {
                if (this.NavigationService != null && this.NavigationService.CanGoBack)
                {
                    this.NavigationService.GoBack();
                }
                else
                {
                    // Если навигации нет — закрываем приложение (или можно показать окно логина)
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