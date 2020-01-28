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
				Expression = await ParseValues(Expression, c);
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
		public async Task SkillCheck(string Skill, params string[] args)
		{
			Character c;
			if (args.Length >= 1 && args.Contains("-c"))
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

			string arguments = string.Join(" ", args).Replace("-c","");
			
			var sheet = await SheetService.GetFullSheet(c);
			var values = await SheetService.GetValues(c);
;
			if(values == null|| sheet == null)
			{
				var embed = new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
					.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.");
				await ReplyAsync("", embed.Build());
				return;
			}

			
			if(Skill.ToLower() == "perception")
			{
				JToken bonus;
				string message = "";
				if (args.Contains("-f"))
				{
					bonus = values["famperception"]["bonus"] ?? 0;
					arguments = arguments.Replace("-f", "");
					message = c.Name + "'s familiar makes a Preception check!";
				}
				else
				{
					bonus = values["perception"]["bonus"] ?? 0;
					message = c.Name + " makes a Preception check!";
				}
				var result = Roller.Roll("d20 + " + bonus + arguments);

				var embed = new EmbedBuilder()
					.WithTitle(message)
					.WithThumbnailUrl(c.ImageUrl)
					.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`")
					.WithFooter((c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString())); ;
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);

				if (c.Color != null)
				{
					embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
				}
				
				await ReplyAsync("", embed.Build());
				return;
			}
			else
			{
				JToken bonus;
				string message = "";
				if (args.Contains("-f"))
				{
					switch (Skill.ToLower())
					{
						case "acrobatics":
							bonus = values["famacrobatics"]["bonus"] ?? 0;
							message = c.Name + "'s familiar makes an Acrobatics check!";
							break;
						case "stealth":
							bonus = values["famstealth"]["bonus"] ?? 0;
							message = c.Name + "'s familiar makes a Stealth check!";
							break;
						default:
							bonus = values["famother"]["bonus"] ?? 0;
							message = c.Name + "'s familiar makes a skill check!";
							break;
					}
					arguments = arguments.Replace("-f", "");
				}
				else
				{
					var skill = from sk in sheet["skills"].Children()
								where ((string)sk["name"]).ToLower().StartsWith(Skill.ToLower()) ||
								(sk["lore"] != null && ((string)sk["lore"]).ToLower().StartsWith(Skill.ToLower()))
								orderby sk["name"]
								select sk;

					if (skill.Count() == 0)
					{
						await ReplyAsync("You have no skill whose name starts with that.");
						return;
					}

					var s = skill.FirstOrDefault();
					string name = (string)s["lore"] ?? (string)s["name"];
					message = c.Name + " makes " + (name.StartsWithVowel() ? "an " : "a ") + name.Uppercase() + " check!";
					bonus = values[name.ToLower()]["bonus"] ?? 0;
				}

				var result = Roller.Roll("d20 + " + bonus + arguments);

				var embed = new EmbedBuilder()
					.WithTitle(message)
					.WithThumbnailUrl(c.ImageUrl)
					.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`")
					.WithFooter((c.ValuesLastUpdated.Outdated()? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString()));
				
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
		}
		[Command("Save"), Alias("sv")]
		public async Task Save(Ability Throw, params string[] args)
		{
			Character c;
			if (args.Length >= 1 && args.Contains("-c"))
			{
				c = GetCompanion();
				if (c == null)
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

			string arguments = string.Join(" ", args).Replace("-c", "");

			var sheet = await SheetService.GetFullSheet(c);
			var values = await SheetService.GetValues(c);

			if (values == null || sheet == null)
			{
				var err = new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
					.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.");
				await ReplyAsync("", err.Build());
				return;
			}

			JToken bonus = null;
			string message = "";
			if (args.Contains("-f"))
			{
				switch ((int)Throw)
				{
					case 1:
						bonus = values["famfort"]["bonus"] ?? 0;
						message = c.Name + "'s familiar makes a fortitude check!";
						break;
					case 2:
						bonus = values["famref"]["bonus"] ?? 0;
						message = c.Name + "'s familiar makes a reflex check!";
						break;
					case 3:
						bonus = values["famwill"]["bonus"] ?? 0;
						message = c.Name + "'s familiar makes a will check!";
						break;
				}
				arguments = arguments.Replace("-f", "");
			}
			else
			{
				switch ((int)Throw)
				{
					case 1:
						bonus = values["fortitude"]["bonus"] ?? 0;
						message = c.Name + " makes a fortitude check!";
						break;
					case 2:
						bonus = values["reflex"]["bonus"] ?? 0;
						message = c.Name + " makes a reflex check!";
						break;
					case 3:
						bonus = values["will"]["bonus"] ?? 0;
						message = c.Name + " makes a will check!";
						break;
				}
			}

			var result = Roller.Roll("d20 + " + bonus + arguments);

			var embed = new EmbedBuilder()
				.WithTitle(message)
				.WithThumbnailUrl(c.ImageUrl)
				.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`")
				.WithFooter((c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString()));

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

		[Command("Ability"), Alias("A")]
		public async Task ability(Saves saves, params string[] args)
		{
			Character c;
			if (args.Length >= 1 && args.Contains("-c"))
			{
				c = GetCompanion();
				if (c == null)
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

			string arguments = string.Join(" ", args).Replace("-c", "");

			var sheet = await SheetService.GetFullSheet(c);
			var values = await SheetService.GetValues(c);

			if (values == null || sheet == null)
			{
				var err = new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
					.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.");
				await ReplyAsync("", err.Build());
				return;
			}

			JToken bonus = null;
			string message = "";

			switch ((int)saves)
			{
				case 1:
					bonus = ((int)values["strength"]["values"]).GetModifier();
					message = c.Name + " makes a strength check!";
					break;
				case 2:
					bonus = ((int)values["dexterity"]["values"]).GetModifier();
					message = c.Name + " makes a dexterity check!";
					break;
				case 3:
					bonus = ((int)values["constitution"]["values"]).GetModifier();
					message = c.Name + " makes a constitution check!";
					break;
				case 4:
					bonus = ((int)values["intelligence"]["values"]).GetModifier();
					message = c.Name + " makes a intelligence check!";
					break;
				case 5:
					bonus = ((int)values["wisdom"]["values"]).GetModifier();
					message = c.Name + " makes a wisdom check!";
					break;
				case 6:
					bonus = ((int)values["charisma"]["values"]).GetModifier();
					message = c.Name + " makes a charisma check!";
					break;
			}

			var result = Roller.Roll("d20 + " + bonus + arguments);

			var embed = new EmbedBuilder()
				.WithTitle(message)
				.WithThumbnailUrl(c.ImageUrl)
				.WithDescription(ParseResult(result) + "\nTotal = `" + result.Value + "`")
				.WithFooter((c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString()));

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
		
		[Command("Strike"), Alias("S")]
		public async Task Attack(string Strike = null, params string[] args)
		{
			Character c;
			if (args.Length >= 1 && args.Contains("-c"))
			{
				c = GetCompanion();
				if (c == null)
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
			if (Strike.NullorEmpty())
			{
				var strikes = await SheetService.Get(c, "strikes");
				var embed = new EmbedBuilder()
					.WithTitle(c.Name + "'s strikes")
					.WithThumbnailUrl(c.ImageUrl);

				if (strikes.Count() == 0)
				{
					await ReplyAsync(c.Name + " has no strikes.");
					return;
				}				

				var sb = new StringBuilder();
				foreach (var s in strikes)
				{
					string act = Icons.Actions["1"];
					if (!((string)s["action"]).NullorEmpty())
					{
						if (Icons.Actions.TryGetValue((string)s["action"], out string ic))
						{
							act = ic;
						}
						else act = "[" + s["action"] + "]";
					}
					sb.AppendLine(Icons.Strike[(string)s["attack"]] +" " +(((string)s["name"]).NullorEmpty()? "Unnamed Strike": (string)s["name"]) + " " + act);
				}
				embed.WithDescription(sb.ToString());

				if (c.Color != null)
				{
					embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
				}
				else
				{
					Random randonGen = new Random();
					Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
					randonGen.Next(255));
					embed.WithColor(randomColor);
				}

				await ReplyAsync("", embed.Build());
				return;
			}
			else
			{
				string arguments = string.Join(" ", args).Replace("-c", "");

				var Jstrikes = await SheetService.Get(c, "strikes");
				var values = await SheetService.GetValues(c);

				if (values == null || Jstrikes == null)
				{
					var err = new EmbedBuilder()
						.WithTitle("Click here")
						.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
						.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.");
					await ReplyAsync("", err.Build());
					return;
				}

				var strikes = from sk in Jstrikes
							  where ((string)sk["name"]).ToLower().StartsWith(Strike.ToLower())
							  orderby sk["name"]
							  select sk;

				if (strikes.Count() == 0)
				{
					await ReplyAsync("You have no strikes whose name starts with that.");
					return;
				}

				var s = strikes.FirstOrDefault();
				var embed = new EmbedBuilder()
					.WithTitle(c.Name + " strikes with a " + (((string)s["name"]).NullorEmpty() ? "Unnamed Strike" : (string)s["name"]) + "!")
					.WithThumbnailUrl(c.ImageUrl);

				if (c.Color != null)
				{
					embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
				}
				else
				{
					Random randonGen = new Random();
					Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
					randonGen.Next(255));
					embed.WithColor(randomColor);
				}

				await ReplyAsync("Rolling...");

				string hit = "";
				string dmg = "";
				string bonus = "";
				string dmgtype = "Untyped";

				if ((string)s["attack"] == "spell")
				{
					hit = (string)values["ranged " + (string)s["name"]]["bonus"];
					dmg = (string)s["overridedamage"];
					if (!dmg.NullorEmpty() && AttributeRegex.IsMatch(dmg))
					{
						dmg = await ParseValues(dmg, c, values);
					}

					string summary = "";

					var result = Roller.Roll("d20 + " + hit + arguments);

					summary += "Attack roll: " + ParseResult(result) + " = `" + result.Value + "`";

					if (!dmg.NullorEmpty())
					{
						try
						{
							RollResult result2 = Roller.Roll(dmg + bonus);
							summary += "\n" + dmgtype.Uppercase() + " damage: " + ParseResult(result2) + " = `" + result2.Value + "` ";

							if (!((string)s["extradamage"]).NullorEmpty())
							{
								RollResult result3 = Roller.Roll((string)s["extradamage"]);
								summary += "\n" + ((string)s["overridedamage"]).Uppercase() + " damage: " + ParseResult(result3) + " = `" + result3.Value + "`";
							}
						}
						catch
						{
							await ReplyAsync("It seems like this strike doesn't have a valid dice roll on its damage or additional damage fields. If this is a spell make sure you have a valid dice expression on the damage fields.");
							return;
						}
					}


					embed.WithDescription(summary)
						.WithFooter((c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString()));



					await ReplyAsync(" ", embed.Build());
				}
				else
				{
					if (!((string)s["weapon"]).NullorEmpty())
					{
						var items = await SheetService.Get(c, "items");
						var i = items.First(x => (string)x["id"] == (string)s["weapon"]);
						dmgtype = (string)i["damagetype"] ?? "Untyped";
					}
					else
					{
						dmgtype = "Bludgeoning";
					}

					if ((string)s["attack"] == "melee")
					{
						hit = (string)values["melee " + (string)s["name"]]["bonus"];
						bonus += ((int)values["strength"]["value"]).PrintModifier();
					}
					else
					{
						hit = (string)values["ranged " + (string)s["name"]]["bonus"];
					}
					
					dmg = (string)values["damagedice " + (string)s["name"]]["value"] + GetDie((int)values["damagedie " + (string)s["name"]]["value"]);

					bonus += (int)s["moddamage"] > 0 ? ((int)s["moddamage"]).ToModifierString() : "";

					string summary = "";

					var result = Roller.Roll("d20 + " + hit + arguments);

					summary += "Attack roll: " + ParseResult(result) + " = `" + result.Value + "`";

					if (!dmg.NullorEmpty())
					{
						try
						{
							RollResult result2 = Roller.Roll(dmg + bonus);
							summary += "\n" + dmgtype.Uppercase() + " damage: " + ParseResult(result2) + " = `" + result2.Value + "` ";

							if (!((string)s["extradamage"]).NullorEmpty())
							{
								RollResult result3 = Roller.Roll((string)s["extradamage"]);
								summary += "\n" + ((string)s["overridedamage"]).Uppercase() + " damage: " + ParseResult(result3) + " = `" + result3.Value + "`";
							}
						}
						catch
						{
							await ReplyAsync("It seems like this strike doesn't have a valid dice roll on its damage or additional damage fields. If this is a spell make sure you have a valid dice expression on the damage fields.");
							return;
						}
					}


					embed.WithDescription(summary)
						.WithFooter((c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Roll might not be accurate" : DateTime.Now.ToString()));



					await ReplyAsync(" ", embed.Build());
				}
			}
		}

		public enum Saves { fort = 1, fortitude = 1, reflex = 2, will = 3 }
		public enum Ability { strength = 1, str = 1, dexterity = 2, dex = 2, constitution = 3, con = 3, intelligence = 4, wisdom = 5, wis = 5, charisma = 6, cha = 6 }

		private string[] DieScale = { "d1","d2","d4","d6","d8","d10", "d12" };
		private string GetDie(int mod)
		{
			mod--;
			if (mod <= 0) return DieScale[0];
			else if (mod > 6) return DieScale[6] + "+" + (mod - 6);
			else return DieScale[mod];
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
					default:
						sb.Append(dice.Value + " ");
						break;
				}
			}

			return sb.ToString().Trim();
		}
		private async Task<string> ParseValues(string Raw, Character c, JObject values = null)
		{
			if(values == null)
			{
				values = await SheetService.GetValues(c);
				if (values == null)
				{
					foreach (Match m in AttributeRegex.Matches(Raw))
					{
						Raw = Raw.Replace(m.Value, "");
					}
					return Raw;
				}
			}

			foreach(Match m in AttributeRegex.Matches(Raw))
			{
				if (Enum.TryParse(m.Groups[2].Value.ToLower(),out Score sc))
				{
					Raw = Raw.Replace(m.Value, ((int)values[m.Groups[2].Value.ToLower()]["value"]).GetModifier().ToString());
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
