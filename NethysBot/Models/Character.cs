using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	class Character
	{
		/// <summary>
		/// Internal ID.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// id of whoever imported the character.
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
	}
}
