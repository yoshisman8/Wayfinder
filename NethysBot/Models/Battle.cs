using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NethysBot.Models
{
	public class Battle
	{
		[BsonId]
		public int Id { get; set; }
		public ulong Channel { get; set; }
		public bool Active { get; set; }
		public ulong Director { get; set; }
		public List<Participant> Participants { get; set; } = new List<Participant>();
		public Participant CurrentTurn { get; set; }
	}
	public struct Participant
	{
		public string Name { get; set; }
		public float Initiative { get; set; }
		public ulong Player { get; set; }
	}
}
