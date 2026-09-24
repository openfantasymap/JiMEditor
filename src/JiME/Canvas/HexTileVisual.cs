using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace JiME
{
	/// <summary>
	/// Draws a HexTile on the tile editor canvas: a hex outline Path plus the (optional) tile artwork.
	/// This is the drawing half of the WPF editor's HexTile class; the data lives in JiME.Core.
	/// </summary>
	public class HexTileVisual
	{
		static readonly IBrush defaultFill = new SolidColorBrush( Color.FromRgb( 70, 70, 74 ) );

		public HexTile Tile { get; }
		public Path hexPathShape { get; private set; }
		public Image tileImage { get; private set; }

		//top-left of the outline geometry, so the Path never has negative coordinates
		Point geometryOffset;
		Point clickV;

		public HexTileVisual( HexTile tile )
		{
			Tile = tile;
			BuildShape();
			BuildImage();
			Update();
		}

		void BuildShape()
		{
			//dims 64 x 55.4256256 of ONE hexagon, hexagon ratio 1 : 1.1547005
			Tile.UpdateHexRoot();
			Vector[][] polygons = Tile.GetHexCenters().Select( c => HexTile.RegularPolygon( c, 32, 6, 0 ) ).ToArray();
			double minX = polygons.SelectMany( p => p ).Min( p => p.X );
			double minY = polygons.SelectMany( p => p ).Min( p => p.Y );
			geometryOffset = new Point( minX, minY );

			var geometry = new PathGeometry();
			foreach ( Vector[] polygon in polygons )
			{
				var figure = new PathFigure { StartPoint = ToLocal( polygon[0] ), IsClosed = false, IsFilled = true };
				for ( int i = 1; i < polygon.Length; i++ )
					figure.Segments.Add( new LineSegment { Point = ToLocal( polygon[i] ) } );
				geometry.Figures.Add( figure );
			}

			hexPathShape = new Path
			{
				Stroke = Brushes.White,
				StrokeThickness = 2,
				Fill = defaultFill,
				Data = geometry,
				DataContext = Tile,
				RenderTransformOrigin = RelativePoint.TopLeft
			};
		}

		Point ToLocal( Vector v ) => new Point( v.X - geometryOffset.X, v.Y - geometryOffset.Y );

		void BuildImage()
		{
			var source = TileGraphics.TileImage( Tile.tileSide, Tile.idNumber );
			tileImage = new Image
			{
				Source = source,
				Width = Math.Ceiling( source.Size.Width ),
				Height = Math.Ceiling( source.Size.Height ),
				IsHitTestVisible = false,
				RenderTransformOrigin = RelativePoint.TopLeft
			};
		}

		/// <summary>
		/// updates shape and image position on canvas
		/// </summary>
		void Update()
		{
			hexPathShape.RenderTransform = new TranslateTransform( Tile.position.X + geometryOffset.X, Tile.position.Y + geometryOffset.Y );

			TileImageTransform t = Tile.GetImageTransform( tileImage.Width, tileImage.Height );
			var group = new TransformGroup();
			group.Children.Add( new ScaleTransform( t.Scale, t.Scale ) );
			group.Children.Add( new RotateTransform( t.Angle, t.RotateCenterX, t.RotateCenterY ) );
			group.Children.Add( new TranslateTransform( t.TranslateX, t.TranslateY ) );
			tileImage.RenderTransform = group;
		}

		/// <summary>
		/// is the canvas point inside the hex outline?
		/// </summary>
		public bool HitTest( Point canvasPoint )
		{
			var local = new Point( canvasPoint.X - Tile.position.X - geometryOffset.X, canvasPoint.Y - Tile.position.Y - geometryOffset.Y );
			return hexPathShape.Data?.FillContains( local ) == true;
		}

		public void ChangeColor( int idx )
		{
			Tile.color = idx;
			if ( !Tile.useGraphic )
				hexPathShape.Fill = TileGraphics.hexColors[Math.Max( idx, 0 )];
			else
				hexPathShape.Fill = Brushes.White;
		}

		public void Select()
		{
			hexPathShape.Stroke = Brushes.Red;
			hexPathShape.ZIndex = 100;
			tileImage.ZIndex = 101;
		}

		public void Unselect()
		{
			hexPathShape.Stroke = Brushes.White;
			hexPathShape.ZIndex = 0;
			tileImage.ZIndex = 1;
		}

		public void RemoveFrom( Canvas canvas )
		{
			canvas.Children.Remove( hexPathShape );
			canvas.Children.Remove( tileImage );
		}

		public void Rehydrate( Canvas canvas )
		{
			RemoveFrom( canvas );
			BuildShape();
			BuildImage();
			canvas.Children.Add( hexPathShape );
			if ( Tile.useGraphic )
				canvas.Children.Add( tileImage );
			Update();
		}

		public void ToggleGraphic( Canvas canvas )
		{
			if ( Tile.useGraphic )
			{
				if ( !canvas.Children.Contains( tileImage ) )
					canvas.Children.Add( tileImage );
			}
			else
			{
				canvas.Children.Remove( tileImage );
			}
			ChangeColor( Tile.color );
		}

		public void ChangeTileSide( string side, Canvas canvas )
		{
			Tile.tileSide = side;
			Tile.position = new Vector( Utils.dragSnapX[5], Utils.dragSnapY[5] );
			Tile.angle = 0;
			Rehydrate( canvas );
			ChangeColor( Tile.color );
			Select();
		}

		public void Rotate( double amount, Canvas canvas )
		{
			Tile.angle += amount;
			Tile.angle %= 360;
			Rehydrate( canvas );
			ChangeColor( Tile.color );
			Select();
		}

		/// <summary>
		/// remember where inside the tile the drag started
		/// </summary>
		public void SetClickV( Point canvasPoint )
		{
			clickV = new Point( canvasPoint.X - Tile.position.X, canvasPoint.Y - Tile.position.Y );
		}

		public void Drag( Point canvasPoint )
		{
			if ( Tile.DragTo( new Vector( canvasPoint.X - clickV.X, canvasPoint.Y - clickV.Y ) ) )
				Update();
		}
	}
}
