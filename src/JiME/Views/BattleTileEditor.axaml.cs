using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for BattleTileEditor.xaml
	/// </summary>
	public partial class BattleTileEditor : Window, INotifyPropertyChanged
	{
		BattleTile _selectedLeft;

		int selectedWallIndex;
		int wallValue;
		Path selectedWall;
		bool closing = false;

		public Scenario scenario { get; set; }
		public Chapter chapter { get; set; }

		public BattleTile selectedLeft
		{
			get => _selectedLeft;
			set
			{
				_selectedLeft = value;
				PropChanged( "selectedLeft" );
			}
		}

		public BattleTileEditor( Scenario s, Chapter c )
		{
			InitializeComponent();
			DataContext = this;

			scenario = s;
			chapter = c;

			foreach ( Control el in canvas.Children )
			{
				if ( el.DataContext.ToString().Contains( "wall" ) )
					el.ZIndex = 1000;
				if ( el.DataContext.ToString() == "wall0" )
				{
					( (Path)el ).Stroke = new SolidColorBrush( Color.FromArgb( 255, 255, 0, 0 ) );
					selectedWall = (Path)el;
				}
			}
			selectedLeft = chapter.tileObserver[0] as BattleTile;
			( (Path)canvas.Children[0] ).Stroke = Brushes.Red;
			canvas.Children[0].ZIndex = 100;

			selectedWallIndex = 0;
			wallValue = scenario.wallTypes[0];
			SetRadioButtons();
			FillWallColors();
			FillRegionColors();
		}

		//hides AvaloniaObject.PropertyChanged; the bindings use this INotifyPropertyChanged event
		public new event PropertyChangedEventHandler PropertyChanged;

		private async void OkButton_Click( object sender, RoutedEventArgs e )
		{
			if ( !TryClose() )
			{
				await ShowTriggerError();
				return;
			}
			closing = true;
			Close( true );
		}

		bool TryClose()
		{
			if ( selectedLeft.terrainToken > 0 )
			{
				if ( selectedLeft.tokenTrigger == "None" )
				{
					return false;
				}
			}
			return true;
		}

		System.Threading.Tasks.Task ShowTriggerError()
		{
			return MessageBox.Show( this, "'Trigger On Token Interaction' cannot be set to None.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error );
		}

		private void Canvas_MouseDown( object sender, PointerPressedEventArgs e )
		{
			//only the region/wall pieces carry a string DataContext
			if ( !( e.Source is Path source ) || !( source.DataContext is string dc ) )
				return;

			if ( dc.Contains( "wall" ) )//wall tile
			{
				selectedWallIndex = int.Parse( dc.Substring( 4 ) );
				wallValue = scenario.wallTypes[selectedWallIndex];
				selectedWall = source;
				SetRadioButtons();

				//unselect
				foreach ( Control el in canvas.Children )
				{
					if ( el.DataContext.ToString().Contains( "wall" ) )
					{
						( (Path)el ).Stroke = new SolidColorBrush( Color.FromArgb( (byte)( .15f * 255f ), 255, 255, 255 ) );
					}
				}
				//select
				source.Stroke = new SolidColorBrush( Color.FromArgb( 255, 255, 0, 0 ) );
			}
			else//region tile
			{
				int idx = int.Parse( dc );

				selectedLeft = null;
				selectedLeft = chapter.tileObserver[idx] as BattleTile;
				foreach ( Control el in canvas.Children )
				{
					if ( !el.DataContext.ToString().Contains( "wall" ) )
					{
						//unselect
						( (Path)el ).Stroke = Brushes.White;
						//( (Path)el ).Fill = (SolidColorBrush)FindResource( "bgColorLight" );
						el.ZIndex = 0;
					}
				}
				source.ZIndex = 100;
				( (Path)canvas.Children[idx] ).Stroke = Brushes.Red;
				FillRegionColors();
			}
		}

		private async void AddTriggerButton_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				selectedLeft.triggerName = tw.triggerName;
			}
		}

		private async void AddTokenTrigger_Click( object sender, RoutedEventArgs e )
		{
			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				selectedLeft.tokenTrigger = tw.triggerName;
			}
		}

		private void RadioButton_Click( object sender, RoutedEventArgs e )
		{
			wallValue = int.Parse( ( (RadioButton)sender ).DataContext.ToString() );
			scenario.wallTypes[selectedWallIndex] = wallValue;
			FillWallColors();
		}

		IBrush Res( string key )
		{
			return this.FindResource( key ) as IBrush;
		}

		void FillWallColors()
		{
			switch ( wallValue )
			{
				case 0:
					selectedWall.Fill = Res( "wallNone" );
					break;
				case 1:
					selectedWall.Fill = Res( "wallBrown" );
					break;
				case 2:
					selectedWall.Fill = Res( "wallRiver" );
					break;
			}
		}

		void FillRegionColors()
		{
			for ( int i = 0; i < chapter.tileObserver.Count; i++ )
			{
				switch ( ( (BattleTile)chapter.tileObserver[i] ).terrainToken )
				{
					case 0:
						( (Path)canvas.Children[i] ).Fill = Res( "bgColorLight" );
						break;
					case 1:
						( (Path)canvas.Children[i] ).Fill = Res( "pit" );
						break;
					case 2:
						( (Path)canvas.Children[i] ).Fill = Res( "mist" );
						break;
					case 3:
						( (Path)canvas.Children[i] ).Fill = Res( "barrels" );
						break;
					case 4:
						( (Path)canvas.Children[i] ).Fill = Res( "table" );
						break;
					case 5:
						( (Path)canvas.Children[i] ).Fill = Res( "firepit" );
						break;
					case 6:
						( (Path)canvas.Children[i] ).Fill = Res( "statue" );
						break;
				}
			}
		}

		void SetRadioButtons()
		{
			wNone.IsChecked = wallValue == 0;
			wWall.IsChecked = wallValue == 1;
			wRiver.IsChecked = wallValue == 2;
		}

		private void TokenCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			//fires during InitializeComponent, before the canvas pieces are set up
			if ( chapter == null )
				return;
			//posted so the SelectedIndex binding has already updated terrainToken
			Dispatcher.UIThread.Post( FillRegionColors );
		}

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private async void Window_Closing( object sender, WindowClosingEventArgs e )
		{
			if ( closing )
				return;
			e.Cancel = !TryClose();
			if ( e.Cancel )
				await ShowTriggerError();
		}
	}
}
