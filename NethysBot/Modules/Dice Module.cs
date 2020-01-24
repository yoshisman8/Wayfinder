using Dice;
using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace NethysBot.Modules
{
	public class Dice_Module : NethysBase<SocketCommandContext>
	{
		private Regex DiceRegex = new Regex(@"(\d?[dD]\d)+\s*((\+|\-|)?\s*(\d+)?)*");
		private Regex AttributeRegex = new Regex(@"(\{(\w+)\})");


		[Command("Roll"), Alias("R","Dice")]
		[Summary("Make a dice roll.")]
		[Help("roll")]
		public async Task Roll([Remainder] string Expression)
		{
			if (AttributeRegex.IsMatch(Expression))
			{
				var c = GetCharacter();
				var matches = AttributeRegex.Matches(Expression);
				if (c == null)
				{
					foreach(Match m in matches)
					{
						Expression = Expression.Replace(m.Value,"0");
					}
				}
				else
				{
					foreach(Match m in matches)
					{
						var values = JObject.Parse(c.ValuesCache);
						if (values.TryGetValue(m.Groups[2].ToString().ToLower(), out JToken v))
						{
							Expression = Expression.Replace(m.Value, (string)v["bonus"] ?? "0");
						}
						else
						{
							Expression = Expression.Replace(m.Value, "0");
						}
					}
				}
			}

			// Add trycatch here
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
		[Command("Check"),Alias("C")]
		public async Task SkillCheck([Remainder]string Skill)
		{
			Character c;

			if (Skill.Contains("-c"))
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
			c = await SheetService.SyncValues(c);

			var sheet = JObject.Parse(c.SheetCache);
			var values = JObject.Parse(c.ValuesCache);
			
			if(Skill.ToLower() == "perception")
			{
				var bonus = values["perception"]["bonus"] ?? 0;
				var result = Roller.Roll("d20 + " + bonus);

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
				var skill = from sk in sheet["skills"]
							where ((string)sk["name"]).ToLower().StartsWith(Skill.ToLower()) ||
							(sk["lore"] != null && ((string)sk["lore"]).ToLower().StartsWith(Skill.ToLower()))
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
				var result = Roller.Roll("d20 + " + bonus);

				var embed = new EmbedBuilder()
					.WithTitle(c.Name + " makes "+ (((string)s["name"]).StartsWithVowel()?"an":"") +" " + name.Uppercase() +" check!")
					.WithThumbnailUrl(c.ImageUrl)
					.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`");
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);

				await ReplyAsync("", embed.Build());
			}
		}

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

				}
			}

			return sb.ToString().Trim();
		}
	}
}
