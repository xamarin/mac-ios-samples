using System;
using Foundation;

namespace Adventure
{
	public class UserData : DictionaryContainer
	{
		private const string PlayerKey = "kPlayer";

		public Player Player
		{
			get {
				return (Player)Dictionary [PlayerKey];
			}
			set {
				Dictionary [PlayerKey] = value;
			}
		}

		public UserData()
		{
		}

		public UserData (NSMutableDictionary dictionary)
			: base(dictionary)
		{
		}
	}
}

