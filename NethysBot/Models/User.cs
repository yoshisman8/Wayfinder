using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	public class User
	{
		[BsonId]
		public ulong Id { get; set; }
		/// <summary>
		/// The user's current active character
		/// </summary>
		[BsonRef("Characters")]
		public Character Character { get; set; }
		/// <summary>
		/// The user's current active companion
		/// </summary>
		[BsonRef("Characters")]
		public Character Companion { get; set; }
	}
}
