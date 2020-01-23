using Dice;
using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
						if (values.TryGetValue(m.Groups[1].ToString().ToLower(),out JToken v))
						{
							Expression = Expression.Replace(m.Value, (string)v["bonus"] ?? "0");
						}
					}
				}
			}


			var results = Roller.Roll(Expression);
			decimal total = results.Value;

			var embed = new EmbedBuilder()
				.WithTitle(Context.User.Username + " rolled some dice.")
				.WithDescription(ParseResult(results) + "\nTotal = `" + total + "`");
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			await ReplyAsync("", embed.Build());
		}


		private string ParseResult (RollResult result)
		{
			var sb = new StringBuilder();

			foreach (var dice in result.Values)
			{
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
				sb.Append(" + ");
			}

			return sb.ToString().Trim().Substring(0, sb.Length - 2);
		}
	}
}
