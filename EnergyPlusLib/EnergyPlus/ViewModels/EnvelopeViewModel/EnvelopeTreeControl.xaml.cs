using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EnergyPlusLib.DataAccess;


namespace EnergyPlusLib.ViewModel
{
    /// <summary>
    /// Interaction logic for EnvelopeTreeControl.xaml
    /// </summary>
    public partial class EnvelopeTreeControl : UserControl
    {
        public EnvelopeTreeControl()
        {


            InitializeComponent();
            IDFDatabase idf = new IDFDatabase();
            idf.WeatherFilePath = @"C:\EnergyPlusV5-0-0\WeatherData\USA_CA_San.Francisco.Intl.AP.724940_TMY3.epw";
            idf.EnergyPlusRootFolder = @"C:\EnergyPlusV5-0-0\";
            idf.LoadIDDFile(@"C:\EnergyPlusV5-0-0\Energy+.idd");
            idf.LoadIDFFile(@"C:\EnergyPlusV5-0-0\ExampleFiles\BasicsFiles\Exercise2C-Solution.idf");
            EnvelopeTreeViewModel viewModel = new EnvelopeTreeViewModel(idf);
            base.DataContext = viewModel;
        }





        }
    }

