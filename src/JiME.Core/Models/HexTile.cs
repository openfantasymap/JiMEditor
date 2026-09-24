using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace JiME
{
	/// <summary>
	/// A hex map tile placed in a Chapter. Holds the saved data plus the pure geometry the
	/// tile editor needs to draw it (see HexTileVisual in the editor for the drawing).
	/// </summary>
	public class HexTile : INotifyPropertyChanged, ITile
	{
		public const double HexWidth = 64;
		public const double HexHeight = 55.4256256d;
		public const double HalfHexHeight = 27.7128128d;

		string _tileSide, _triggerName;
		bool _isStartTile;

		public double angle { get; set; }
		public int idNumber { get; set; }
		public int tokenCount { get; set; }
		public Guid GUID { get; set; }
		public string tileSide
		{
			get => _tileSide;
			set
			{
				_tileSide = value;
				PropChanged( "tileSide" );
			}
		}
		public Vector position { get; set; }
		public TileType tileType { get; set; }
		public TextBookData flavorBookData { get; set; }
		public ObservableCollection<Token> tokenList { get; set; }
		public bool isStartTile
		{
			get => _isStartTile;
			set
			{
				_isStartTile = value;
				PropChanged( "isStartTile" );
			}
		}
		public int color;
		/// <summary>
		/// position of the FIRST hexagon that makes up the tile, needed by the companion app to calculate offsets
		/// </summary>
		public Vector hexRoot;
		public string triggerName
		{
			get { return _triggerName; }
			set
			{
				_triggerName = value;
				PropChanged( "triggerName" );
			}
		}

		[JsonIgnore]
		public bool useGraphic;

		public event PropertyChangedEventHandler PropertyChanged;

		public HexTile() { }

		public HexTile( int n, bool skipBuild = false )
		{
			tileType = TileType.Hex;
			idNumber = n;
			tokenCount = ( n / 100 ) % 10;
			GUID = Guid.NewGuid();
			tileSide = "A";
			position = new Vector( Utils.dragSnapX[5], Utils.dragSnapY[5] );
			tokenList = new ObservableCollection<Token>();
			flavorBookData = new TextBookData( "" );
			flavorBookData.pages.Add( "" );
			isStartTile = false;
			triggerName = "None";

			if ( !skipBuild )
				UpdateHexRoot();
		}

		[JsonIgnore]
		public HexTileData HexData => tileSide == "A" ? Utils.hexDictionary[idNumber] : Utils.hexDictionaryB[idNumber];

		public void UpdateHexRoot()
		{
			Utils.LoadHexData();
			string[] s = HexData.coords.Split( ' ' )[0].Split( ',' );
			hexRoot = new Vector( double.Parse( s[0], CultureInfo.InvariantCulture ), double.Parse( s[1], CultureInfo.InvariantCulture ) );
		}

		/// <summary>
		/// canvas-local centres of every hexagon of this tile, rotated by angle and snapped to the hex grid
		/// </summary>
		public Vector[] GetHexCenters()
		{
			Utils.LoadHexData();
			HexTileData hexdata = HexData;
			//local coords, example: (0,1)
			Vector[] hexPositions = HexTileData.ParseCoords( hexdata.coords );
			Vector center = hexPositions[0];
			for ( int i = 0; i < hexdata.tileCount; i++ )
				hexPositions[i] = RotatePoint( hexPositions[i], center, angle );
			return hexPositions.Take( hexdata.tileCount ).ToArray();
		}

		/// <summary>
		/// closed polygon: the first point is repeated at the end
		/// </summary>
		public static Vector[] RegularPolygon( Vector c, double r, int numSides, double offsetDegree )
		{
			double a = Math.PI * offsetDegree / 180.0d;
			double step = 2d * Math.PI / Math.Max( numSides, 3 );
			Vector[] points = new Vector[numSides + 1];
			for ( int i = 0; i < numSides; i++, a += step )
				points[i] = new Vector( c.X + r * Math.Cos( a ), c.Y + r * Math.Sin( a ) );
			points[numSides] = points[0];
			return points;
		}

		/// <summary>
		/// Transform that places the tile artwork (scaled, rotated about a centre, then translated) under the hex outline
		/// </summary>
		public TileImageTransform GetImageTransform( double imageWidth, double imageHeight )
		{
			Utils.LoadHexData();
			//size of the hex outline. NOTE: the B side uses the A side's height, as the original editor did
			double dimsX = tileSide == "A" ? Utils.hexDictionary[idNumber].width : Utils.hexDictionaryB[idNumber].width;
			double dimsY = Utils.hexDictionary[idNumber].height;

			//scale of largest side (width or height)
			double scale = imageWidth > imageHeight ? dimsX / 512 : dimsY / 512;

			double centerX = 32f;
			double centerY = HexHeight;
			if ( hexRoot.X != 0 )
				centerX = HexWidth * hexRoot.X;
			if ( hexRoot.Y != 1 )
				centerY = HexHeight * ( hexRoot.Y - 2 );

			double yoffset = tileSide == "A"
				? HandleSpecialCaseA( ref centerX, ref centerY )
				: HandleSpecialCaseB( ref centerX, ref centerY );

			return new TileImageTransform
			{
				Scale = scale,
				Angle = angle,
				RotateCenterX = centerX,
				RotateCenterY = centerY,
				TranslateX = position.X - 32f,
				TranslateY = position.Y - ( HalfHexHeight + yoffset )
			};
		}

		double HandleSpecialCaseA( ref double centerX, ref double centerY )
		{
			double yoffset = 0;
			double h = HalfHexHeight;

			switch ( idNumber )
			{
				case 201:
				case 207:
				case 305:
				case 307:
				case 400:
					yoffset = -h;
					centerY = h;
					break;
				case 205:
					yoffset = -h;
					centerX = HexWidth * hexRoot.X - 16;
					centerY = h;
					break;
				case 206:
					centerY = h * 5;
					break;
				case 209:
					centerY = h * 3;
					break;
				case 304:
					centerY = h * 4;
					break;
			}
			return yoffset;
		}

		double HandleSpecialCaseB( ref double centerX, ref double centerY )
		{
			double yoffset = 0;
			double h = HalfHexHeight;

			switch ( idNumber )
			{
				case 200:
				case 302:
					centerY = h * 5;
					break;
				case 201:
					yoffset = -h;
					centerY = h * 3;
					break;
				case 204:
				case 205:
					yoffset = -h;
					centerY = h;
					break;
				case 206:
				case 207:
				case 209:
					centerY = h * 2;
					break;
				case 303:
					centerY = h * 4;
					break;
				case 304:
				case 308:
					centerY = h;
					break;
				case 305:
					yoffset = -h;
					centerY = h * 4;
					break;
				case 306:
					centerY = h * 3;
					break;
				case 307:
					yoffset = -h;
					centerY = h * 2;
					break;
				case 400:
					yoffset = -h;
					centerY = h * 5;
					break;
			}
			return yoffset;
		}

		/// <summary>
		/// snaps a dragged canvas point to the tile placement grid; returns false if it's outside the grid
		/// </summary>
		public bool DragTo( Vector clickPoint )
		{
			Vector snapped = new Vector(
				( from snapx in Utils.dragSnapX where clickPoint.X.WithinTolerance( snapx, Utils.tolerance ) select snapx ).FirstOr( -1 ),
				( from snapy in Utils.dragSnapY where clickPoint.Y.WithinTolerance( snapy, Utils.tolerance ) select snapy ).FirstOr( -1 ) );

			position = snapped;
			return snapped != new Vector( -1, -1 );
		}

		static Vector RotatePoint( Vector pointToRotate, Vector centerPoint, double angleInDegrees )
		{
			double angleInRadians = angleInDegrees * ( Math.PI / 180 );
			double cosTheta = Math.Cos( angleInRadians );
			double sinTheta = Math.Sin( angleInRadians );
			double x = (int)( cosTheta * ( pointToRotate.X - centerPoint.X ) - sinTheta * ( pointToRotate.Y - centerPoint.Y ) + centerPoint.X );
			double y = (int)( sinTheta * ( pointToRotate.X - centerPoint.X ) + cosTheta * ( pointToRotate.Y - centerPoint.Y ) + centerPoint.Y );

			return new Vector(
				( from snapx in Utils.hexSnapX where x.WithinTolerance( snapx, 5 ) select snapx ).FirstOr( -1 ),
				( from snapy in Utils.hexSnapY where y.WithinTolerance( snapy, 5 ) select snapy ).FirstOr( -1 ) );
		}

		public void RenameTrigger( string oldName, string newName )
		{
			foreach ( Token t in tokenList )
			{
				if ( t.triggerName == oldName )
					t.triggerName = newName;
			}

			if ( triggerName == oldName )
				triggerName = newName;
		}

		void PropChanged( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}
	}

	/// <summary>
	/// Render transform for a tile image: scale, then rotate by Angle around (RotateCenterX, RotateCenterY), then translate
	/// </summary>
	public struct TileImageTransform
	{
		public double Scale, Angle, RotateCenterX, RotateCenterY, TranslateX, TranslateY;
	}
}
