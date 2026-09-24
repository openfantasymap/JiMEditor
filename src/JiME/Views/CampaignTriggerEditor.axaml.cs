using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for CampaignTriggerEditor.xaml
	/// </summary>
	public partial class CampaignTriggerEditor : Window
	{
		public string triggerName { get; set; }
		public bool isMulti { get; set; }

		Campaign campaign;

		public CampaignTriggerEditor( Campaign c )
		{
			InitializeComponent();
			DataContext = this;
			nameTB.Text = "";
			nameTB.SelectAll();
			campaign = c;
		}

		//parameterless ctor for the XAML previewer
		public CampaignTriggerEditor() : this( new Campaign() ) { }

		private async void okButton_Click( object sender, RoutedEventArgs e )
		{
			triggerName = nameTB.Text.Trim();
			if ( triggerName == "" )
			{
				await MessageBox.Show( this, "The name can't be an empty string.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			triggerName += " [CMPN]";

			if ( campaign.triggerCollection.Any( x => x.dataName == triggerName ) )
			{
				await MessageBox.Show( this, "A Campaign Trigger with this name already exists.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			isMulti = multiCB.IsChecked.Value;
			Close( true );
		}

		private void cancelButton_Click( object sender, RoutedEventArgs e )
		{
			Close( false );
		}

		private async void help_Click( object sender, RoutedEventArgs e )
		{
			HelpWindow hw = new HelpWindow( HelpType.Triggers, 2 );
			await hw.ShowDialog( this );
		}

		private void Window_ContentRendered( object sender, System.EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
		}
	}
}
