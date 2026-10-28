using System.Reflection;
using System.Windows.Controls;

namespace Nabra.Views
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = "v" + (version != null ? version.ToString(3) : "1.0.0");
        }
    }
}
