using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Text.RegularExpressions;


namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for MultiEventWindow.xaml
	/// </summary>
	public partial class MultiEventWindow : Window, INotifyPropertyChanged
	{
		string oldName;

		public Scenario scenario { get; set; }
		public MultiEventInteraction interaction { get; set; }
		bool closing = false;

		public new event PropertyChangedEventHandler PropertyChanged;
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

		public MultiEventWindow( Scenario s, MultiEventInteraction inter = null )
		{
			InitializeComponent();
			DataContext = this;

			scenario = s;
			cancelButton.IsVisible = inter == null;
			interaction = inter ?? new MultiEventInteraction( "New Multi-Event" );

			triggerRB.IsChecked = interaction.usingTriggers;
			eventRB.IsChecked = !interaction.usingTriggers;

			meTriggerBox.IsEnabled = triggerRB.IsChecked.Value;
			meEventBox.IsEnabled = eventRB.IsChecked.Value;
			eventbox.IsEnabled = !interaction.isSilent;

			isThreatTriggered = scenario.threatObserver.Any( x => x.triggerName == interaction.dataName );
			if ( isThreatTriggered )
			{
				addMainTriggerButton.IsEnabled = false;
				triggeredByCB.IsEnabled = false;
				isTokenCB.IsEnabled = false;
				interaction.isTokenInteraction = false;
			}

			if ( interaction.isTokenInteraction && interaction.tokenType == TokenType.Person )
				personType.IsVisible = true;
			humanRadio.IsChecked = interaction.personType == PersonType.Human;
			elfRadio.IsChecked = interaction.personType == PersonType.Elf;
			hobbitRadio.IsChecked = interaction.personType == PersonType.Hobbit;
			dwarfRadio.IsChecked = interaction.personType == PersonType.Dwarf;

			personRadio.IsChecked = interaction.tokenType == TokenType.Person;
			searchRadio.IsChecked = interaction.tokenType == TokenType.Search;
			darkRadio.IsChecked = interaction.tokenType == TokenType.Darkness;
			threatRadio.IsChecked = interaction.tokenType == TokenType.Threat;

			oldName = interaction.dataName;
		}

		private void isTokenCB_Click( object sender, RoutedEventArgs e )
		{
			if ( isTokenCB.IsChecked == true )
			{
				interaction.triggerName = "None";
				personType.IsVisible = personRadio.IsChecked == true;
			}
			else
				personType.IsVisible = false;
		}

		private async void EditFlavorButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Flavor, interaction.textBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.textBookData.pages = tw.textBookController.pages;
			}
		}

		private async void EditEventButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Event, interaction.eventBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.eventBookData.pages = tw.textBookController.pages;
			}
		}

		private async void OkButton_Click( object sender, RoutedEventArgs e )
		{
			if ( !await TryClosing() )
				return;

			if ( searchRadio.IsChecked.HasValue && searchRadio.IsChecked.Value )
				interaction.tokenType = TokenType.Search;
			if ( personRadio.IsChecked.HasValue && personRadio.IsChecked.Value )
				interaction.tokenType = TokenType.Person;
			if ( darkRadio.IsChecked.HasValue && darkRadio.IsChecked.Value )
				interaction.tokenType = TokenType.Darkness;
			if ( threatRadio.IsChecked.HasValue && threatRadio.IsChecked.Value )
				interaction.tokenType = TokenType.Threat;

			if ( humanRadio.IsChecked == true )
				interaction.personType = PersonType.Human;
			if ( elfRadio.IsChecked == true )
				interaction.personType = PersonType.Elf;
			if ( hobbitRadio.IsChecked == true )
				interaction.personType = PersonType.Hobbit;
			if ( dwarfRadio.IsChecked == true )
				interaction.personType = PersonType.Dwarf;

			scenario.UpdateEventReferences( oldName, interaction );

			interaction.usingTriggers = triggerRB.IsChecked.Value;

			closing = true;
			Close( true );
		}

		private void CancelButton_Click( object sender, RoutedEventArgs e )
		{
			closing = true;
			Close( false );
		}

		private void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( !closing )
				e.Cancel = true;
		}

		async Task<bool> TryClosing()
		{
			//check for dupe name
			if ( interaction.dataName == "New Multi-Event" || scenario.interactionObserver.Count( x => x.dataName == interaction.dataName ) > 1 )
			{
				await MessageBox.Show( this, "Give this Event a unique name.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			return true;
		}

		private void Window_ContentRendered( object sender, System.EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
			triggerCB.SelectedIndex = 0;
			eventCB.SelectedIndex = 0;
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

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private void removeTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			string sel = ( (Button)sender ).DataContext as string;
			if ( interaction.triggerList.Contains( sel ) )
				interaction.triggerList.Remove( sel );
		}

		private void addSelectedTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			string t = triggerCB.SelectedValue as string;
			if ( !interaction.triggerList.Contains( t ) )
			{
				interaction.triggerList.Add( t );
			}
		}

		private async void AddTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				if ( !interaction.triggerList.Contains( tw.triggerName ) )
					interaction.triggerList.Add( tw.triggerName );
			}
		}

		private void removeEventButton_Click( object sender, RoutedEventArgs e )
		{
			string sel = ( (Button)sender ).DataContext as string;
			if ( interaction.eventList.Contains( sel ) )
				interaction.eventList.Remove( sel );
		}

		private void addSelectedEventButton_Click( object sender, RoutedEventArgs e )
		{
			string t = eventCB.SelectedValue as string;
			if ( !interaction.eventList.Contains( t ) )
			{
				interaction.eventList.Add( t );
			}
		}

		private void triggerRB_Click( object sender, RoutedEventArgs e )
		{
			meTriggerBox.IsEnabled = true;
			meEventBox.IsEnabled = false;
		}

		private void eventRB_Click( object sender, RoutedEventArgs e )
		{
			meTriggerBox.IsEnabled = false;
			meEventBox.IsEnabled = true;
		}

		private void silenCB_Click( object sender, RoutedEventArgs e )
		{
			eventbox.IsEnabled = !( (CheckBox)sender ).IsChecked.Value;
		}

		private void eventCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			addSelectedEventButton.IsEnabled = eventCB.SelectedIndex != 0;
		}

		private void triggerCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			addSelectedTriggerButton.IsEnabled = triggerCB.SelectedIndex != 0;
		}

		private void tokenTypeClick( object sender, RoutedEventArgs e )
		{
			RadioButton rb = sender as RadioButton;
			if ( ( (string)rb.Content ) == "Person" )
				personType.IsVisible = true;
			else
				personType.IsVisible = false;
		}
	}
}
