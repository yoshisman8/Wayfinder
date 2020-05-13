using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Dice;
using System.Runtime.InteropServices;

namespace NethysBot.Modules
{

	public class Combat : NethysBase<SocketCommandContext>
	{
		private Regex BonusRegex = new Regex(@"[\+\-]+\s?\d+");

		[Command("Encounter"), Alias("Enc", "Battle", "Combat")]
		[RequireContext(ContextType.Guild)]
		public async Task NewBattle(EncArgs Args = EncArgs.Info)
		{
			Battle b = GetBattle(Context.Channel.Id);
			switch ((int)Args)
			{
				case 0:
					await ReplyAsync(" ", DisplayBattle(b, Context));
					return;
				case 1:
					if (b.Active && Context.User.Id != b.Director)
					{
						await ReplyAsync(Context.Client.GetUser(b.Director).Username + " is directing an encounter in this room already. To forcefully end this encounter, use the command `!ForceEnd` (Available only to users with the \"Manage Messages\" permission.)");
						return;
					}
					else if (Context.User.Id == b.Director && b.Active)
					{
						if (b.Participants.Count == 0)
						{
							await ReplyAsync("There are no participants on this encounter!");
							return;
						}
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x => x.Tiebreaker).Reverse().ToList();
						b.CurrentTurn = b.Participants.First();
						b.Started = true;
						b.Round = 1;
						UpdateBattle(b);
						await CurrentTurn(b, Context);
						return;
					}
					else
					{
						b.Participants = new List<Participant>();
						b.Effects = new List<BattleEffect>();
						b.LapsedEffects = new List<LapsedEffect>();
						b.Director = Context.User.Id;
						b.Active = true;
						b.Started = false;
						b.Round = 0;
						var embed = new EmbedBuilder().WithTitle("Roll for initiative!")
							.WithDescription(Context.User.Username + " has started a new encounter!")
							.AddField("Players", "Use the `!Initiative SkillName` command to enter initiative.\nYou can also use `!Initiative #` to add your initiative number manually.", true)
							.AddField("Director", "Use `!AddNPC Initaitve Name` to add NPCs to the turn order.", true)
							.AddField("Ready to go?", "Once all characters have been added, use the `!Encounter Start` command again to start the encounter.")
							.AddField("Advancing Turns", "Use `!next` to end your turn and ping the next person in the initiative order.")
							.AddField("Need more help?", "Use the `!Help Encounter` for a breakdown of all Encounter commands!");
						UpdateBattle(b);
						await ReplyAsync(" ", embed.Build());
						return;
					}
				case 2:
					if (Context.User.Id != b.Director)
					{
						await ReplyAsync("You aren't the director of this encounter! To forcefully end this battle, use the command `!ForceEnd` (Available only to users with the \"Manage Messages\" permission.)");
						return;
					}
					else
					{
						b.Active = false;
						b.Started = false;
						b.Participants = new List<Participant>();
						b.Effects = new List<BattleEffect>();
						b.LapsedEffects = new List<LapsedEffect>();
						b.Round = 0;
						UpdateBattle(b);
						await ReplyAsync("Encounter over!");
					}
					return;
			}
		}
		[Command("Initiative"), Alias("Join", "Init")]
		[Priority(2)]
		[RequireContext(ContextType.Guild)]
		public async Task Initiative(int number)
		{
			await Initiative(number, 0);
		}

		[Command("Initiative"), Alias("Join", "Init")]
		[Priority(2)]
		[RequireContext(ContextType.Guild)]
		public async Task Initiative(int number, int tiebreaker)
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			Character c = GetCharacter();

			if (c == null)
			{
				await ReplyAsync("You have no active character!");
				return;
			}
			var embed = new EmbedBuilder()
				.WithTitle(c.Name + " Rolled initiative!")
				.WithThumbnailUrl(c.ImageUrl)
				.WithDescription("Initiative: `" + number + "`");

			if (b.Participants.Any(x => x.Name.ToLower() == c.Name.ToLower()))
			{
				var i = b.Participants.FindIndex(x => x.Name.ToLower() == c.Name.ToLower());
				b.Participants[i] = new Participant() { Initiative = number, Tiebreaker = tiebreaker, Name = c.Name, Player = c.Owner };
				b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
				UpdateBattle(b);
			}
			else
			{
				b.Participants.Add(new Participant() { Initiative = number, Tiebreaker = tiebreaker, Name = c.Name, Player = c.Owner });
				b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
				UpdateBattle(b);
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}
			await ReplyAsync(" ", embed.Build());
		}

		[Command("Initiative"), Alias("Join", "Init")]
		[Priority(1)]
		[RequireContext(ContextType.Guild)]
		public async Task Initiative([Remainder]string skill = null)
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			Character c = GetCharacter();
			skill = skill?.ToLower() ?? "";

			if (c == null)
			{
				await ReplyAsync("You have no active character!");
				return;
			}

			string[] Bonuses = new string[0];
			if (BonusRegex.IsMatch(skill))
			{
				Bonuses = BonusRegex.Matches(skill).Select(x => x.Value).ToArray();
				foreach (var bo in Bonuses)
				{
					skill = skill.Replace(bo, "");
				}
				skill = skill.Trim();
			}

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + " Rolled initiative!")
				.WithThumbnailUrl(c.ImageUrl);
			var values = await SheetService.GetValues(c);
			if (skill.NullorEmpty() || skill.ToLower().Equals("perception"))
			{

				var bonus = ((int)values["perception"]["bonus"] - (int)values["perception"]["penalty"]);

				try
				{
					var results = Roller.Roll("1d20 + " + bonus + (Bonuses.Length > 0 ? string.Join(" ", Bonuses) : ""));
					embed.WithDescription("Perception: " + results.ParseResult() + " = `" + results.Value + "`");

					if (b.Participants.Any(x => x.Name.ToLower() == c.Name.ToLower()))
					{
						var i = b.Participants.FindIndex(x => x.Name.ToLower() == c.Name.ToLower());
						b.Participants[i] = new Participant() { Initiative = (int)results.Value, Tiebreaker = 0, Name = c.Name, Player = c.Owner };
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
						UpdateBattle(b);
					}
					else
					{
						b.Participants.Add(new Participant() { Initiative = (int)results.Value, Tiebreaker = 0, Name = c.Name, Player = c.Owner });
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
						UpdateBattle(b);
					}
				}
				catch
				{
					await ReplyAsync("Something went wrong when rolling the dice, make sure you only added a bonus/penalty using +X or -X where x is an integer (No fractions or decimals).");
					return;
				}
			}
			else
			{
				var sheet = await SheetService.GetFullSheet(c);

				var snk = from sk in sheet["skills"].Children()
						  where ((string)sk["name"]).ToLower().StartsWith(skill.ToLower()) ||
						  (sk["lore"] != null && ((string)sk["lore"]).ToLower().StartsWith(skill.ToLower()))
						  orderby sk["name"]
						  select sk;

				if (snk.Count() == 0)
				{
					await ReplyAsync("You have no skill whose name starts with that.");
					return;
				}

				var s = snk.FirstOrDefault();
				string name = (string)s["lore"] ?? (string)s["name"];
				var bonus = (int)values[name.ToLower()]["bonus"] - (int)values[name.ToLower()]["penalty"];

				try
				{
					var results = Roller.Roll("1d20 + " + bonus + (Bonuses.Length > 0 ? string.Join(" ", Bonuses) : ""));
					embed.WithDescription(name.Uppercase() + ": " + results.ParseResult() + " = `" + results.Value + "`");

					if (b.Participants.Any(x => x.Name.ToLower() == c.Name.ToLower()))
					{
						var i = b.Participants.FindIndex(x => x.Name.ToLower() == c.Name.ToLower());
						b.Participants[i] = new Participant() { Initiative = (int)results.Value, Tiebreaker = 0, Name = c.Name, Player = c.Owner };
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
						UpdateBattle(b);
					}
					else
					{
						b.Participants.Add(new Participant() { Initiative = (int)results.Value, Tiebreaker = 0, Name = c.Name, Player = c.Owner });
						b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x=> x.Tiebreaker).Reverse().ToList();
						UpdateBattle(b);
					}
				}
				catch
				{
					await ReplyAsync("Something went wrong when rolling the dice, make sure you only added a bonus/penalty using +X or -X where x is an integer (No fractions or decimals).");
					return;
				}
			}


			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}
			await ReplyAsync(" ", embed.Build());
		}
		[Command("AddNPC")]
		[RequireContext(ContextType.Guild)]
		public async Task addnpc(int initiative, [Remainder] string Name)
		{
			await addnpc(initiative, 0, Name);
		}

		[Command("AddNPC")]
		[RequireContext(ContextType.Guild)]
		public async Task addnpc(int initiative, int tiebreaker, [Remainder] string Name)
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			if (b.Active && b.Director != Context.User.Id)
			{
				await ReplyAsync("You are not the Game Master for this encounter.");
				return;
			}
			if (b.Participants.Any(x => x.Name.ToLower() == Name.ToLower()))
			{
				var i = b.Participants.FindIndex(x => x.Name.ToLower() == Name.ToLower());
				b.Participants[i] = new Participant() { Initiative = initiative, Tiebreaker = tiebreaker, Name = Name, Player = b.Director };
				b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x => x.Tiebreaker).Reverse().ToList();
				UpdateBattle(b);
				await ReplyAsync("Changed \"" + Name + "\"'s Initiative to `" + initiative + "`.");
			}
			else
			{
				b.Participants.Add(new Participant() { Initiative = initiative, Tiebreaker = tiebreaker, Name = Name, Player = b.Director });
				b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x => x.Tiebreaker).Reverse().ToList();
				UpdateBattle(b);
				await ReplyAsync("Added NPC \"" + Name + "\" to the encounter with initiative `" + initiative + "`.");
			}
		}

		[Command("Remove")]
		[RequireContext(ContextType.Guild)]
		public async Task remNPC([Remainder] string Name)
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			if (b.Active && b.Director != Context.User.Id)
			{
				await ReplyAsync("You are not the Game Master for this encounter.");
				return;
			}

			var pars = b.Participants.FindAll(x => x.Name.ToLower().StartsWith(Name.ToLower()));
			if (pars.Count() == 0)
			{
				await ReplyAsync("There are no participants in this encounter with a name that starts with \"" + Name + "\".");
				return;
			}
			else if (pars.Count() > 1)
			{
				pars = pars.Take(Math.Min(5, pars.Count())).ToList();

				var sb = new StringBuilder("Multiple results were found:\n");

				for (int i = 0; i < pars.Count(); i++)
				{
					sb.AppendLine("`[" + i + "]` " + pars[i].Name);
				}
				var msg = await ReplyAsync(sb.ToString());
				var reply = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
				if (reply == null)
				{
					await msg.ModifyAsync(x => x.Content = "Timed out on selection.");
					return;
				}
				if (int.TryParse(reply.Content, out int index))
				{
					if (index >= pars.Count())
					{
						await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
						return;
					}
					else
					{
						try
						{
							await reply.DeleteAsync();
						}
						catch
						{

						}

						var p = pars[index];
						await removeParticipantFromBattle(b, p);
						return;
					}
				}
			}
			else
			{
				var p = pars.FirstOrDefault();
				await removeParticipantFromBattle(b, p);
				return;
			}
		}

		private async Task removeParticipantFromBattle(Battle b, Participant p)
		{
			b.Participants.Remove(p);
			b.Participants = b.Participants.OrderBy(x => x.Initiative).ThenBy(x => x.Tiebreaker).Reverse().ToList();
			if (p.Name == b.CurrentTurn.Name)
			{
				b.CurrentTurn = b.Participants.FirstOrDefault();
			}
			//remove any effects owned by the removed participant.
			foreach (BattleEffect be in b.Effects.FindAll(x => x.HostCharacter == p.Name.ToLower()))
			{
				b.Effects.Remove(be);
			}
			UpdateBattle(b);
			await ReplyAsync(p.Name + " has been removed form initiative.");
		}

		[Command("ForceEnd")]
		[RequireUserPermission(ChannelPermission.ManageMessages)]
		[RequireContext(ContextType.Guild)]
		public async Task forceend()
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			b.Active = false;
			b.Started = false;
			UpdateBattle(b);
			await ReplyAsync("The encounter in this room has been forcefully ended.");
		}
		[Command("Next")]
		[RequireContext(ContextType.Guild)]
		public async Task next()
		{
			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			if (!b.Started)
			{
				await ReplyAsync("This encounter hasn't started yet. The Game Master has use the `!Encounter Start` command to start the encounter.");
				return;
			}
			if (b.CurrentTurn.Player != Context.User.Id && b.Director != Context.User.Id)
			{
				await ReplyAsync("It is not your turn!");
				return;
			}
			await NextTurn(b, Context);
		}

		[Command("RemoveEffect")]
		[RequireContext(ContextType.Guild)]
		public async Task RemoveEffect(string name, params string[] targetsArr)
		{

			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}

			//the targetsArr param is split into space-delimited entries UNLESS those spaces are within " ".
			List<string> validTargets = new List<string>();


			var targetsLower = targetsArr.Select(s => s.ToLower());

			//for each target, remove effects
			//if no targets, remove all matching effects
			var effectsToRemove = targetsLower.Count() > 1 ? b.Effects.FindAll(x => x.Name == name && targetsLower.Contains(x.HostCharacter.ToLower())) : b.Effects.FindAll(x => x.Name == name);
			foreach(BattleEffect be in effectsToRemove)
			{
				b.Effects.Remove(be);
				validTargets.Add(be.HostCharacter);
			}

			if(validTargets.Any())
			{
				UpdateBattle(b);
				await ReplyAsync("\""+name+"\" removed from: "+ string.Join(" ,",validTargets));
			}
			else
			{
				await ReplyAsync("No valid effects found to remove, or no valid targets found to remove from.");
			}
		}

		[Command("AddEffect")]
		[RequireContext(ContextType.Guild)]
		public async Task AddEffect(string name, int duration, params string[] targetsArr)
		{
			//the targetsArr param is split into space-delimited entries UNLESS those spaces are within " ".
			List<string> badTargets = new List<string>();
			List<string> validTargets = new List<string>();
			bool didModify = false;

			var b = GetBattle(Context.Channel.Id);
			if (!b.Active)
			{
				await ReplyAsync("There is no encounter happening on this channel. Start one with `!Encounter Start`");
				return;
			}
			//for each target, add effects
			foreach(string target in targetsArr)
			{
				if(b.Participants.Any(x => x.Name.ToLower() == target.ToLower()))
				{
					//look for the named effect on the target.
					int ind = b.Effects.FindIndex(x => x.Name == name && x.HostCharacter == target.ToLower());
					if(ind == -1)
					{
						//new effect, add it
						b.Effects.Add( new BattleEffect() { Duration = duration, IsEndOfTurn = true, Name = name, HostCharacter = target.ToLower() });
					}
					else
					{
						//existing effect, update it
						b.Effects[ind] = new BattleEffect() { Duration = duration, IsEndOfTurn = true, Name = name, HostCharacter = target.ToLower() };
						didModify = true;
					}
					validTargets.Add(target);
				}
				else
				{
					//invalid target. Store to inform the user.
					badTargets.Add(target);
				}
			}

			if (validTargets.Count > 0)
			{
				string response = string.Format("{0} Effect \"{1}\" to {2} with duration of {3} round(s). {4}",
												didModify ? "Updated" : "Added",
												name,
												validTargets.Count > 1 ? "multiple characters" : validTargets[0],
												duration,
												badTargets.Count == 0 ? "" : Environment.NewLine + "Invalid targets: "+string.Join(", ", badTargets));

				UpdateBattle(b);
				await ReplyAsync(response);
			}
			else
			{
				
				await ReplyAsync("No valid targets found to Add Effect. Invalid targets: " + string.Join(", ", badTargets));
			}
		}

		private Battle GetBattle(ulong channel)
		{
			var col = Database.GetCollection<Battle>("Battles");
			if (col.Exists(x => x.Channel == channel))
			{
				return col.FindOne(x => x.Channel == channel);
			}
			else
			{
				var b = new Battle()
				{
					Channel = channel,
				};
				col.Insert(b);
				col.EnsureIndex(x => x.Channel);
				return col.FindOne(x => x.Channel == channel);
			}
		}
		private void UpdateBattle(Battle b)
		{
			var col = Database.GetCollection<Battle>("Battles");
			col.Update(b);
		}

		public async Task CurrentTurn(Battle b, SocketCommandContext context)
		{
			var channel = context.Guild.GetTextChannel(b.Channel);

			if (b.CurrentTurn.Player > 0)
			{
				var player = context.Client.GetUser(b.CurrentTurn.Player);
				await channel.SendMessageAsync(player.Mention + ", " + b.CurrentTurn.Name + "'s turn!", false, DisplayBattle(b, context));
			}
			else
			{
				var player = context.Client.GetUser(b.Director);
				await channel.SendMessageAsync(player.Mention + ", " + b.CurrentTurn.Name + "'s turn!", false, DisplayBattle(b, context));
			}
		}
		public async Task NextTurn(Battle b, SocketCommandContext context)
		{
			int i = b.Participants.IndexOf(b.CurrentTurn);
			string prevName = b.CurrentTurn.Name;
			//remove durative effect notices from last turn
			b.LapsedEffects.Clear();
			
			if (i + 1 >= b.Participants.Count)
			{
				b.CurrentTurn = b.Participants.First();
				b.Round++;
				
			}
			else
			{
				b.CurrentTurn = b.Participants[i + 1];
			}

			string currName = b.CurrentTurn.Name;
			//find all effects that ended on this round ||
			//find all effects that end start of next round
			var progressingEffects = b.Effects.FindAll(ef => (ef.HostCharacter.Equals(prevName.ToLower()) && ef.IsEndOfTurn == true) ||
														     (ef.HostCharacter.Equals(currName.ToLower()) && ef.IsStartOfTurn == true));
			elapseEffects(b, progressingEffects);

			UpdateBattle(b);
			await CurrentTurn(b, context);
		}

		private static void elapseEffects(Battle b, List<BattleEffect> progressingEffects)
		{
			foreach (BattleEffect eff in progressingEffects)
			{
				int ind = b.Effects.IndexOf(eff);
				b.Effects[ind] = eff.ElapseRound();
			}

			var toBeRemoved = b.Effects.FindAll(eff => eff.Duration <= 0);
			foreach(BattleEffect eff in toBeRemoved)
			{
				b.Effects.Remove(eff);
			}

			var durativeEffects = progressingEffects.FindAll(x => x.Duration == 1).Select(s => new LapsedEffect() {Name = s.Name, HostCharacter = s.HostCharacter });

			if (durativeEffects.Count() > 0) b.LapsedEffects.AddRange(durativeEffects);
		}

		private Embed DisplayBattle(Battle b, SocketCommandContext context)
		{
			if (!b.Active)
			{
				return new EmbedBuilder().WithDescription("No encounter is currently being ran on this room.").Build();
			}

			//check for lapsedEffects
			var lapsedEffects = b.LapsedEffects;
			string elapsedEffects = "";
			if(lapsedEffects.Count > 0)
			{
				foreach (LapsedEffect le in lapsedEffects)
				{
					elapsedEffects += Environment.NewLine + le.HostCharacter + "'s \"" + le.Name + "\" has expired!";
				}
			}


			var embed = new EmbedBuilder()
				.WithTitle("Encounter")
				.WithDescription("Started? " + (b.Started ? "Yes" : "No") + Environment.NewLine +
				"Round: `" + b.Round + "`" + elapsedEffects)
				.AddField("Game Master", context.Client.GetUser(b.Director).Mention, true);
			var summary = new StringBuilder();
			foreach (var p in b.Participants)
			{
				if (p.Name == b.CurrentTurn.Name) summary.AppendLine("`" + p.InitiativeReadout + "` - " + p.Name + " (Current)");
				else summary.AppendLine("`" + p.InitiativeReadout + "` - " + p.Name);
			}
			if (summary.ToString().NullorEmpty()) summary.AppendLine("No participants");

			embed.AddField("Participants", summary.ToString(), true);
			//only add effects if they're present on the current character
			if (b.Effects.Any(x => x.HostCharacter == b.CurrentTurn.Name.ToLower()))
			{
				var activeEffects = b.Effects.FindAll(x => x.HostCharacter == b.CurrentTurn.Name.ToLower());
				var effectDetails = new StringBuilder();
				foreach (var eff in activeEffects)
				{
					string roundString = (eff.Duration == 1) ? "round" : "rounds";
					effectDetails.AppendLine(eff.Name + " (" + eff.Duration + " " + roundString + " remaining)");
				}

				embed.AddField("Active Effects", effectDetails.ToString(), true);
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			return embed.Build();
		}
		public enum EncArgs { Info = 0, Create = 1, New = 1, Start = 1, Stop = 2, End = 2, Delete = 2 };
	}
}
