using System.Windows;
using SessionHub.ViewModels;

namespace SessionHub
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			
		}
		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			DataContext = new MainViewModel();
		}
	}
}