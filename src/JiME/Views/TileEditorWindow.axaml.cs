using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for TileEditorWindow.xaml
	/// </summary>
	public partial class TileEditorWindow : Window, INotifyPropertyChanged
	{
		HexTile _selected;
		public HexTile selected
		{
			get => _selected;
			set
			{
				_selected = value;
				PropChanged( "selected" );
			}
		}
		public Chapter chapter { get; set; }
		public Scenario scenario { get; set; }

		bool dragging;
		//canvas drawing of each tile in the chapter
		readonly Dictionary<HexTile, HexTileVisual> visuals = new Dictionary<HexTile, HexTileVisual>();

		public new event PropertyChangedEventHandler PropertyChanged;

		public TileEditorWindow( Scenario s, Chapter c = null )
		{
			InitializeComponent();

			scenario = s;
			chapter = c ?? new Chapter( "New Block" );
			selected = null;
			//after scenario/chapter are set: Avalonia evaluates bindings as soon as the DataContext is assigned
			DataContext = this;

			//rehydrate existing tiles in this chapter
			for ( int i = 0; i < chapter.tileObserver.Count; i++ )
			{
				HexTile hex = (HexTile)chapter.tileObserver[i];
				hex.useGraphic = scenario.useTileGraphics;
				HexTileVisual v = Visual( hex );
				v.Rehydrate( canvas );
				v.ChangeColor( i );
				v.ToggleGraphic( canvas );
			}

			editTokenButton.IsEnabled = !chapter.usesRandomGroups;
			disabledMessage.IsVisible = chapter.usesRandomGroups;

			AddHandler( KeyDownEvent, Window_PreviewKeyDown, RoutingStrategies.Tunnel );
		}

		HexTileVisual Visual( HexTile tile )
		{
			if ( !visuals.TryGetValue( tile, out HexTileVisual v ) )
				visuals[tile] = v = new HexTileVisual( tile );
			return v;
		}

		void UnselectAll()
		{
			foreach ( HexTile tt in chapter.tileObserver )
				Visual( tt ).Unselect();
		}

		int GetUnusedColor()
		{
			int[] used = chapter.tileObserver.Select( x => ( (HexTile)x ).color ).ToArray();
			for ( int i = 0; i < chapter.tileObserver.Count; i++ )
			{
				if ( i < used.Length && !used.Contains( i ) )
					return i;
			}
			return chapter.tileObserver.Count;
		}

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private void OkButton_Click( object sender, RoutedEventArgs e )
		{
			Close( true );
		}

		private void Canvas_PointerReleased( object sender, PointerReleasedEventArgs e )
		{
			dragging = false;
		}

		private void Canvas_PointerMoved( object sender, PointerEventArgs e )
		{
			if ( dragging && selected != null )
			{
				Visual( selected ).Drag( e.GetPosition( canvas ) );
			}
		}

		private void Window_PreviewKeyDown( object sender, KeyEventArgs e )
		{
			if ( selected != null )
			{
				if ( e.Key == Key.PageUp )
					Visual( selected ).Rotate( 60, canvas );
				else if ( e.Key == Key.PageDown )
					Visual( selected ).Rotate( -60, canvas );
				//Backspace too: Mac keyboards have no Delete key
				else if ( ( e.Key == Key.Delete || e.Key == Key.Back ) && !( e.Source is TextBox ) )
					RemoveSelectedTile();
			}
		}

		void RemoveSelectedTile()
		{
			scenario.globalTilePool.Add( selected.idNumber );
			Visual( selected ).RemoveFrom( canvas );
			visuals.Remove( selected );
			chapter.RemoveTile( selected );
			selected = null;
			//sort list
			TileSorter sorter = new TileSorter();
			List<int> foo = scenario.globalTilePool.ToList();
			foo.Sort( sorter );
			scenario.globalTilePool.Clear();
			foreach ( int s in foo )
				scenario.globalTilePool.Add( s );
			tilePool.SelectedIndex = 0;
		}

		private void AddTileButton_Click( object sender, RoutedEventArgs e )
		{
			if ( tilePool.SelectedIndex == -1 || chapter.tileObserver.Count == 5 )
				return;

			UnselectAll();

			int id = (int)tilePool.SelectedItem;
			int color = GetUnusedColor();
			HexTile hex = new HexTile( id );
			hex.useGraphic = scenario.useTileGraphics;
			HexTileVisual v = Visual( hex );
			v.ChangeColor( color );
			chapter.AddTile( hex );
			canvas.Children.Add( v.hexPathShape );
			if ( scenario.useTileGraphics )
				canvas.Children.Add( v.tileImage );
			selected = hex;
			v.Select();
			radioA.IsChecked = selected.tileSide == "A";
			radioB.IsChecked = selected.tileSide == "B";
			inChapterCB.SelectedIndex = chapter.tileObserver.Count - 1;
			scenario.globalTilePool.Remove( id );
		}

		private void removeTileButton_Click( object sender, RoutedEventArgs e )
		{
			if ( selected != null )
				RemoveSelectedTile();
		}

		/// <summary>
		/// tiles in chapter CB
		/// </summary>
		private void ComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			UnselectAll();

			selected = null;

			var s = ( (ComboBox)sender ).SelectedItem as HexTile;
			if ( s != null )
			{
				var t = ( from HexTile tile in chapter.tileObserver where s.GUID == tile.GUID select tile ).FirstOr( null );
				if ( t != null )
				{
					selected = t;
					Visual( selected ).Select();
					radioA.IsChecked = selected.tileSide == "A";
					radioB.IsChecked = selected.tileSide == "B";
					tokenCount.Text = "Tokens in Tile: " + t.tokenList.Count;
				}
			}
		}

		private void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			canvas.Children.Clear();
		}

		private async void editTokenButton_Click( object sender, RoutedEventArgs e )
		{
			if ( selected == null )
				return;
			Visual( selected ).Unselect();
			TokenEditorWindow tw = new TokenEditorWindow( selected, scenario );
			selected = null;
			await tw.ShowDialog<bool?>( this );
		}

		private async void canvas_PointerPressed( object sender, PointerPressedEventArgs e )
		{
			if ( !e.GetCurrentPoint( canvas ).Properties.IsLeftButtonPressed )
				return;

			if ( e.ClickCount == 1 )
			{
				UnselectAll();

				selected = null;

				//topmost tile under the pointer (tiles added later are drawn on top)
				Point p = e.GetPosition( canvas );
				HexTile hit = chapter.tileObserver.OfType<HexTile>().LastOrDefault( t => Visual( t ).HitTest( p ) );
				if ( hit != null )
				{
					selected = hit;
					HexTileVisual v = Visual( selected );
					v.Select();
					dragging = true;
					v.SetClickV( p );
					inChapterCB.SelectedItem = selected;
					radioA.IsChecked = selected.tileSide == "A";
					radioB.IsChecked = selected.tileSide == "B";
					tokenCount.Text = "Tokens in Tile: " + selected.tokenList.Count;
				}
			}

			//double click
			else if ( e.ClickCount == 2 )
			{
				dragging = false;
				if ( selected == null )
					return;

				TokenEditorWindow tw = new TokenEditorWindow( selected, scenario );
				Visual( selected ).Unselect();
				selected = null;
				await tw.ShowDialog<bool?>( this );
			}
		}

		private void radioA_Click( object sender, RoutedEventArgs e )
		{
			if ( selected != null && selected.tileSide != "A" )
				Visual( selected ).ChangeTileSide( "A", canvas );
		}

		private void radioB_Click( object sender, RoutedEventArgs e )
		{
			if ( selected != null && selected.tileSide != "B" )
				Visual( selected ).ChangeTileSide( "B", canvas );
		}

		private async void tileGalleryButton_Click( object sender, RoutedEventArgs e )
		{
			GalleryWindow gw = new GalleryWindow( scenario, chapter.tileObserver.Count );
			if ( await gw.ShowDialog<bool?>( this ) == true && gw.selectedData.Length > 0 )
			{
				UnselectAll();

				foreach ( var t in gw.selectedData )
				{
					int color = GetUnusedColor();
					HexTile hex = new HexTile( t.Item1 );
					hex.useGraphic = scenario.useTileGraphics;
					HexTileVisual v = Visual( hex );
					v.ChangeColor( color );
					//ChangeTileSide() also rehydrates and adds the shape and tile image to the canvas
					v.ChangeTileSide( t.Item2, canvas );
					chapter.AddTile( hex );
					selected = hex;
					v.Select();
					radioA.IsChecked = selected.tileSide == "A";
					radioB.IsChecked = selected.tileSide == "B";
					inChapterCB.SelectedIndex = chapter.tileObserver.Count - 1;
					scenario.globalTilePool.Remove( t.Item1 );
				}
			}
		}

		private async void addExploredTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true && selected != null )
			{
				selected.triggerName = tw.triggerName;
			}
		}

		private void toggleUseGraphics_Click( object sender, RoutedEventArgs e )
		{
			for ( int i = 0; i < chapter.tileObserver.Count; i++ )
			{
				HexTile hex = (HexTile)chapter.tileObserver[i];
				hex.useGraphic = scenario.useTileGraphics;
				Visual( hex ).ToggleGraphic( canvas );
			}
		}
	}
}
