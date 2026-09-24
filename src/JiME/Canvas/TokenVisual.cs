using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace JiME
{
	/// <summary>
	/// Draws a Token on the token editor's 512x512 tile canvas (the drawing half of the WPF editor's Token class)
	/// </summary>
	public class TokenVisual
	{
		//indexed by TokenType: Search, Person, Threat, Darkness, Exploration, None
		static readonly IBrush[] fillColors = { Brushes.ForestGreen, Brushes.BlueViolet, Brushes.DarkRed, Brushes.Black, Brushes.Gray, Brushes.Gray };

		public Token Token { get; }
		public Ellipse tokenPathShape { get; private set; }

		public TokenVisual( Token token )
		{
			Token = token;
			tokenPathShape = new Ellipse
			{
				StrokeThickness = 4,
				Stroke = Brushes.White,
				Width = Token.Radius * 2,
				Height = Token.Radius * 2,
				DataContext = token,
				RenderTransformOrigin = RelativePoint.TopLeft
			};
			ReColor();
			Update();
		}

		public void ReColor()
		{
			tokenPathShape.Fill = fillColors[(int)Token.tokenType];
		}

		void Update()
		{
			tokenPathShape.RenderTransform = new TranslateTransform( Token.position.X - Token.Radius, Token.position.Y - Token.Radius );
		}

		public void Rehydrate( Canvas canvas )
		{
			canvas.Children.Remove( tokenPathShape );
			canvas.Children.Add( tokenPathShape );
			ReColor();
			Update();
		}

		public void Select()
		{
			tokenPathShape.Stroke = Brushes.Red;
			tokenPathShape.ZIndex = 100;
		}

		public void Unselect()
		{
			tokenPathShape.Stroke = Brushes.White;
			tokenPathShape.ZIndex = 0;
		}

		/// <summary>
		/// the token's centre follows the pointer, staying inside the tile canvas
		/// </summary>
		public void Drag( Point canvasPoint )
		{
			if ( Token.MoveTo( new Vector( canvasPoint.X, canvasPoint.Y ) ) )
				Update();
		}
	}
}
