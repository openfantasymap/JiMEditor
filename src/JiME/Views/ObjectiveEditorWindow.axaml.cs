using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for ObjectiveWindow.xaml
	/// </summary>
	public partial class ObjectiveEditorWindow : Window
	{
		public Scenario scenario { get; set; }
		public Objective objective { get; set; }
		public string shortName { get; set; }

		public ObjectiveEditorWindow( Scenario s, Objective obj, bool isNew = true )
		{
			InitializeComponent();
			DataContext = this;

			scenario = s;
			objective = obj;

			shortName = obj.dataName;

			//triggerCB.ItemsSource = scenario.triggersObserver;
			//triggerCB.SelectedItem = (Trigger)scenario.GetData<Trigger>( obj.triggerName );

			if ( !isNew )
				cancelButton.IsVisible = false;
		}

		private async void OkButton_Click( object sender, RoutedEventArgs e )
		{
			objective.dataName = ( shortName ?? "" ).Trim();

			//check empty string
			if ( string.IsNullOrEmpty( objective.dataName ) || string.IsNullOrEmpty( objective.objectiveReminder ) )
			{
				await MessageBox.Show( this, "The Objective Name and Objective Reminder cannot be empty.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			//check if name duplicated
			//string ret = scenario.IsDuplicate( objective );
			if ( scenario.IsDuplicate( objective ) )//ret != null )
			{
				await MessageBox.Show( this, $"An Objective with name [{objective.dataName}] already exists.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return;
			}

			//check if trigger isn't set
			//if ( ( (Trigger)triggerCB.SelectedItem ).dataName == "None"
			//	|| ( (Trigger)triggerCB.SelectedItem ).dataName.Contains( "Random" ) )
			//{
			//	MessageBox.Show( "You must select a Trigger.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
			//	return;
			//}

			//objective.triggerName = ( (Trigger)triggerCB.SelectedItem ).dataName;
			Close( true );
		}

		private void CancelButton_Click( object sender, RoutedEventArgs e )
		{
			Close( false );
		}

		private async void AddTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				triggerCB.SelectedIndex = scenario.triggersObserver.Count - 1;
			}
		}

		private async void addTriggerButton2_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				nextTriggerCB.SelectedIndex = scenario.triggersObserver.Count - 1;
			}
		}

		private async void addTriggeredByButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				triggeredByCB.SelectedIndex = scenario.triggersObserver.Count - 1;
			}
		}

		private async void EditObjectiveButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow te = new TextEditorWindow( scenario, EditMode.Objective, objective.textBookData );
			if ( await te.ShowDialog<bool?>( this ) == true )
			{
				objective.textBookData.pages = te.textBookController.pages;
			}
		}

		private void Window_ContentRendered( object sender, System.EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
		}
	}
}
