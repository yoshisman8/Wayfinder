using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Linq;
using Discord;
using NethysBot.Helpers;

namespace NethysBot.Services
{
	public class SRD
	{
		private string RemoteUrl = "http://character.pf2.tools/assets/json/all.json";
		
		public SRD()
		{
			Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "data"));

			if(!File.Exists(Path.Combine(Directory.GetCurrentDirectory(),"data" , "all.json")))
			{
				WebClient Client = new WebClient();
				
				Client.DownloadFile(RemoteUrl, Path.Combine(Directory.GetCurrentDirectory(),"data","all.json"));
			}
			string raw = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),"data", "all.json"));

			var tokens = JArray.Parse(raw);

			//var tokens = json[0];
			
			Feats = from f in tokens where (string)f["type"] == "feat" select f;
			Actions = from a in tokens where (string)a["type"] == "action" select a;
			Items = from I in tokens
					where (string)I["type"] == "item" ||
						(string)I["type"] == "armor" ||
						(string)I["type"] == "shield" ||
						(string)I["type"] == "weapon"
					 select I;
			Features = from f in tokens where (string)f["type"] == "feature" select f;
			Traits = from t in tokens where (string)t["type"] == "trait" select t;
			Spells = from s in tokens
					 where (string)s["type"] == "spell" ||
						 (string)s["type"] == "ritual" ||
						 (string)s["type"] == "focus"
					 select s;
			Backgrounds = from b in tokens where (string)b["type"] == "background" select b;
		}

		public IEnumerable<JToken> Feats { get; set; }
		public IEnumerable<JToken> Actions { get; set; }
		public IEnumerable<JToken> Items { get; set; } // Includes types "item", "armor", "shield" and "weapon"
		public IEnumerable<JToken> Features { get; set; }
		public IEnumerable<JToken> Spells { get; set; } // Contains types "spell", "ritual" and "focus"
		public IEnumerable<JToken> Traits { get; set; }
		public IEnumerable<JToken> Backgrounds { get; set; }
		public enum Types { Actions, Feats, Features, Items, Spells, Traits, Backgrounds }

		public EmbedBuilder EmbedFeat(JToken feat)
		{
			string body = (string)feat["body"] ?? "No Description";

			body = body.Replace("(a)", Icons.Actions["1"]).Replace("(aa)", Icons.Actions["2"])
				.Replace("(aaa)", Icons.Actions["3"])
				.Replace("(f)", Icons.Actions["0"])
				.Replace("(r)", Icons.Actions["r"]);

			var embed = new EmbedBuilder()
				.WithTitle((string)feat["name"] ?? "Unammed Feat")
				.AddField("Traits", feat["traits"] ?? "N/A", true)
				.AddField("Type", ((string)feat["subtype"] ?? "Feat").Uppercase() + " " + feat["level"], true)
				.AddField("Description", body);
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedFeature(JToken f)
		{
			string body = (string)f["body"] ?? "No Description";

			body = body.Replace("(a)", Icons.Actions["1"]).Replace("(aa)", Icons.Actions["2"])
				.Replace("(aaa)", Icons.Actions["3"])
				.Replace("(f)", Icons.Actions["0"])
				.Replace("(r)", Icons.Actions["r"]);

			var embed = new EmbedBuilder()
				.WithTitle((string)f["name"] ?? "Unammed Feature")
				.AddField("Type", ((string)f["type"] ?? "Feature") + " " + f["level"])
				.AddField("Description", body);

			if (f["src"] != null)
			{
				embed.WithFooter((string)f["src"]);
				embed.WithUrl((string)f["src"]);
			}
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedAction(JToken f)
		{
			string body = (string)f["body"] ?? "No Description";

			body = body.Replace("(a)", Icons.Actions["1"]).Replace("(aa)", Icons.Actions["2"])
				.Replace("(aaa)", Icons.Actions["3"])
				.Replace("(f)", Icons.Actions["0"])
				.Replace("(r)", Icons.Actions["r"]);

			var embed = new EmbedBuilder()
				.WithTitle((string)f["name"] ?? "Unammed Activity")
				.AddField("Description", body);

			var act = (string)f["actions"];

			if (act != null)
			{
				if (Icons.Actions.TryGetValue(act, out string icon))
				{
					embed.AddField("Actions", icon);
				}
				else
				{
					embed.AddField("Actions", act);
				}
			}

			if (f["src"] != null)
			{
				embed.WithFooter((string)f["src"]);
				embed.WithUrl((string)f["src"]);
			}
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedItem(JToken i)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)i["name"] ?? "Unammed Item")
				.AddField("Traits", i["traits"] ?? "No Traits", true)
				.AddField("Type", (i["type"] ?? "Item") + " " + (i["level"] ?? 0), true)
				.AddField("Status", Icons.Sheet["hp"] + " HP " + ((int)i["hp"] - (int)(i["damage"] ?? 0)) + "/" + i["hp"] +
					"\n" + Icons.Sheet["ac"] + " Hardness " + (i["hardness"]??0), true)
				.WithDescription("Price: " + (i["price"] ?? 0) + " " + i["priceunit"] + "\n" +
					"Bulk: " + (i["bulk"] ?? 0));
			if ((string)i["type"] == "armor" || (string)i["type"] == "shield")
			{
				embed.AddField("Armor bonus", "**Category**: " + (i["category"] ?? "Uncategorized") +
					"\n**AC bonus**: " + (i["acbonus"] ?? 0) +
					"\n**Maximum Dexterity Bonus**: " + (i["dexcap"] ?? "-") +
					"\n**Armor Check Penalty**: " + (i["checkpenalty"] ?? 0) +
					"\n**Speed Penalty**: " + (i["speedpenalty"] ?? 0) + "ft" +
					"\n**Strength**: " + (i["strength"] ?? 0));
			}
			if ((string)i["type"] == "weapon")
			{
				embed.AddField("Weapon Statistics", "**Group**: " + (i["group"] ?? "-") + "; " +
					"**Category**: " + (i["category"] ?? "Uncategorizes") +
					"\n**Damage**: 1" + (i["damagedie"] ?? "d6") + " " + (i["damagetype"] ?? "Untyped"));
			}



			string body = (string)i["body"] ?? "No Description";

			body = body.Replace("(a)", Icons.Actions["1"]).Replace("(aa)", Icons.Actions["2"])
				.Replace("(aaa)", Icons.Actions["3"])
				.Replace("(f)", Icons.Actions["0"])
				.Replace("(r)", Icons.Actions["r"]);

			embed.AddField("Description", body);

			if (i["src"] != null)
			{
				embed.WithFooter((string)i["src"]);
				embed.WithUrl((string)i["src"]);
			}
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedSpell(JToken s)
		{
			var act = (string)s["actions"];

			if (act != null)
			{
				if (Icons.Actions.TryGetValue(act, out string icon))
				{
					act = icon;
				}
				else
				{
					act = "(" + act + ")";
				}
			}

			string type = s["cantrip"] != null ? "Cantrip" : ((string)s["type"]).Uppercase() + " " + (s["level"] ?? 0);

			string comps = s["cast"] != null ? string.Join(", ", s["cast"]).ToUpper() : "";

			string traditions = s["traditions"] != null ? string.Join(", ", s["traditions"]).Uppercase() : "-";

			var embed = new EmbedBuilder()
				.WithTitle(((string)s["name"] ?? "Unammed Spell") + " (" + type + ")")
				.AddField("Traits", s["traits"] ?? "No Traits", true)
				.AddField("Cast", act + " " + comps, true)
				.AddField("Traditions", traditions, true);

			var sb = new StringBuilder();
			if (s["range"] != null) sb.AppendLine("**Range** " + s["range"]);
			// if (s["area"] != null) sb.AppendLine("**Area** " + s["area"]);
			// if (s["targets"] != null) sb.AppendLine("**Targets** " + s["targets"]);
			if (s["savingthrow"] != null) sb.AppendLine("**Saving Throw**" + ((string)s["savingthrow"]).Uppercase() + (s["basic"] != null ? " (Basic)" : ""));


			sb.AppendLine((string)s["body"] ?? "No Description");
			embed.AddField("Description", sb.ToString());

			if (s["src"] != null)
			{
				embed.WithFooter((string)s["src"]);
				embed.WithUrl((string)s["src"]);
			}
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
	}
}
