using Dice;
using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace NethysBot.Modules
{
	public class DiceModule : NethysBase<SocketCommandContext>
	{
		private Regex DiceRegex = new Regex(@"(\d?[dD]\d)+\s*((\+|\-|)?\s*(\d+)?)*");
		private Regex AttributeRegex = new Regex(@"(\{(\w+)\})");


		[Command("Roll"), Alias("R", "Dice")]
		[Summary("Make a dice roll.")]
		public async Task Roll([Remainder] string Expression)
		{
			if (AttributeRegex.IsMatch(Expression))
			{
				var c = GetCharacter();
				if (c == null)
				{
					await ReplyAsync("You have no active character.");
					return;
				}
				Expression = ParseValues(Expression, c);
			}
			try
			{
				var results = Roller.Roll(Expression);
				decimal total = results.Value;


				var embed = new EmbedBuilder()
					.WithTitle(Context.User.Username + " rolled some dice.")
					.WithDescription(ParseResult(results) + "\nTotal = `" + total + "`");
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);

				await ReplyAsync(" ", embed.Build());
			}

			catch (DiceException e)
			{
				await ReplyAsync("No dice were found to roll");
			}

		}
		[Command("Check"), Alias("C")]
		public async Task SkillCheck([Remainder]string Skill)
		{
			Character c;
			string[] input = Skill.Split("/");
			if (input.Length > 1 && input.Contains("c"))
			{
				c = GetCompanion();
				if(c == null)
				{
					await ReplyAsync("You have no active companion.");
					return;
				}
			}
			else
			{
				c = GetCharacter();
				if (c == null)
				{
					await ReplyAsync("You have no active character.");
					return;
				}

			}
			string b = input.Where(x=>x.ToLower().StartsWith('b')).FirstOrDefault();
			if (!b.NullorEmpty())
			{
				b = "+" + b.Substring(1);
			}
			else b = null;
			
			var sheet = await SheetService.GetFullSheet(c);
			var values = await SheetService.GetValues(c);
;
			if(!values.HasValues || !sheet.HasValues)
			{
				await ReplyAsync("Seems like your sheet is missing data. Try using the command `!sync` to sync your sheet data. If this error persists try changing any value on your sheet over at [pf2.tools](http://character.pf2.tools) in order to refresh these values and then sync your sheet.");
				return;
			}

			
			if(input[0].ToLower() == "perception")
			{
				var bonus = values["perception"]["bonus"] ?? 0;
				var result = Roller.Roll("d20 + " + bonus+ (b??""));

				var embed = new EmbedBuilder()
					.WithTitle(c.Name + " makes a Preception check!")
					.WithImageUrl(c.ImageUrl)
					.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`");
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);

				await ReplyAsync("", embed.Build());
				return;
			}
			else
			{
				var skill = from sk in sheet["skills"].Children()
							where ((string)sk["name"]).ToLower().StartsWith(input[0].ToLower()) ||
							(sk["lore"] != null && ((string)sk["lore"]).ToLower().StartsWith(input[0].ToLower()))
							orderby sk["name"]
							select sk;

				if(skill.Count() == 0)
				{
					await ReplyAsync("You have no skill whose name starts with that.");
					return;
				}

				var s = skill.FirstOrDefault();
				string name = (string)s["lore"] ?? (string)s["name"];
				var bonus = values[name.ToLower()]["bonus"] ?? 0;
				var result = Roller.Roll("d20 + " + bonus+(b ?? ""));

				var embed = new EmbedBuilder()
					.WithTitle(c.Name + " makes "+ (((string)s["name"]).StartsWithVowel()?"an":"") +" " + name.Uppercase() +" check!")
					.WithThumbnailUrl(c.ImageUrl)
					.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`");

				if (Context != null && c.Owners.Any(x => x.Id == Context.User.Id))
				{
					var id = c.Owners.FindIndex(x => x.Id == Context.User.Id);
					if (c.Owners[id].Color != null)
					{
						embed.WithColor(new Color(c.Owners[id].Color[0], c.Owners[id].Color[1], c.Owners[id].Color[2]));
					}
					if (!c.Owners[id].ImageUrl.NullorEmpty()) embed.WithThumbnailUrl(c.Owners[id].ImageUrl);
				}

				await ReplyAsync("", embed.Build());
			}
		}
		public enum Saves { fort = 1, fortitude = 1, reflex = 2, will = 3 }
		private string ParseResult (RollResult result)
		{
			var sb = new StringBuilder();

			foreach (var dice in result.Values)
			{
				switch (dice.DieType)
				{
					case DieType.Normal:
						switch (dice.NumSides)
						{
							case 4:
								sb.Append(Icons.d4[(int)dice.Value] + " ");
								break;
							case 6:
								sb.Append(Icons.d6[(int)dice.Value] + " ");
								break;
							case 8:
								sb.Append(Icons.d8[(int)dice.Value] + " ");
								break;
							case 10:
								sb.Append(Icons.d10[(int)dice.Value] + " ");
								break;
							case 12:
								sb.Append(Icons.d12[(int)dice.Value] + " ");
								break;
							case 20:
								sb.Append(Icons.d20[(int)dice.Value] + " ");
								break;
							default:
								sb.Append(dice.Value);
								break;
						}
						break;
					case DieType.Special:
						switch ((SpecialDie)dice.Value)
						{
							case SpecialDie.Add:
								sb.Append("+ ");
								break;
							case SpecialDie.CloseParen:
								sb.Append(") ");
								break;
							case SpecialDie.Comma:
								sb.Append(", ");
								break;
							case SpecialDie.Divide:
								sb.Append("/ ");
								break;
							case SpecialDie.Multiply:
								sb.Append("* ");
								break;
							case SpecialDie.Negate:
								sb.Append("- ");
								break;
							case SpecialDie.OpenParen:
								sb.Append(") ");
								break;
							case SpecialDie.Subtract:
								sb.Append("- ");
								break;
							case SpecialDie.Text:
								sb.Append(dice.Data);
								break;
						}
						break;
					default:
						sb.Append(dice.Value + " ");
						break;
				}
			}

			return sb.ToString().Trim();
		}
		private async Task<string> ParseValues(string Raw, Character c)
		{
			var values = await SheetService.GetValues(c);
			if(values == null)
			{
				foreach (Match m in AttributeRegex.Matches(Raw))
				{
					Raw = Raw.Replace(m.Value, "");
				}
				return Raw;
			}

			foreach(Match m in AttributeRegex.Matches(Raw))
			{
				if (Enum.TryParse<Score>(m.Groups[2].Value.ToLower(),out Score sc))
				{
					Raw = Raw.Replace(m.Value, ((int)values[m.Groups[2].Value.ToLower()]["value"]).Modifier().ToString());
				}
				if (values.TryGetValue(m.Groups[2].Value,out JToken jToken))
				{
					Raw = Raw.Replace(m.Value, (string)values[m.Groups[2].Value.ToLower()]["bonus"] ?? "0");
				}
				else
				{
					Raw = Raw.Replace(m.Value, "");
				}
			}
			return Raw;
		}
	}
}
