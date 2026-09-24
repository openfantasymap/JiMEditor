using System;
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
	/// Interaction logic for DialogInteractionWindow.xaml
	/// </summary>
	public partial class DialogInteractionWindow : Window, INotifyPropertyChanged
	{
		string oldName;
		public Scenario scenario { get; set; }
		public DialogInteraction interaction { get; set; }
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

		public DialogInteractionWindow( Scenario s, DialogInteraction inter = null )
		{
			InitializeComponent();
			DataContext = this;

			scenario = s;
			cancelButton.IsVisible = inter == null;
			interaction = inter ?? new DialogInteraction( "New Dialog Event" );

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

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private async void okButton_Click( object sender, RoutedEventArgs e )
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

		private void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( !closing )
				e.Cancel = true;
		}

		async Task<bool> TryClosing()
		{
			//check for dupe name
			if ( interaction.dataName == "New Dialog Event" || scenario.interactionObserver.Count( x => x.dataName == interaction.dataName ) > 1 )
			{
				await MessageBox.Show( this, "Give this Event a unique name.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			return true;
		}

		private void Window_ContentRendered( object sender, EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
		}

		private async void story1_Click( object sender, RoutedEventArgs e )
		{
			TextBookData tbd = new TextBookData( "Dialog Text" );
			tbd.pages.Add( interaction.c1Text );

			TextEditorWindow te = new TextEditorWindow( scenario, EditMode.Dialog, tbd );
			if ( await te.ShowDialog<bool?>( this ) == true )
			{
				interaction.c1Text = te.textBookController.pages[0];
			}
		}

		private async void story2_Click( object sender, RoutedEventArgs e )
		{
			TextBookData tbd = new TextBookData( "Dialog Text" );
			tbd.pages.Add( interaction.c2Text );

			TextEditorWindow te = new TextEditorWindow( scenario, EditMode.Dialog, tbd );
			if ( await te.ShowDialog<bool?>( this ) == true )
			{
				interaction.c2Text = te.textBookController.pages[0];
			}
		}

		private async void story3_Click( object sender, RoutedEventArgs e )
		{
			TextBookData tbd = new TextBookData( "Dialog Text" );
			tbd.pages.Add( interaction.c3Text );

			TextEditorWindow te = new TextEditorWindow( scenario, EditMode.Dialog, tbd );
			if ( await te.ShowDialog<bool?>( this ) == true )
			{
				interaction.c3Text = te.textBookController.pages[0];
			}
		}

		private async void addTrigger1Button_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.c1Trigger = tw.triggerName;
			}
		}

		private async void addTrigger2Button_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.c2Trigger = tw.triggerName;
			}
		}

		private async void addTrigger3Button_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.c3Trigger = tw.triggerName;
			}
		}

		private void cancelButton_Click( object sender, RoutedEventArgs e )
		{
			closing = true;
			Close( false );
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
			RadioButton rb = sender as RadioButton;
			if ( ( (string)rb.Content ) == "Person" )
				personType.IsVisible = true;
			else
				personType.IsVisible = false;
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

		private async void addMainTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.triggerName = tw.triggerName;
			}
		}

		private async void addMainTriggerAfterButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				interaction.triggerAfterName = tw.triggerName;
			}
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

		private async void editPersText_Click( object sender, RoutedEventArgs e )
		{
			TextBookData tbd = new TextBookData( "Dialog Text" );
			tbd.pages.Add( interaction.persistentText );

			TextEditorWindow te = new TextEditorWindow( scenario, EditMode.Persistent, tbd );
			if ( await te.ShowDialog<bool?>( this ) == true )
			{
				interaction.persistentText = te.textBookController.pages[0];
			}
		}
	}
}
