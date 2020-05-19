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
		public bool Started { get; set; }
		public ulong Director { get; set; }
		public int Round { get; set;  }
		public List<Participant> Participants { get; set; } = new List<Participant>();
		public List<BattleEffect> Effects { get; set; } = new List<BattleEffect>();
		//these are effects that dropped off after the last turn or the start of the current one.
		public List<LapsedEffect> LapsedEffects { get; set; } = new List<LapsedEffect>();
		public Participant CurrentTurn { get; set; }
	}

	public struct LapsedEffect
	{
		public string Name { get; set; }
		public string HostCharacter { get; set; }
	}

	public struct BattleEffect
	{
		public const string AllPlayers = "players";
		public const string AllNpcs = "npcs";

		public string Name { get; set; }
		public int Duration { get; set; }
		public string HostCharacter { get; set; }
		public bool IsEndOfTurn { get; set; }
		public bool IsStartOfTurn
		{
			get
			{
				return !IsEndOfTurn;
			}
			set
			{
				IsEndOfTurn = !value;
			}
		}

		internal BattleEffect ElapseRound()
		{
			return new BattleEffect()
			{
				Duration = this.Duration - 1,
				HostCharacter = this.HostCharacter,
				Name = this.Name,
				IsEndOfTurn = this.IsEndOfTurn
			};
		}
	}

	public struct Participant
	{
		public string Name { get; set; }
		public float Initiative { get; set; }
		public int Tiebreaker { get; set; }
		public ulong Player { get; set; }

		public string InitiativeReadout
		{
			get
			{
				if(Tiebreaker == 0)
				{
					return Initiative.ToString();
				}
				else
				{
					return Initiative.ToString() + "-" + Tiebreaker.ToString();
				} 
					
			}
		}
	}
}
