using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	class User
	{
		[BsonId]
		public ulong Id { get; set; }
		[BsonRef("Characters")]
		public Character ActiveCharacter { get; set; }

		[BsonRef("Characters")]
		public List<Character> Characters { get; set; }
	}
}
