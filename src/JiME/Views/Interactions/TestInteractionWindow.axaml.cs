using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for AbilityInteractionWindow.xaml
	/// </summary>
	public partial class TestInteractionWindow : Window, INotifyPropertyChanged
	{
		string oldName;

		public Scenario scenario { get; set; }
		public TestInteraction interaction { get; set; }
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

		public TestInteractionWindow( Scenario s, TestInteraction inter = null )
		{
			InitializeComponent();

			scenario = s;
			cancelButton.IsVisible = inter == null;
			interaction = inter ?? new TestInteraction( "New Stat Test" );

			mightRB.IsChecked = interaction.testAttribute == Ability.Might;
			agilityRB.IsChecked = interaction.testAttribute == Ability.Agility;
			spiritRB.IsChecked = interaction.testAttribute == Ability.Spirit;
			wisdomRB.IsChecked = interaction.testAttribute == Ability.Wisdom;
			witRB.IsChecked = interaction.testAttribute == Ability.Wit;

			mightRB2.IsChecked = interaction.altTestAttribute == Ability.Might;
			agilityRB2.IsChecked = interaction.altTestAttribute == Ability.Agility;
			spiritRB2.IsChecked = interaction.altTestAttribute == Ability.Spirit;
			wisdomRB2.IsChecked = interaction.altTestAttribute == Ability.Wisdom;
			witRB2.IsChecked = interaction.altTestAttribute == Ability.Wit;

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

			//bindings evaluate immediately in Avalonia, so set the DataContext after the properties
			DataContext = this;
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

		async Task<bool> TryClosing()
		{
			//check for dupe name
			if ( interaction.dataName == "New Stat Test" || scenario.interactionObserver.Count( x => x.dataName == interaction.dataName ) > 1 )
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

		private async void EditProgress_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Progress, interaction.progressBookData );
			await tw.ShowDialog( this );
			interaction.progressBookData.pages = tw.textBookController.pages;
		}

		private async void EditFail_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Fail, interaction.failBookData );
			await tw.ShowDialog( this );
			interaction.failBookData.pages = tw.textBookController.pages;
		}

		private async void EditPass_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Pass, interaction.passBookData );
			await tw.ShowDialog( this );
			interaction.passBookData.pages = tw.textBookController.pages;
		}

		private async void AddTriggerPassButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.successTrigger = tw.triggerName;
			}
		}

		private async void AddTriggerFailButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.failTrigger = tw.triggerName;
			}
		}

		private void mightRB_Click( object sender, RoutedEventArgs e )
		{
			interaction.testAttribute = Ability.Might;
		}

		private void agilityRB_Click( object sender, RoutedEventArgs e )
		{
			interaction.testAttribute = Ability.Agility;
		}

		private void spiritRB_Click( object sender, RoutedEventArgs e )
		{
			interaction.testAttribute = Ability.Spirit;
		}

		private void wisdomRB_Click( object sender, RoutedEventArgs e )
		{
			interaction.testAttribute = Ability.Wisdom;
		}

		private void witRB_Click( object sender, RoutedEventArgs e )
		{
			interaction.testAttribute = Ability.Wit;
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

		private void tokenTypeClick( object sender, RoutedEventArgs e )
		{
			RadioButton rb = e.Source as RadioButton;
			if ( ( (string)rb.Content ) == "Person" )
				personType.IsVisible = true;
			else
				personType.IsVisible = false;
		}

		private void mightRB2_Click( object sender, RoutedEventArgs e )
		{
			interaction.altTestAttribute = Ability.Might;
		}

		private void witRB2_Click( object sender, RoutedEventArgs e )
		{
			interaction.altTestAttribute = Ability.Wit;
		}

		private void wisdomRB2_Click( object sender, RoutedEventArgs e )
		{
			interaction.altTestAttribute = Ability.Wisdom;
		}

		private void spiritRB2_Click( object sender, RoutedEventArgs e )
		{
			interaction.altTestAttribute = Ability.Spirit;
		}

		private void agilityRB2_Click( object sender, RoutedEventArgs e )
		{
			interaction.altTestAttribute = Ability.Agility;
		}
	}
}
