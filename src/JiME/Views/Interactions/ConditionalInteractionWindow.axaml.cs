using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for ConditionalInteractionWindow.xaml
	/// </summary>
	public partial class ConditionalInteractionWindow : Window, INotifyPropertyChanged
	{
		string oldName;

		public Scenario scenario { get; set; }
		public ConditionalInteraction interaction { get; set; }

		public new event PropertyChangedEventHandler PropertyChanged;
		bool closing = false;
		bool _isThreatTriggered;
		public bool isThreatTriggered
		{
			get => _isThreatTriggered;
			set
			{
				_isThreatTriggered = value;
				PropChanged( "isThreatTriggered" );
			}
		}

		public ConditionalInteractionWindow( Scenario s, ConditionalInteraction inter = null )
		{
			InitializeComponent();
			DataContext = this;

			scenario = s;
			cancelButton.IsVisible = inter == null;
			interaction = inter ?? new ConditionalInteraction( "New Conditional Event" );


			isThreatTriggered = scenario.threatObserver.Any( x => x.triggerName == interaction.dataName );
			if ( isThreatTriggered )
			{
				//addMainTriggerButton.IsEnabled = false;
				//triggeredByCB.IsEnabled = false;
				//isTokenCB.IsEnabled = false;
				interaction.isTokenInteraction = false;
			}

			//personRadio.IsChecked = interaction.tokenType == TokenType.Person;
			//searchRadio.IsChecked = interaction.tokenType == TokenType.Search;
			//darkRadio.IsChecked = interaction.tokenType == TokenType.Darkness;
			//threatRadio.IsChecked = interaction.tokenType == TokenType.Threat;

			oldName = interaction.dataName;
		}

		private void isTokenCB_Click( object sender, RoutedEventArgs e )
		{
			//if ( isTokenCB.IsChecked.Value )
			//	interaction.triggerName = "None";
		}

		private void ComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			//this was commented out??
			//if ( !interaction.isFromThreatThreshold )
			//{
			//	if ( interaction.triggerName == "None" )
			//		eventbox.IsVisible = true;
			//	else
			//		eventbox.IsVisible = false;
			//}
			//else
			//	flavorbox.IsVisible = false;
		}

		private async void EditFlavorButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Flavor, interaction.textBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.textBookData.pages = tw.textBookController.pages;
				//flavorTB.Text = tw.textBookController.pages[0];
			}
		}

		private async void EditEventButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Event, interaction.eventBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.eventBookData.pages = tw.textBookController.pages;
				//eventTB.Text = tw.textBookController.pages[0];
			}
		}

		async Task<bool> TryClosing()
		{
			//check for dupe name
			if ( interaction.dataName == "New Conditional Event" || scenario.interactionObserver.Count( x => x.dataName == interaction.dataName ) > 1 )
			{
				await MessageBox.Show( this, "Give this Event a unique name.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			return true;
		}

		private void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( !closing )
				e.Cancel = true;
		}

		private async void OkButton_Click( object sender, RoutedEventArgs e )
		{
			if ( !await TryClosing() )
				return;

			//if ( searchRadio.IsChecked.HasValue && searchRadio.IsChecked.Value )
			//	interaction.tokenType = TokenType.Search;
			//if ( personRadio.IsChecked.HasValue && personRadio.IsChecked.Value )
			//	interaction.tokenType = TokenType.Person;
			//if ( darkRadio.IsChecked.HasValue && darkRadio.IsChecked.Value )
			//	interaction.tokenType = TokenType.Darkness;
			//if ( threatRadio.IsChecked.HasValue && threatRadio.IsChecked.Value )
			//	interaction.tokenType = TokenType.Threat;

			scenario.UpdateEventReferences( oldName, interaction );

			closing = true;
			Close( true );
		}

		private void CancelButton_Click( object sender, RoutedEventArgs e )
		{
			closing = true;
			Close( false );
		}

		private void Window_ContentRendered( object sender, System.EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
			triggerCB.SelectedIndex = 0;
		}

		private async void addMainTriggerAfterButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.triggerAfterName = tw.triggerName;
			}
		}

		private async void addMainTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.triggerName = tw.triggerName;
			}
		}

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private async void tokenHelp_Click( object sender, RoutedEventArgs e )
		{
			HelpWindow hw = new HelpWindow( HelpType.Token, 1 );
			await hw.ShowDialog( this );
		}

		private async void groupHelp_Click( object sender, RoutedEventArgs e )
		{
			HelpWindow hw = new HelpWindow( HelpType.Grouping );
			await hw.ShowDialog( this );
		}

		private void nameTB_TextChanged( object sender, TextChangedEventArgs e )
		{
			interaction.dataName = ( (TextBox)sender ).Text;
			Regex rx = new Regex( @"\sGRP\d+$" );
			MatchCollection matches = rx.Matches( interaction.dataName );
			if ( matches.Count > 0 )
				groupInfo.Text = "This Event is in the following group: " + matches[0].Value.Trim();
			else
				groupInfo.Text = "This Event is in the following group: None";
		}

		private void triggerCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			addSelectedTriggerButton.IsEnabled = triggerCB.SelectedIndex != 0;
		}

		private void addSelectedTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			string t = triggerCB.SelectedValue as string;
			if ( !interaction.triggerList.Contains( t ) )
			{
				interaction.triggerList.Add( t );
			}
		}

		private async void addTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				if ( !interaction.triggerList.Contains( tw.triggerName ) )
					interaction.triggerList.Add( tw.triggerName );
			}
		}

		private void removeTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			string sel = ( (Button)sender ).DataContext as string;
			if ( interaction.triggerList.Contains( sel ) )
				interaction.triggerList.Remove( sel );
		}

		private async void addFinishedTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.finishedTrigger = tw.triggerName;
			}
		}
	}
}
