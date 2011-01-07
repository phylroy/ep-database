using System.Windows.Controls;
using EnergyPlusLib.ViewModel;

namespace EnergyPlusLib.EnergyPlus.ViewModels.EnvelopeViewModel
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

