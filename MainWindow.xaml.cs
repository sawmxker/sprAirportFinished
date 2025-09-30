using sprAirport.Project;
using sprAirport.Project.PagesAccs;
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

namespace sprAirport
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FrmMain.Navigate(new Project.PagesAccs.LoginPage());
            AppConnect.modelOdb = new sprAirportEntities1();
            AppFrame.frameMain = FrmMain;
        }
    }
}
