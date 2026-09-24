using System;
using System.ComponentModel;

namespace JiME
{
	public class Token : INotifyPropertyChanged, ICommonData
	{
		string _dataName;
		string _triggerName, _triggeredByName;
		TokenType _tokenType;
		PersonType _personType;

		public string dataName
		{
			get { return _dataName; }
			set
			{
				if ( value != _dataName )
				{
					_dataName = value;
					Prop( "dataName" );
				}
			}
		}
		public Guid GUID { get; set; }
		public bool isEmpty { get; set; }
		/// <summary>
		/// The name of the Event to trigger
		/// </summary>
		public string triggerName
		{
			get => _triggerName;
			set
			{
				if ( value != _triggerName )
				{
					_triggerName = value;
					Prop( "triggerName" );
				}
			}
		}
		public TokenType tokenType
		{
			get => _tokenType;
			set
			{
				_tokenType = value;
				Prop( "tokenType" );
			}
		}
		public PersonType personType
		{
			get => _personType;
			set
			{
				_personType = value;
				Prop( "personType" );
			}
		}
		public int idNumber { get; set; }
		public Vector position { get; set; }
		public string triggeredByName
		{
			get => _triggeredByName;
			set
			{
				if ( value != _triggeredByName )
				{
					_triggeredByName = value;
					Prop( "triggeredByName" );
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public Token( TokenType ttype )
		{
			dataName = ttype.ToString();
			GUID = Guid.NewGuid();
			triggerName = "None";
			triggeredByName = "None";
			tokenType = ttype;
			personType = PersonType.Human;
			position = new Vector( 256, 256 );
		}

		/// <summary>
		/// token radius on the 512x512 tile canvas
		/// </summary>
		public const double Radius = 25;

		/// <summary>
		/// moves the token, keeping it inside the 512x512 tile canvas; returns false if the move was rejected
		/// </summary>
		public bool MoveTo( Vector newPosition )
		{
			if ( newPosition.X - Radius < 0 || newPosition.Y - Radius < 0 ||
				newPosition.X + Radius > 512 || newPosition.Y + Radius > 512 )
				return false;
			position = newPosition;
			return true;
		}

		void Prop( string name )
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
		}
	}
}
