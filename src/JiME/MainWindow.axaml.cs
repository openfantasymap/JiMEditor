using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using JiME.Views;

namespace JiME
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public Scenario scenario { get; set; }
		//set once the user confirmed closing a project with unsaved changes
		bool closeConfirmed;

		public MainWindow( Guid campaignGUID ) : this( null )
		{
			scenario.campaignGUID = campaignGUID;
		}

		public MainWindow( Scenario s = null )
		{
			InitializeComponent();

			System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			scenario = s ?? new Scenario();
			scenario.TriggerTitleChange( false );
			DataContext = scenario;
			Debug.Log( scenario.scenarioGUID );

			appVersion.Text = Utils.appVersion;
			formatVersion.Text = Utils.formatVersion;

			interactionsUC.onAddEvent += OnAddEvent;
			interactionsUC.onRemoveEvent += OnRemoveEvent;
			triggersUC.onAddEvent += OnAddTrigger;
			triggersUC.onRemoveEvent += OnRemoveTrigger;
			interactionsUC.onSettingsEvent += OnSettingsInteraction;
			triggersUC.onSettingsEvent += OnSettingsTrigger;
			objectivesUC.onAddEvent += OnAddObjective;
			objectivesUC.onRemoveEvent += OnRemoveObjective;
			objectivesUC.onSettingsEvent += OnSettingsObjective;
			//Debug.Log( this.FindResource( "mylist" ).GetType() );

			//setup source of UI lists (scenario has to be created first!)
			interactionsUC.dataListView.ItemsSource = scenario.interactionObserver;
			triggersUC.dataListView.ItemsSource = scenario.triggersObserver;
			objectivesUC.dataListView.ItemsSource = scenario.objectiveObserver;

			//initialize utilities
			Utils.Init();

			SetupKeyBindings();

			//debug
			//debug();
		}

		void debug()
		{
			//scenario.threatObserver.Add( new Threat( "Threat 1", 10 ) { threshold = 10, triggerName = "Threat Trigger" } );
			//scenario.AddInteraction( new Interaction( "Dummy Event", false ) { interactionType = InteractionType.Text, triggerName = "Threat Trigger" } );

			//scenario.AddInteraction( new TextInteraction( "Dummy Text Interaction" ) );
		}

		#region TOOLBAR ACTIONS
		void OnAddEvent( object sender, EventArgs e )
		{
			ShowNewEventMenu( sender as Control ?? interactionsUC );
		}

		void ShowNewEventMenu( Control target )
		{
			var cm = (ContextMenu)Resources["cmButton"];
			cm.Open( target );
		}

		void OnRemoveEvent( object sender, EventArgs e )
		{
			//TODO check if USED by a THREAT
			int idx = interactionsUC.dataListView.SelectedIndex;
			if ( idx != -1 )
				scenario.RemoveData( interactionsUC.dataListView.SelectedItem );
			interactionsUC.dataListView.SelectedIndex = 0;
		}

		async void OnAddTrigger( object sender, EventArgs e )
		{
			await AddTrigger();
		}

		async void OnRemoveTrigger( object sender, EventArgs e )
		{
			string selected = ( (Trigger)triggersUC.dataListView.SelectedItem ).dataName;
			var strigger = (Trigger)triggersUC.dataListView.SelectedItem;

			Tuple<string, string> used = scenario.IsTriggerUsed( selected );

			if ( used != null )
			{
				await MessageBox.Show( this, $"The selected Trigger [{selected}] is being used by [{used.Item2}] called [{used.Item1}].", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			if ( strigger.isCampaignTrigger )
			{
				await MessageBox.Show( this, "Campaign Triggers cannot be removed.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			int idx = triggersUC.dataListView.SelectedIndex;
			if ( idx != -1 )
				scenario.RemoveData( triggersUC.dataListView.SelectedItem );
			triggersUC.dataListView.SelectedIndex = 0;
		}

		async void OnAddObjective( object sender, EventArgs e )
		{
			await AddObjective();
		}

		async void OnRemoveObjective( object sender, EventArgs e )
		{
			if ( scenario.objectiveObserver.Count > 1 )
			{
				int idx = objectivesUC.dataListView.SelectedIndex;
				if ( idx != -1 )
					scenario.RemoveData( objectivesUC.dataListView.Items[idx] );
				objectivesUC.dataListView.SelectedIndex = 0;
			}
			else
				await MessageBox.Show( this, "There must be at least one Objective.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
		}

		async void OnSettingsInteraction( object sender, EventArgs e )
		{
			if ( interactionsUC.dataListView.SelectedItem is TextInteraction )
			{
				TextInteractionWindow tw = new TextInteractionWindow( scenario, (TextInteraction)interactionsUC.dataListView.SelectedItem );
				await tw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is BranchInteraction )
			{
				BranchInteractionWindow bw = new BranchInteractionWindow( scenario, (BranchInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is TestInteraction )
			{
				TestInteractionWindow bw = new TestInteractionWindow( scenario, (TestInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is DecisionInteraction )
			{
				DecisionInteractionWindow bw = new DecisionInteractionWindow( scenario, (DecisionInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is ThreatInteraction )
			{
				ThreatInteractionWindow bw = new ThreatInteractionWindow( scenario, (ThreatInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is MultiEventInteraction )
			{
				MultiEventWindow bw = new MultiEventWindow( scenario, (MultiEventInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is PersistentTokenInteraction )
			{
				PersistentInteractionWindow bw = new PersistentInteractionWindow( scenario, (PersistentTokenInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is ConditionalInteraction )
			{
				ConditionalInteractionWindow bw = new ConditionalInteractionWindow( scenario, (ConditionalInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is DialogInteraction )
			{
				DialogInteractionWindow bw = new DialogInteractionWindow( scenario, (DialogInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is ReplaceTokenInteraction )
			{
				ReplaceTokenInteractionWindow bw = new ReplaceTokenInteractionWindow( scenario, (ReplaceTokenInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
			else if ( interactionsUC.dataListView.SelectedItem is RewardInteraction )
			{
				RewardInteractionWindow bw = new RewardInteractionWindow( scenario, (RewardInteraction)interactionsUC.dataListView.SelectedItem );
				await bw.ShowDialog<bool?>( this );
			}
		}

		async void OnSettingsTrigger( object sender, EventArgs e )
		{
			string selected = ( (Trigger)triggersUC.dataListView.SelectedItem ).dataName;
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario, selected );
			await tw.ShowDialog<bool?>( this );
		}

		async void OnSettingsObjective( object sender, EventArgs e )
		{
			ObjectiveEditorWindow ow = new ObjectiveEditorWindow( scenario, ( (Objective)objectivesUC.dataListView.SelectedItem ), false );
			await ow.ShowDialog<bool?>( this );
		}
		#endregion

		private void scenarioName_PointerPressed( object sender, PointerPressedEventArgs e )
		{
			if ( scenarioName.IsVisible )
			{
				scenarioName.IsVisible = false;
				scenarioNameEdit.IsVisible = true;
				scenarioNameEdit.Text = scenarioName.Text;
				scenarioNameEdit.Focus();
				scenarioNameEdit.SelectAll();
			}
			else
			{
				scenarioName.IsVisible = true;
				scenarioNameEdit.IsVisible = false;
			}
		}

		private void ScenarioNameEdit_KeyDown( object sender, KeyEventArgs e )
		{
			if ( e.Key == Key.Enter )
			{
				onScenarioNameFocusLost();
			}
		}

		private void scenarioNameEdit_LostFocus( object sender, RoutedEventArgs e )
		{
			onScenarioNameFocusLost();
		}

		async void onScenarioNameFocusLost()
		{
			if ( !scenarioNameEdit.IsVisible )
				return;
			scenarioName.IsVisible = true;
			scenarioNameEdit.IsVisible = false;
			if ( scenarioNameEdit.Text.Trim() != string.Empty )
			{
				scenario.scenarioName = scenarioNameEdit.Text;
			}
			else
				await MessageBox.Show( this, "The Scenario name cannot be an empty string.", "Invalid Scenario Name", MessageBoxButton.OK, MessageBoxImage.Information );
		}

		private async void ScenarioSettingsButton_Click( object sender, RoutedEventArgs e )
		{
			await ScenarioSettings();
		}

		private async void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( !scenario.isDirty || closeConfirmed )
				return;

			//MessageBox is async, so cancel now and close again once confirmed
			e.Cancel = true;
			if ( await ConfirmDiscardChanges() )
			{
				closeConfirmed = true;
				Close();
			}
		}

		async Task<bool> ConfirmDiscardChanges()
		{
			return await MessageBox.Show( this, "The Project has changes that haven't been saved.  Are you sure you want to exit without saving?", "Project Changes Not Saved", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes;
		}

		#region COMMANDS
		/// <summary>
		/// keyboard shortcuts of the WPF editor; Ctrl becomes Cmd on macOS
		/// </summary>
		void SetupKeyBindings()
		{
			KeyModifiers cmd = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
			void Bind( Key key, KeyModifiers modifiers, Action action )
			{
				KeyBindings.Add( new KeyBinding { Gesture = new KeyGesture( key, modifiers ), Command = new RelayCommand( action ) } );
			}

			Bind( Key.X, KeyModifiers.Alt, CommandExit );
			Bind( Key.O, KeyModifiers.Alt, async () => await AddObjective() );
			Bind( Key.T, KeyModifiers.Alt, async () => await AddTrigger() );
			Bind( Key.E, KeyModifiers.Alt, () => ShowNewEventMenu( newEventButton ) );
			Bind( Key.N, cmd, CommandNewProject );
			Bind( Key.O, cmd, CommandOpenProject );
			Bind( Key.S, cmd, () => SaveProject( false ) );
			Bind( Key.S, cmd | KeyModifiers.Alt, () => SaveProject( true ) );
			Bind( Key.S, KeyModifiers.Alt, async () => await ScenarioSettings() );
			Bind( Key.C, KeyModifiers.Alt, async () => await NewChapter() );
		}

		async Task AddObjective()
		{
			ObjectiveEditorWindow ow = new ObjectiveEditorWindow( scenario, new Objective( "Default Short Name - Change This" ) );
			if ( await ow.ShowDialog<bool?>( this ) == true )
			{
				scenario.AddObjective( ow.objective );
			}
		}

		async Task AddTrigger()
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			await tw.ShowDialog<bool?>( this );
		}

		async void CommandExit()
		{
			if ( scenario.isDirty && !await ConfirmDiscardChanges() )
				return;
			closeConfirmed = true;
			Close();
		}

		async void CommandNewProject()
		{
			if ( await MessageBox.Show( this, "Are you sure you want to close this Project and start a new one?", "New Project", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes )
				ReturnToProjectWindow();
		}

		async void CommandOpenProject()
		{
			if ( await MessageBox.Show( this, "Are you sure you want to close this Project and open a different one?", "Open Project", MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes )
				ReturnToProjectWindow();
		}

		void ReturnToProjectWindow()
		{
			ProjectWindow projectWindow = new ProjectWindow();
			projectWindow.Show();
			closeConfirmed = true;
			Close();
		}

		async void SaveProject( bool saveAs )
		{
			FileManager fm = new FileManager( scenario );
			if ( saveAs ? await fm.SaveAs() : await fm.Save() )
			{
				scenario.fileName = fm.fileName;
				scenario.saveDate = fm.saveDate;
				scenario.TriggerTitleChange();
			}
		}

		async Task ScenarioSettings()
		{
			ScenarioWindow sw = new ScenarioWindow( scenario );
			if ( await sw.ShowDialog<bool?>( this ) == true )
			{
				scenario.scenarioName = sw.scenarioName;
			}
		}

		async Task NewChapter()
		{
			ChapterPropertiesWindow cw = new ChapterPropertiesWindow( scenario );
			if ( await cw.ShowDialog<bool?>( this ) == true )
			{
				scenario.AddChapter( cw.chapter );
			}
		}

		async void NewInteraction<T>( T window, Func<T, IInteraction> result ) where T : Window
		{
			if ( await window.ShowDialog<bool?>( this ) == true )
			{
				scenario.AddInteraction( result( window ) );
			}
		}

		private void NewProject_Click( object sender, RoutedEventArgs e ) => CommandNewProject();
		private void OpenProject_Click( object sender, RoutedEventArgs e ) => CommandOpenProject();
		private void SaveProject_Click( object sender, RoutedEventArgs e ) => SaveProject( false );
		private async void NewChapter_Click( object sender, RoutedEventArgs e ) => await NewChapter();
		private async void NewObjective_Click( object sender, RoutedEventArgs e ) => await AddObjective();
		private void NewEvent_Click( object sender, RoutedEventArgs e ) => ShowNewEventMenu( newEventButton );
		private async void NewTrigger_Click( object sender, RoutedEventArgs e ) => await AddTrigger();

		//"New Event" popup menu
		private void NewTextInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new TextInteractionWindow( scenario ), w => w.interaction );
		private void NewBranchInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new BranchInteractionWindow( scenario ), w => w.interaction );
		private void NewThreatInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new ThreatInteractionWindow( scenario ), w => w.interaction );
		private void NewTestInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new TestInteractionWindow( scenario ), w => w.interaction );
		private void NewDecisionInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new DecisionInteractionWindow( scenario ), w => w.interaction );
		private void NewMultiInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new MultiEventWindow( scenario ), w => w.interaction );
		private void NewPersistentInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new PersistentInteractionWindow( scenario ), w => w.interaction );
		private void NewConditionalInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new ConditionalInteractionWindow( scenario ), w => w.interaction );
		private void NewDialogInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new DialogInteractionWindow( scenario ), w => w.interaction );
		private void NewReplaceTokenInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new ReplaceTokenInteractionWindow( scenario ), w => w.interaction );
		private void NewRewardInteraction_Click( object sender, RoutedEventArgs e ) => NewInteraction( new RewardInteractionWindow( scenario ), w => w.interaction );
		#endregion

		private void RemoveChapterButton_Click( object sender, RoutedEventArgs e )
		{
			Chapter c = ( (Control)sender ).DataContext as Chapter;
			foreach ( var tile in c.tileObserver )
				scenario.globalTilePool.Add( tile.idNumber );
			TileSorter sorter = new TileSorter();
			List<int> foo = scenario.globalTilePool.ToList();
			foo.Sort( sorter );
			scenario.globalTilePool.Clear();
			foreach ( int s in foo )
				scenario.globalTilePool.Add( s );
			scenario.RemoveData( c );
		}

		private async void ChapterPropsButton_Click( object sender, RoutedEventArgs e )
		{
			Chapter c = ( (Control)sender ).DataContext as Chapter;

			ChapterPropertiesWindow cw = new ChapterPropertiesWindow( scenario, c );
			await cw.ShowDialog<bool?>( this );
		}

		private async void TileEditButton_Click( object sender, RoutedEventArgs e )
		{
			Chapter c = ( (Control)sender ).DataContext as Chapter;
			if ( c.isRandomTiles )
			{
				TilePoolEditorWindow tp = new TilePoolEditorWindow( scenario, c );
				await tp.ShowDialog<bool?>( this );
			}
			else
			{
				TileEditorWindow tw = new TileEditorWindow( scenario, c );
				await tw.ShowDialog<bool?>( this );
			}
		}
	}
}
