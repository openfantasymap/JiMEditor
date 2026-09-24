using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Text.RegularExpressions;

namespace JiME.Views
{
	/// <summary>
	/// Interaction logic for TokenEditorWindow.xaml
	/// </summary>
	public partial class TokenEditorWindow : Window, INotifyPropertyChanged
	{
		Token _selected;
		public Token selected
		{
			get { return _selected; }
			set
			{
				_selected = value;
				PropChanged( "selected" );
			}
		}
		public Scenario scenario { get; set; }
		public HexTile hexTile { get; set; }
		public new event PropertyChangedEventHandler PropertyChanged;
		public ObservableCollection<IInteraction> tokenInteractions { get; set; }

		//token drawing
		bool dragging;
		readonly Dictionary<Token, TokenVisual> visuals = new Dictionary<Token, TokenVisual>();

		public TokenEditorWindow( HexTile hex, Scenario s, bool fromRandom = false )
		{
			InitializeComponent();

			scenario = s;
			hexTile = hex;
			selected = null;

			tokenInteractions = new ObservableCollection<IInteraction>( scenario.interactionObserver.Where( x => ( x.isTokenInteraction && !Regex.IsMatch( x.dataName, @"\sGRP\d+$" ) ) || x.dataName == "None" ) );
			//after the properties are set: Avalonia evaluates bindings as soon as the DataContext is assigned
			DataContext = this;

			if ( hexTile.tokenList.Count > 0 )
				tokenCombo.SelectedIndex = 0;

			//rehydrate existing tokens in this tile
			for ( int i = 0; i < hexTile.tokenList.Count; i++ )
				Visual( hexTile.tokenList[i] ).Rehydrate( canvas );

			try
			{
				tileImage.Source = TileGraphics.TileImage( hexTile.tileSide, hexTile.idNumber );
			}
			catch ( Exception e ) { Debug.Log( e.Message ); }
			UpdateButtonsEnabled();

			if ( string.IsNullOrEmpty( hexTile.flavorBookData.pages[0] ) )
				exploreStatus.Text = "Exploration Text is Empty";
			else
				exploreStatus.Text = "Exploration Text is Set";

			if ( fromRandom )
				explorationBox.IsVisible = false;

			AddHandler( KeyDownEvent, Window_PreviewKeyDown, RoutingStrategies.Tunnel );
		}

		TokenVisual Visual( Token token )
		{
			if ( !visuals.TryGetValue( token, out TokenVisual v ) )
				visuals[token] = v = new TokenVisual( token );
			return v;
		}

		void UnselectAll()
		{
			foreach ( Token tt in hexTile.tokenList )
				Visual( tt ).Unselect();
		}

		private void OkButton_Click( object sender, RoutedEventArgs e )
		{
			Close( true );
		}

		//private void addSearch_Click( object sender, RoutedEventArgs e )
		//{
		//	foreach ( Token tt in hexTile.tokenList )
		//		tt.Unselect();
		//	Token t = new Token( TokenType.Search );
		//	hexTile.tokenList.Add( t );
		//	selected = t;
		//	canvas.Children.Add( t.tokenPathShape );
		//	t.Select();
		//	UpdateButtonsEnabled();
		//}

		//private void addPerson_Click( object sender, RoutedEventArgs e )
		//{
		//	foreach ( Token tt in hexTile.tokenList )
		//		tt.Unselect();
		//	Token t = new Token( TokenType.Person );
		//	hexTile.tokenList.Add( t );
		//	selected = t;
		//	canvas.Children.Add( t.tokenPathShape );
		//	t.Select();
		//	UpdateButtonsEnabled();
		//}

		//private void addThreat_Click( object sender, RoutedEventArgs e )
		//{
		//	foreach ( Token tt in hexTile.tokenList )
		//		tt.Unselect();
		//	Token t = new Token( TokenType.Threat );
		//	hexTile.tokenList.Add( t );
		//	selected = t;
		//	canvas.Children.Add( t.tokenPathShape );
		//	t.Select();
		//	UpdateButtonsEnabled();
		//}

		//private void addDarkness_Click( object sender, RoutedEventArgs e )
		//{
		//	foreach ( Token tt in hexTile.tokenList )
		//		tt.Unselect();
		//	Token t = new Token( TokenType.Darkness );
		//	hexTile.tokenList.Add( t );
		//	selected = t;
		//	canvas.Children.Add( t.tokenPathShape );
		//	t.Select();
		//	UpdateButtonsEnabled();
		//}

		void UpdateButtonsEnabled()
		{
			addToken.IsEnabled = hexTile.tokenList.Count < hexTile.tokenCount;
		}

		private void Canvas_PointerPressed( object sender, PointerPressedEventArgs e )
		{
			UnselectAll();
			selected = null;

			//topmost token under the pointer
			Point p = e.GetPosition( canvas );
			Token hit = hexTile.tokenList.LastOrDefault( t =>
				Math.Pow( t.position.X - p.X, 2 ) + Math.Pow( t.position.Y - p.Y, 2 ) <= Token.Radius * Token.Radius );
			if ( hit != null )
			{
				selected = hit;
				Visual( selected ).Select();
				dragging = true;
				tokenCombo.SelectedItem = selected;
			}
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

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}

		private void token_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			var s = ( (ComboBox)sender ).SelectedItem as Token;
			if ( s != null )
			{
				UnselectAll();
				Visual( s ).Select();
				selected = s;
			}
		}

		private void removeToken_Click( object sender, RoutedEventArgs e )
		{
			if ( selected == null )
				return;

			RemoveSelectedToken();
		}

		void RemoveSelectedToken()
		{
			var s = tokenCombo.SelectedItem as Token;
			if ( s != null )
			{
				hexTile.tokenList.Remove( s );
				canvas.Children.Remove( Visual( s ).tokenPathShape );
				visuals.Remove( s );
			}
			UpdateButtonsEnabled();
			selected = null;
		}

		private async void addFlavor_Click( object sender, RoutedEventArgs e )
		{
			TextEditorWindow tw = new TextEditorWindow( scenario, EditMode.Flavor, hexTile.flavorBookData );
			if ( await tw.ShowDialog<bool?>( this ) == true )
				hexTile.flavorBookData.pages = tw.textBookController.pages;

			if ( string.IsNullOrEmpty( hexTile.flavorBookData.pages[0] ) )
				exploreStatus.Text = "Exploration Text is Empty";
			else
				exploreStatus.Text = "Exploration Text is Set";
		}

		private async void addTrigger_Click( object sender, RoutedEventArgs e )
		{
			if ( selected == null )
				return;

			TriggerEditorWindow tw = new TriggerEditorWindow( scenario );
			if ( await tw.ShowDialog<bool?>( this ) == true )
			{
				selected.triggeredByName = tw.triggerName;
			}
		}

		private void addToken_Click( object sender, RoutedEventArgs e )
		{
			UnselectAll();
			Token t = new Token( TokenType.None );
			hexTile.tokenList.Add( t );
			selected = t;
			canvas.Children.Add( Visual( t ).tokenPathShape );
			Visual( t ).Select();
			UpdateButtonsEnabled();
		}

		private void interactionCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( selected != null && ( (ComboBox)sender ).SelectedItem != null )
			{
				selected.tokenType = ( (IInteraction)( (ComboBox)sender )?.SelectedItem ).tokenType;
				selected.personType = ( (IInteraction)( (ComboBox)sender )?.SelectedItem ).personType;
				selected.dataName = selected.tokenType.ToString();
				Visual( selected ).ReColor();
			}

			//	Debug.Log( selected.tokenType );
			//var foo = ( (ComboBox)sender )?.SelectedItem;
			//if ( foo != null )
			//	Debug.Log( ( (ComboBox)sender ).SelectedItem.ToString() );
		}

		private void Window_PreviewKeyDown( object sender, KeyEventArgs e )
		{
			//Backspace too: Mac keyboards have no Delete key
			if ( selected != null && ( e.Key == Key.Delete || e.Key == Key.Back ) && !( e.Source is TextBox ) )
				RemoveSelectedToken();
		}
	}
}
