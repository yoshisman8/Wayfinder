using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace NethysBot.Models
{
	class Server
	{
		[BsonId]
		public ulong Id { get; set; }
		public string Prefix { get; set; } = "!";

	}
}
