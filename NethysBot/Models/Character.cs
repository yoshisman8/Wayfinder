using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Models
{
	class Character
	{
		[BsonId]
		public int Id { get; set; }
		public string RemoteId { get; set; }
		public string Name { get; set; }
	}
}
