using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	public class Character
	{
		/// <summary>
		/// Internal ID.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary> 
		/// ids of whoever has imported the character.
		/// </summary>
		public List<ulong> Owners { get; set; } = new List<ulong>();

		/// <summary>
		/// ID on characters.pf2.tools.
		/// </summary>
		public string RemoteId { get; set; }

		/// <summary>
		/// Character Name for easy querying.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Backup of the complete sheet.
		/// </summary>
		public string SheetCache { get; set; }
		/// <summary>
		/// Backup of the values segment of the sheet.
		/// </summary>
		public string ValuesCache { get; set; }
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
		/// RGB colors for the embed's sidebar
		/// </summary>
		public int[] Color { get; set; } = null;
	}

	public enum SheetType { character, companion }
}
