using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	public class Character
	{
		[BsonId]
		public int InternalId { get; set; }
		/// <summary> 
		/// Individual per-user settings
		/// </summary>
		public ulong Owner { get; set; }
		/// <summary>
		/// ID on characters.pf2.tools.
		/// </summary>
		public string RemoteId { get; set; }
		/// <summary>
		/// Character Name for easy querying.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Cache of the Values
		/// </summary>
		public string Values { get; set; }
		/// <summary>
		/// Date of the last time the sheet's values were updated
		/// </summary>
		public DateTime ValuesLastUpdated { get; set; }
		/// <summary>
		/// Date of the last time the sheet was updated.
		/// </summary>
		public DateTime LastUpdated { get; set; }
		/// <summary>
		/// Whether or not this is a character sheet or a companion sheet.
		/// </summary>
		public SheetType Type { get; set; }
		/// <summary>
		/// Url of the character's thumbnail image
		/// </summary>
		public string ImageUrl { get; set; }
		/// <summary>
		/// Url of the character's familiar
		/// </summary>
		public string FamImg { get; set; }
		public string Familiar { get; set; } 
		public int[] Color { get; set; } = null;

	}
	public enum SheetType { Character, Companion }
	public enum Score { strength, dexterity, constitution, intelligence, wisdom, charisma }

}
