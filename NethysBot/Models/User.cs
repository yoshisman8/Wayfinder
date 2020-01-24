using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	public class User
	{
		[BsonId]
		public string Id { get; set; }
		/// <summary>
		/// ID of the user's current active character
		/// </summary>
		public string Character { get; set; }
		/// <summary>
		/// ID of the user's current active companion
		/// </summary>
		public string Companion { get; set; }
	}
}
