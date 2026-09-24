using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace JiME
{
	/// <summary>
	/// Tile artwork and colours used by the editor UI (the WPF editor kept these in Utils)
	/// </summary>
	public static class TileGraphics
	{
		public static SolidColorBrush[] hexColors;
		/// <summary>
		/// tile artwork, indexed like Utils.LoadTiles()
		/// </summary>
		public static Bitmap[] tileSourceA, tileSourceB;

		static readonly Dictionary<string, Bitmap> assetCache = new Dictionary<string, Bitmap>();

		public static void Init()
		{
			if ( hexColors != null )
				return;

			hexColors = new SolidColorBrush[5];
			hexColors[0] = new SolidColorBrush( Colors.SaddleBrown );
			hexColors[1] = new SolidColorBrush( Colors.DodgerBlue );
			hexColors[2] = new SolidColorBrush( Colors.SeaGreen );
			hexColors[3] = new SolidColorBrush( Colors.DarkMagenta );
			hexColors[4] = new SolidColorBrush( Colors.DarkOrange );

			int[] ids = Utils.LoadTiles().ToArray();
			tileSourceA = new Bitmap[ids.Length];
			tileSourceB = new Bitmap[ids.Length];
			for ( int i = 0; i < ids.Length; i++ )
			{
				tileSourceA[i] = Asset( $"TilesA/{ids[i]}.png" );
				tileSourceB[i] = Asset( $"TilesB/{ids[i]}.png" );
			}
		}

		/// <summary>
		/// artwork for a tile id and side ("A"/"B")
		/// </summary>
		public static Bitmap TileImage( string side, int id )
		{
			return Asset( $"Tiles{side}/{id}.png" );
		}

		/// <summary>
		/// cached bitmap from the Assets folder, e.g. "ring.png" or "TilesA/100.png"
		/// </summary>
		public static Bitmap Asset( string relativePath )
		{
			lock ( assetCache )
			{
				if ( !assetCache.TryGetValue( relativePath, out Bitmap bitmap ) )
				{
					using ( var stream = AssetLoader.Open( new Uri( $"avares://JiME/Assets/{relativePath}" ) ) )
						bitmap = new Bitmap( stream );
					assetCache[relativePath] = bitmap;
				}
				return bitmap;
			}
		}
	}

	public class GalleryTile : INotifyPropertyChanged
	{
		bool _selected, _enabled;
		string _side;
		IImage _source;
		public int id { get; set; }
		public IImage source
		{
			get { return _source; }
			set
			{
				_source = value;
				PropChanged( "source" );
			}
		}
		public bool selected
		{
			get { return _selected; }
			set
			{
				_selected = value;
				PropChanged( "selected" );
			}
		}
		public bool enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				PropChanged( "enabled" );
			}
		}
		public SolidColorBrush color;
		public string side
		{
			get { return _side; }
			set
			{
				_side = value;
				PropChanged( "side" );
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public GalleryTile()
		{
			enabled = true;
			selected = false;
			side = "A";
		}

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}
	}
}
