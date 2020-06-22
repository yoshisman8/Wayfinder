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
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace NethysBot.Modules
{
	public class DiceModule : NethysBase<SocketCommandContext>
	{
		// private Regex DiceRegex = new Regex(@"(\d?[dD]\d)+\s*((\+|\-|)?\s*(\d+)?)*"); TO BE DELETED
		private Regex AttributeRegex = new Regex(@"(\{(\w+)\})");
		private Regex BonusRegex = new Regex(@"[\+\-]+\s?\d+");

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
					.WithDescription(results.ParseResult() + "\nTotal = `" + total + "`");

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
		
		[Command("Check"), Alias("C", "Skill","SkillCheck","SC")]
		public async Task SkillCheck([Remainder] string Skill)
		{
			Character c;
			Skill = Skill.ToLower();
			bool familiar = false;

			if (Skill.Contains("-c"))
			{
				c = GetCompanion();
				if(c == null)
				{
					await ReplyAsync("You have no active companion.");
					return;
				}
				Skill = Skill.Replace("-c", "").Trim();
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
			if (Skill.Contains("-f"))
			{
				familiar = true;
				Skill = Skill.Replace("-f", "".Trim());
			}
			string[] Bonuses = new string[0];
			if (BonusRegex.IsMatch(Skill))
			{
				Bonuses = BonusRegex.Matches(Skill).Select(x => x.Value).ToArray();
				foreach (var b in Bonuses)
				{
					Skill = Skill.Replace(b, "");
				}
				Skill = Skill.Trim();
			}
			
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

			
			if(Skill == "perception"||Skill== "per" || Skill == "perc")
			{
				var embed = new EmbedBuilder();
				JToken bonus;
				string message = "";
				if (familiar)
				{
					if (c.Familiar.NullorEmpty())
					{
						await ReplyAsync("You have no named familiars.");
						return;
					}
					bonus = (int)values["famperception " + c.Familiar]["bonus"] - (int)values["famperception " + c.Familiar]["penalty"]; ;
					message = c.Name + "'s familiar makes a Perception check!";
					embed.WithThumbnailUrl(c.FamImg);
				}
				else
				{
					bonus = values["perception"]["bonus"] ?? 0;
					message = c.Name + " makes a Perception check!";
					embed.WithThumbnailUrl(c.ImageUrl);
				}
				var result = Roller.Roll("d20 + " + bonus + (Bonuses.Length>0?string.Join(" ",Bonuses):""));

				
				embed.WithTitle(message)
					.WithDescription(result.ParseResult() + "\nTotal = `" + result.Value + "`")
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
				var embed = new EmbedBuilder();
				JToken bonus;
				string message = "";
				if (familiar)
				{
					if (c.Familiar.NullorEmpty())
					{
						await ReplyAsync("You have no named familiars.");
						return;
					}
					switch (Skill.ToLower())
					{
						case "acrobatics":
							bonus = (int)values["famacrobatics " + c.Familiar]["bonus"] - (int)values["famacrobatics " + c.Familiar]["penalty"];
							message = c.Name + "'s familiar makes an Acrobatics check!";
							break;
						case "stealth":
							bonus = (int)values["famstealth " + c.Familiar]["bonus"] - (int)values["feamstealth " + c.Familiar]["penalty"];
							message = c.Name + "'s familiar makes a Stealth check!";
							break;
						default:
							bonus = (int)values["famother " + c.Familiar]["bonus"] - (int)values["famother " + c.Familiar]["penalty"];
							message = c.Name + "'s familiar makes a skill check!";
							break;
					}
					embed.WithThumbnailUrl(c.FamImg);
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
					embed.WithThumbnailUrl(c.ImageUrl);
				}

				var result = Roller.Roll("d20 + " + bonus + (Bonuses.Length > 0 ? string.Join(" ", Bonuses) : ""));

				
				embed.WithTitle(message)
					.WithDescription(result.ParseResult() + "\nTotal = `" + result.Value + "`")
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
		public async Task Save(Saves Throw, params string[] args)
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

			for (int i = 0; i < args.Length; i++)
			{
				if (!int.TryParse(args[i], out int a) && args[i].ToLower() != "-c" && args[i].ToLower() != "-f")
				{
					args[i] = " ";
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
			var embed = new EmbedBuilder();
			int bonus = 0;
			string message = "";
			if (args.Contains("-f"))
			{
				if (c.Familiar.NullorEmpty())
				{
					await ReplyAsync("You have no named familiars.");
					return;
				}
				switch ((int)Throw)
				{
					case 1:
						bonus = (int)values["famfort " + c.Familiar]["bonus"] + (int)values["famfort "+c.Familiar]["penalty"];
						message = c.Name + "'s familiar makes a fortitude check!";
						break;
					case 2:
						bonus = (int)values["famref " + c.Familiar]["bonus"] + (int)values["famref "+c.Familiar]["penalty"];
						message = c.Name + "'s familiar makes a reflex check!";
						break;
					case 3:
						bonus = (int)values["famwill " + c.Familiar]["bonus"] +(int)values["famwill "+c.Familiar]["penalty"];
						message = c.Name + "'s familiar makes a will check!";
						break;
				}
				arguments = arguments.Replace("-f", "");
				embed.WithThumbnailUrl(c.FamImg);
			}
			else
			{
				switch ((int)Throw)
				{
					case 1:
						bonus = (int)values["fortitude"]["bonus"] + (int)values["fortitude"]["penalty"];
						message = c.Name + " makes a fortitude check!";
						break;
					case 2:
						bonus = (int)values["reflex"]["bonus"] + (int)values["reflex"]["penalty"];
						message = c.Name + " makes a reflex check!";
						break;
					case 3:
						bonus = (int)values["will"]["bonus"] + (int)values["will"]["penalty"];
						message = c.Name + " makes a will check!";
						break;
				}
				embed.WithThumbnailUrl(c.ImageUrl);
			}

			var result = Roller.Roll("d20 + " + bonus + arguments);

			embed.WithTitle(message)
				.WithDescription(result.ParseResult() + "\nTotal = `" + result.Value + "`")
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

		[Command("Ability"), Alias("A","AbilityCheck")]
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

			for (int i = 0; i < args.Length; i++)
			{
				if (!int.TryParse(args[i], out int a) && args[i].ToLower() != "-c" && args[i].ToLower() != "-f")
				{
					args[i] = " ";
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
				.WithDescription(result.ParseResult() + "\nTotal = `" + result.Value + "`")
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

		[Command("Strike"), Alias("S", "Strikes")]
		public async Task Attack([Remainder]string args = "")
		{
			Character c;
			args = args.ToLower();

			if (args.Contains("-c"))
			{
				c = GetCompanion();
				args = args.Replace("-c", "").Trim();
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
			if (args.NullorEmpty())
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
					if (!((string)s["actions"]).NullorEmpty())
					{
						if (Icons.Actions.TryGetValue((string)s["actions"], out string ic))
						{
							act = ic;
						}
						else act = "[" + s["actions"] + "]";
					}
					sb.AppendLine(Icons.Strike[(string)s["attack"]] + " " + (((string)s["name"]).NullorEmpty() ? "Unnamed Strike" : (string)s["name"]) + " " + act);
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
				
				string[] Bonuses = new string[0];

				if (BonusRegex.IsMatch(args))
				{
					Bonuses = BonusRegex.Matches(args).Select(x => x.Value).ToArray();
					foreach (var b in Bonuses)
					{
						args = args.Replace(b, "");
					}
					args = args.Trim();
				}

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
							  where ((string)sk["name"]).ToLower().StartsWith(args)
							  orderby sk["name"]
							  select sk;

				if (strikes.Count() == 0)
				{
					await ReplyAsync("You have no strikes whose name starts with '" + args + "'.");
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
				string penalties = "";
				string damagebonus = "";
				string dmgtype = "Untyped";

				if ((string)s["attack"] == "spell")
				{
					dmgtype = (string)s["overridedamage"] ?? "Magic";
					JToken cl = null;

					var classes = await SheetService.Get(c, "classes");
					var traditions = await SheetService.Get(c, "traditions");

					classes.Merge(traditions);
					if (classes == null || classes.Count == 0)
					{
						await ReplyAsync("You don't seem to have a class. Without one you can't make spell attacks.");
						return;
					}
					if (((string)s["class"]).NullorEmpty())
					{
						cl = classes.FirstOrDefault();
					}
					else
					{
						cl = classes.First(x => (string)x["id"] == (string)s["class"]);
					}

					

					if (!values.ContainsKey(((string)cl["name"]).ToLower()))
					{
						await ReplyAsync("Sorry! It seems we cannot retrieve the data for this strike! This issue is outside of the bot's control and will likely be solved later.");
						return;
					}

					hit = (string)values[((string)cl["name"]).ToLower()]["bonus"];

					int dc = int.Parse(hit ?? "0") + 10;

					dmg = (string)s["extradamage"];

					if (!dmg.NullorEmpty() && AttributeRegex.IsMatch(dmg))
					{
						dmg = await ParseValues(dmg, c, values);
					}

					string summary = "";

					var result = Roller.Roll("d20 + " + hit +(Bonuses.Length >0?string.Join(" ",Bonuses):""));

					summary += "**Spell Attack roll**: " + result.ParseResult() + " = `" + result.Value + "`";

					if (!((string)s["spell"]).NullorEmpty())
					{
						var spells = await SheetService.Get(c, "spells");
						var sp = spells.First(x => (string)x["id"] == (string)s["spell"]);
						if (!((string)sp["body"]).NullorEmpty())
						{
							embed.AddField("Spell description", sp["body"]);
						}
						if (!((string)sp["savingthrow"]).NullorEmpty())
						{
							summary += "\n**Saving Throw**: " + sp["savingthrow"]+ " (DC: `" + dc + "`)";
						}
						embed.WithTitle(c.Name + " casts " + (sp["name"] ?? "Unnamed Spell") + "!");
					}
					if (!dmg.NullorEmpty())
					{
						try
						{
							RollResult result2 = Roller.Roll(dmg.ToLower() + damagebonus.ToLower());

							summary += "\n**" + dmgtype.Uppercase() + " damage**: " + result2.ParseResult() + " = `" + result2.Value + "` ";

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
				else if ((string)s["attack"] == "custom")
				{
					string summary = "";
					if (!((string)s["weaponcustom"]).NullorEmpty())
					{
						embed.WithTitle(c.Name + " strikes with a " + s["weaponcustom"]);
					}
					if (!((string)s["damagetypecustom"]).NullorEmpty())
					{
						dmgtype = (string)s["damagetypecustom"];
					}
					if (!((string)s["traitscustom"]).NullorEmpty())
					{
						string[] traits = ((string)s["traitscustom"]).Split(',');
						foreach (var x in traits)
						{
							summary += "[" + x.ToUpper() + "] ";
						}
						summary += "\n";
					}
					int range = 0;
					if (!((string)s["range"]).NullorEmpty())
					{
						range += int.Parse((string)s["range"]);
					}
					if (!((string)s["modrange"]).NullorEmpty())
					{
						range += int.Parse((string)s["modrange"]);
					}
					summary += "**Range** " + range + "ft.\n";


					if ((string)s["attackcustom"] == "melee" || ((string)s["attack"]).NullorEmpty())
					{
						hit = (string)values["melee " + (string)s["name"]]["bonus"];
						penalties = (string)values["melee " + (string)s["name"]]["penalty"];
					}
					else
					{
						hit = (string)values["ranged " + (string)s["name"]]["bonus"];
						penalties = (string)values["ranged " + (string)s["name"]]["penalty"];
					}

					damagebonus = (string)values["damage " + (string)s["name"]]["value"];

					dmg = (string)values["damagedice " + (string)s["name"]]["value"] + GetDie((int)values["damagedie " + (string)s["name"]]["value"]);

					

					var result = Roller.Roll("d20 + " + hit + (penalties != "0" ? "-" + penalties : "") + (Bonuses.Length > 0 ? string.Join(" ", Bonuses) : ""));

					summary += "**Attack roll**: " + result.ParseResult() + " = `" + result.Value + "`";

					if (!dmg.NullorEmpty())
					{
						try
						{
							RollResult result2 = Roller.Roll(dmg + "+" + damagebonus);
							summary += "\n**" + dmgtype.Uppercase() + " damage**: " + result2.ParseResult() + " = `" + result2.Value + "` ";

							if (!((string)s["extradamage"]).NullorEmpty())
							{
								string extra = (string)s["extradamage"];
								if (AttributeRegex.IsMatch(extra))
								{
									extra = await ParseValues(extra, c, values);
								}
								RollResult result3 = Roller.Roll(extra);
								summary += "\n**" + ((string)s["overridedamage"]).Uppercase() + " damage**: " + result3.ParseResult() + " = `" + result3.Value + "`";
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
					string summary = "";
					if (!((string)s["weapon"]).NullorEmpty())
					{
						var items = await SheetService.Get(c, "items");
						var i = items.First(x => (string)x["id"] == (string)s["weapon"]);
						dmgtype = (string)i["damagetype"] ?? "Untyped";

						if (!((string)i["traits"]).NullorEmpty())
						{
							string[] traits = ((string)i["traits"]).Split(',');
							foreach(var x in traits)
							{
								summary += "[" + x.ToUpper() + "] ";
							}
							summary += "\n";
						}


						int range = 0;
						if (!((string)i["range"]).NullorEmpty() && int.TryParse((string)i["range"], out int r))
						{
							range += r;
						}
						if (!((string)s["modrange"]).NullorEmpty() && int.TryParse((string)s["modrange"], out int r2))
						{
							range += r2;
						}
						summary += "**Range**: " + range + "ft.\n";

						if((string)i["attack"] == "melee")
						{
							hit = (string)values["melee " + (string)s["name"]]["bonus"];
							penalties = (string)values["melee " + (string)s["name"]]["penalty"];
						}
						else if((string)i["attack"] == "ranged")
						{
							hit = (string)values["ranged " + (string)s["name"]]["bonus"];
							penalties = (string)values["ranged " + (string)s["name"]]["penalty"];
						}
					}
					else if ((string)s["attack"] == "melee")
					{
						hit = (string)values["melee " + (string)s["name"]]["bonus"];
						penalties = (string)values["melee " + (string)s["name"]]["penalty"];
					}
					else if((string)s["attack"] == "ranged")
					{
						hit = (string)values["ranged " + (string)s["name"]]["bonus"];
						penalties = (string)values["ranged " + (string)s["name"]]["penalty"];
					}
					damagebonus = (string)values["damage " + (string)s["name"]]["value"];

					dmg = (string)values["damagedice " + (string)s["name"]]["value"] + GetDie((int)values["damagedie " + (string)s["name"]]["value"]);

					

					var result = Roller.Roll("d20 + " + hit + (penalties != "0"? "-"+penalties:"") + (Bonuses.Length > 0 ? string.Join(" ", Bonuses) : ""));

					summary += "**Attack roll**: " + result.ParseResult() + " = `" + result.Value + "`";

					if (!dmg.NullorEmpty())
					{
						try
						{
							RollResult result2 = Roller.Roll(dmg + "+" + damagebonus);
							summary += "\n**" + dmgtype.Uppercase() + " damage**: " + result2.ParseResult() + " = `" + result2.Value + "` ";

							if (!((string)s["extradamage"]).NullorEmpty())
							{
								string extra = (string)s["extradamage"];
								if (AttributeRegex.IsMatch(extra))
								{
									extra = await ParseValues(extra, c, values);
								}
								RollResult result3 = Roller.Roll(extra);
								summary += "\n**" + ((string)s["overridedamage"]).Uppercase() + " damage**: " + result3.ParseResult() + " = `" + result3.Value + "`";
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
