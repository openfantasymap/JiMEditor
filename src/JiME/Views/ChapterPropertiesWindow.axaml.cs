using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for ChapterPropertiesWindow.xaml
	/// </summary>
	public partial class ChapterPropertiesWindow : Window
	{
		public Chapter chapter { get; set; }
		public ObservableCollection<string> randomInteractions { get; set; }
		public Scenario scenario { get; set; }
		bool closing = false/*, currentRandomToggle*/;
		int numinters = 0, requestedInters = 0;

		public ChapterPropertiesWindow( Scenario s, Chapter c = null )
		{
			InitializeComponent();

			this.HideMinimizeAndMaximizeButtons();

			cancelButton.IsVisible = c == null;
			chapter = c ?? new Chapter( "New Block" );
			scenario = s;
			//currentRandomToggle = chapter.isRandomTiles;
			if ( chapter.dataName != "Start" )
			{
				preExCB.IsVisible = false;
				preExText.IsVisible = false;
			}
			else
			{//disable some settings for Start block
				useRandomCB.IsEnabled = false;
				randomBlock.IsVisible = false;
				//hintBlock.IsVisible = false;
				dynamicCB.IsVisible = false;
				dynText.IsVisible = false;
				flavorBox.IsVisible = false;
				exploreBox.IsVisible = false;
			}

			var ri = from inter in scenario.interactionObserver where inter.isTokenInteraction select inter.dataName;
			HashSet<string> hash = new HashSet<string>( new string[] { "None" } );
			Regex rx = new Regex( @"\sGRP\d+$" );
			foreach ( string item in ri )
			{
				MatchCollection matches = rx.Matches( item );
				if ( matches.Count > 0 )
				{
					//make sure all Events in the group are token interactions
					var isToken = scenario.interactionObserver.Where( x => x.dataName.Contains( matches[0].Value ) ).All( x => x.isTokenInteraction );
					if ( isToken )
						hash.Add( matches[0].Value.Trim() );
				}
			}
			randomInteractions = new ObservableCollection<string>( hash );
			//pre-compute the group counts so the bound TextBox isn't clamped to 0 when the bindings apply
			if ( chapter.randomInteractionGroup != null )
				numinters = scenario.interactionObserver.Count( x => x.dataName != "None" && x.dataName.EndsWith( chapter.randomInteractionGroup ) );
			requestedInters = chapter.randomInteractionGroupCount;
			//set the DataContext once all bound properties exist (Avalonia binds immediately, WPF deferred it)
			DataContext = this;
			randInter.SelectedItem = chapter.randomInteractionGroup;

			UpdateTexts();
		}

		private async void OkButton_Click( object sender, RoutedEventArgs e )
		{
			if ( !await TryClose() )
				return;
			closing = true;
			Close( true );
		}

		private void CancelButton_Click( object sender, RoutedEventArgs e )
		{
			//return any tiles back into the global pool
			foreach ( var tile in chapter.tileObserver )
				scenario.globalTilePool.Add( tile.idNumber );
			List<int> sorted = scenario.globalTilePool.OrderBy( key => key ).ToList();
			for ( int i = 0; i < sorted.Count; i++ )
				scenario.globalTilePool[i] = sorted[i];


			closing = true;
			Close( false );
		}

		async Task<bool> TryClose()
		{
			if ( useRandomCB.IsChecked != true )
			{
				chapter.randomInteractionGroup = "None";
				randInter.SelectedItem = "None";
			}

			if ( chapter.dataName != "Start"
				&& ( chapter.triggeredBy == "None" /*|| chapter.triggeredBy == "Trigger Random Event" */) )
			{
				await MessageBox.Show( this, "'Triggered By' cannot be set to None.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}
			else if ( string.IsNullOrWhiteSpace( chapter.dataName ) )
			{
				await MessageBox.Show( this, "'Chapter Name' cannot be empty.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
				return false;
			}

			chapter.randomInteractionGroup = randInter.SelectedItem as string;

			return true;
		}

		private async void EditFlavorButton_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Flavor, chapter.flavorBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
				chapter.flavorBookData.pages = tw.textBookController.pages;
		}

		private void Window_ContentRendered( object sender, System.EventArgs e )
		{
			nameTB.Focus();
			nameTB.SelectAll();
		}

		private async void AddExploreTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				chapter.exploreTrigger = tw.triggerName;
			}
		}

		private async void AddTriggerByButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				chapter.triggeredBy = tw.triggerName;
			}
		}

		private async void TileEditButton_Click( object sender, RoutedEventArgs e )
		{
			if ( chapter.isRandomTiles )
			{
				TilePoolEditorWindow tp = new TilePoolEditorWindow( scenario, chapter );
				await tp.ShowDialog( this );
				UpdateTexts();
			}
			else
			{
				TileEditorWindow tw = new TileEditorWindow( scenario, chapter );
				await tw.ShowDialog( this );
				UpdateTexts();
			}
		}

		private async void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( closing )
				return;
			//TryClose may show a message box, so cancel now and close again once validated
			e.Cancel = true;
			if ( await TryClose() )
			{
				closing = true;
				Close( false );
			}
		}

		private async void groupHelp_Click( object sender, RoutedEventArgs e )
		{
			HelpWindow hw = new HelpWindow( HelpType.Grouping, 1 );
			await hw.ShowDialog( this );
		}

		private void randInter_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			//get # of interactions in selected group, then update selectedInfoText
			ComboBox cb = sender as ComboBox;
			if ( cb?.SelectedItem == null )
				return;
			numinters = scenario.interactionObserver.Count( x => x.dataName != "None" && x.dataName.EndsWith( cb.SelectedItem.ToString() ) );
			UpdateTexts();
		}

		private void numIntersUsed_TextChanged( object sender, TextChangedEventArgs e )
		{
			if ( chapter == null )
				return;
			Int32.TryParse( ( (TextBox)sender ).Text, out requestedInters );
			UpdateTexts();
		}

		private void useRandomCB_Click( object sender, RoutedEventArgs e )
		{
			//clear tokenlist if using a random group
			//if ( ( (CheckBox)sender ).IsChecked.Value )
			//{
			//	for ( int i = 0; i < chapter.tileObserver.Count; i++ )
			//	{
			//		( (HexTile)chapter.tileObserver[i] ).tokenList.Clear();
			//	}
			//}
			//else

			//clear random group name if NOT using random groups
			if ( ( (CheckBox)sender ).IsChecked != true )
			{
				chapter.randomInteractionGroup = "None";
				randInter.SelectedItem = "None";
				chapter.randomInteractionGroupCount = 0;
			}
		}

		private async void RandomToggleCB_Click( object sender, RoutedEventArgs e )
		{
			if ( !chapter.isRandomTiles )
			{
				await MessageBox.Show( this, "Switching back to fixed tiles will automatically change any random tiles using a Random Side to Side A.\r\n\r\nReminder: Be sure to use the Tile Editor to properly place your tiles, now that they will be in fixed, user-defined positions.", "Switching to Fixed Tiles", MessageBoxButton.OK, MessageBoxImage.Information );
				foreach ( var tile in chapter.tileObserver )
				{
					if ( ( (HexTile)tile ).tileSide == "Random" )
						( (HexTile)tile ).tileSide = "A";
				}
			}
			//if ( chapter.isRandomTiles != currentRandomToggle )
			//{
			//	var ret = MessageBox.Show( "Are you sure you want to toggle between Random Tiles and Fixed Tiles?\n\nALL TILE DATA IN THIS CHAPTER WILL BE RESET IF YOU SWITCH.", "Switch Between Random and Fixed Tiles", MessageBoxButton.YesNo, MessageBoxImage.Question );
			//	if ( ret == MessageBoxResult.Yes )
			//	{
			//		currentRandomToggle = chapter.isRandomTiles;
			//		if ( chapter.isRandomTiles )
			//		{
			//			foreach ( HexTile tile in chapter.tileObserver )
			//				scenario.globalTilePool.Add( tile.idNumber );
			//			chapter.tileObserver.Clear();
			//		}
			//		else
			//		{
			//			foreach ( int tile in chapter.randomTilePool )
			//				scenario.globalTilePool.Add( tile );
			//			chapter.randomTilePool.Clear();
			//		}

			//		List<int> sorted = scenario.globalTilePool.OrderBy( key => key ).ToList();
			//		for ( int i = 0; i < sorted.Count; i++ )
			//			scenario.globalTilePool[i] = sorted[i];
			//	}
			//	else
			//		chapter.isRandomTiles = currentRandomToggle;
			//}
		}

		void UpdateTexts()
		{

			int fixedTokenCount = 0;
			if ( chapter.tileObserver.Count > 0 )
				fixedTokenCount = chapter.tileObserver.Select( x => (HexTile)x ).Select( x => x.tokenList.Count ).Aggregate( ( acc, cur ) => acc + cur );

			selectedInfoText.Text = $"There are {numinters} Events in the selected Group.";
			fixedCountText.Text = $"There are {fixedTokenCount} fixed Tokens in this Block.";

			int numspaces = chapter.tileObserver.Aggregate( 0, ( acc, cur ) =>
			{
				return acc + ( cur.idNumber / 100 ) % 10;
			} );
			spaceInfoText2.Text = $"There are {numspaces} total spaces available on this Block's tiles to place Tokens.";

			int max = Math.Min( numinters, numspaces - fixedTokenCount );
			numIntersUsedText.Text = $"Randomly use how many of the Events from the selected Interaction Group, up to a maximum of {max}:";

			max = Math.Min( requestedInters, max );
			numIntersUsed.Text = max.ToString();
		}
	}
}
