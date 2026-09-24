using System;
using System.Globalization;
using Newtonsoft.Json;

namespace JiME
{
	/// <summary>
	/// 2D vector/point in canvas units. Replaces System.Windows.Vector (WPF only) while keeping
	/// the .jime file format: WPF's type converter wrote vectors as the string "x,y" (invariant
	/// culture), and the Your Journey companion app parses exactly that format.
	/// </summary>
	[JsonConverter( typeof( VectorJsonConverter ) )]
	public struct Vector : IEquatable<Vector>
	{
		public double X { get; set; }
		public double Y { get; set; }

		public Vector( double x, double y )
		{
			X = x;
			Y = y;
		}

		public static Vector operator +( Vector a, Vector b ) => new Vector( a.X + b.X, a.Y + b.Y );
		public static Vector operator -( Vector a, Vector b ) => new Vector( a.X - b.X, a.Y - b.Y );
		public static Vector operator *( Vector a, double s ) => new Vector( a.X * s, a.Y * s );
		public static Vector operator /( Vector a, double s ) => new Vector( a.X / s, a.Y / s );
		public static bool operator ==( Vector a, Vector b ) => a.Equals( b );
		public static bool operator !=( Vector a, Vector b ) => !a.Equals( b );

		public bool Equals( Vector other ) => X == other.X && Y == other.Y;
		public override bool Equals( object obj ) => obj is Vector v && Equals( v );
		public override int GetHashCode() => HashCode.Combine( X, Y );

		/// <summary>
		/// "x,y" with invariant culture, same as WPF's Vector.ToString(CultureInfo.InvariantCulture)
		/// </summary>
		public override string ToString()
		{
			return X.ToString( CultureInfo.InvariantCulture ) + "," + Y.ToString( CultureInfo.InvariantCulture );
		}

		/// <summary>
		/// parses "x,y" (also accepts "x;y" and "x y")
		/// </summary>
		public static Vector Parse( string s )
		{
			string[] parts = s.Split( new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries );
			if ( parts.Length != 2 )
				throw new FormatException( $"Invalid vector: '{s}'" );
			return new Vector(
				double.Parse( parts[0], NumberStyles.Float, CultureInfo.InvariantCulture ),
				double.Parse( parts[1], NumberStyles.Float, CultureInfo.InvariantCulture ) );
		}
	}

	/// <summary>
	/// Writes "x,y"; reads "x,y" strings and {"X":..,"Y":..} objects
	/// </summary>
	public class VectorJsonConverter : JsonConverter<Vector>
	{
		public override void WriteJson( JsonWriter writer, Vector value, JsonSerializer serializer )
		{
			writer.WriteValue( value.ToString() );
		}

		public override Vector ReadJson( JsonReader reader, Type objectType, Vector existingValue, bool hasExistingValue, JsonSerializer serializer )
		{
			switch ( reader.TokenType )
			{
				case JsonToken.String:
					return Vector.Parse( (string)reader.Value );
				case JsonToken.StartObject:
					double x = 0, y = 0;
					while ( reader.Read() && reader.TokenType == JsonToken.PropertyName )
					{
						string name = ( (string)reader.Value ).ToUpperInvariant();
						reader.Read();
						double v = Convert.ToDouble( reader.Value, CultureInfo.InvariantCulture );
						if ( name == "X" )
							x = v;
						else if ( name == "Y" )
							y = v;
					}
					return new Vector( x, y );
				case JsonToken.Null:
					return default;
				default:
					throw new JsonSerializationException( $"Unexpected token {reader.TokenType} for Vector" );
			}
		}
	}
}
