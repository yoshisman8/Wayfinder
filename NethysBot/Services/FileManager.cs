using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Linq;
using Discord;
using NethysBot.Helpers;
using System.Security.Cryptography;
using Antlr4.Runtime.Dfa;

namespace NethysBot.Services
{
	public class FileManager
	{
		private string RemoteUrl = "http://character.pf2.tools/assets/json/all.json";
		
		public FileManager()
		{
			Reload();
		}
		public void Reload()
		{
			Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "data"));

			if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data", "all.json")))
			{
				WebClient Client = new WebClient();

				Client.DownloadFile(RemoteUrl, Path.Combine(Directory.GetCurrentDirectory(), "data", "all.json"));
			}
			string raw = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "data", "all.json"));

			var tokens = JArray.Parse(raw);

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

			if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data", "help.json")))
			{
				Commands = JArray.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "data", "help.json")));

				Categories = Commands.Select(x => (string)x["Category"]).Distinct();
			}
		}
		public IEnumerable<JToken> Feats { get; set; }
		public IEnumerable<JToken> Actions { get; set; }
		public IEnumerable<JToken> Items { get; set; } // Includes types "item", "armor", "shield" and "weapon"
		public IEnumerable<JToken> Features { get; set; }
		public IEnumerable<JToken> Spells { get; set; } // Contains types "spell", "ritual" and "focus"
		public IEnumerable<JToken> Traits { get; set; }
		public IEnumerable<JToken> Backgrounds { get; set; }
		public enum Types { Actions, Feats, Features, Items, Spells, Traits, Backgrounds }

		public IEnumerable<string> Categories { get; set; }
		public JArray Commands { get; set; }


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
				.AddField("Type", ((string)feat["subtype"] ?? "Feat").Uppercase() + " " + feat["level"], true);

			var sb = new StringBuilder();
			sb.AppendLine((string)feat["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int i = 0; i < segments.Length; i++)
				{
					embed.AddField("Description (" + (i + 1) + "/" + (segments.Length) + ")", segments[i]);
				}
			}
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
				.AddField("Type", ((string)f["type"] ?? "Feature") + " " + f["level"]);

			var sb = new StringBuilder();
			sb.AppendLine((string)f["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int i = 0; i < segments.Length; i++)
				{
					embed.AddField("Description (" + (i + 1) + "/" + (segments.Length) + ")", segments[i]);
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
		public EmbedBuilder EmbedAction(JToken f)
		{
			string body = (string)f["body"] ?? "No Description";

			body = body.Replace("(a)", Icons.Actions["1"]).Replace("(aa)", Icons.Actions["2"])
				.Replace("(aaa)", Icons.Actions["3"])
				.Replace("(f)", Icons.Actions["0"])
				.Replace("(r)", Icons.Actions["r"]);

			var embed = new EmbedBuilder()
				.WithTitle((string)f["name"] ?? "Unammed Activity");

			var sb = new StringBuilder();
			sb.AppendLine((string)f["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int i = 0; i < segments.Length; i++)
				{
					embed.AddField("Description (" + (i + 1) + "/" + (segments.Length) + ")", segments[i]);
				}
			}


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
				.WithTitle(((string)i["name"]).NullorEmpty() ? "Unammed Item" : (string)i["name"]);

			embed.AddField("Traits", ((string)i["traits"]).NullorEmpty() ? "No traits" : (string)i["traits"], true);

			embed.AddField("Type", (((string)i["type"]).NullorEmpty() ? "Item" : (string)i["type"]) + " " + (i["level"] ?? 0), true);

			embed.AddField("Status", Icons.Sheet["hp"] + " HP " + ((int)i["hpmax"] - (int)(i["damage"] ?? 0)) + "/" + i["hpmax"] +
				"\n" + Icons.Sheet["ac"] + " Hardness " + (i["hardness"] ?? 0), true);

			embed.WithDescription("Price: " + (i["price"] ?? 0) + " " + i["priceunit"] + "\n" +
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

			var sb = new StringBuilder();
			sb.AppendLine(body ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}

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

			string comps;
			if(s["cast"].Type == JTokenType.Array)
			{
				comps = s["cast"] != null ? string.Join(", ", s["cast"]).ToUpper() : "";
			}
			else
			{
				comps = (string)s["cast"];
			}

			string traditions;
			if (s["traditions"]!= null && s["traditions"].Type == JTokenType.Array)
			{
				traditions = s["traditions"] != null ? string.Join(", ", s["traditions"]).Uppercase() : "-";
			}
			else
			{
				traditions = (string)s["traditions"];
			}

			var embed = new EmbedBuilder()
				.WithTitle(((string)s["name"] ?? "Unammed Spell") + " (" + type + ")")
				.AddField("Traits", s["traits"] ?? "No Traits", true)
				.AddField("Cast", act + " " + comps, true);
			if (!traditions.NullorEmpty()) embed.AddField("Traditions", traditions, true);
			var sb = new StringBuilder();
			if (s["range"] != null) sb.AppendLine("**Range** " + s["range"]);
			// if (s["area"] != null) sb.AppendLine("**Area** " + s["area"]);
			// if (s["targets"] != null) sb.AppendLine("**Targets** " + s["targets"]);
			if (s["savingthrow"] != null) sb.AppendLine("**Saving Throw** " + ((string)s["savingthrow"]).Uppercase() + (s["basic"] != null ? " (Basic)" : ""));
			if (s["cost"] != null) embed.AddField("Cost ", (string)s["cost"],true);
			if (s["primarycheck"] != null) embed.AddField("Primary Check ", (string)s["primarycheck"], true);

			sb.AppendLine((string)s["body"] ?? "No Description");

			if(sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for(int i = 0; i < segments.Length; i++)
				{
					embed.AddField("Description (" + (i + 1) + "/" + (segments.Length) + ")", segments[i]);
				}
			}

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
		public EmbedBuilder EmbedTrait(JToken t)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)t["name"])
				.WithUrl((string)t["src"]);

			var sb = new StringBuilder();
			sb.AppendLine((string)t["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedItemSRD(JToken i)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)i["name"] +" [Item" +((string)i["level"]??"0")+"]");
			if (i["traits"] != null) embed.AddField("Traits", ((string)i["traits"]).NullorEmpty()?" - ":(string)i["traits"],true);
			if (i["price"] != null) embed.AddField("Price", (((string)i["price"]).NullorEmpty() ? " - " : (string)i["price"]) + (((string)i["priceunit"]).NullorEmpty() ? " - " : (string)i["priceunit"]), true);
			if (i["bulk"] != null) embed.AddField("Bulk", ((string)i["bulk"]).NullorEmpty() ? " - " : (string)i["bulk"], true);
			if (i["hands"] != null) embed.AddField("Hands", ((string)i["hands"]).NullorEmpty() ? " - " : (string)i["hands"], true);


			var sb = new StringBuilder();
			sb.AppendLine((string)i["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedWeapon(JToken w)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)w["name"]);
			if (w["group"] != null) embed.AddField("Group", (string)w["group"], true);
			if (w["traits"] != null) embed.AddField("Traits", (string)w["traits"], true);
			if (w["price"] != null) embed.AddField("Price", (string)w["price"] + (string)w["priceunit"], true);
			if (w["bulk"] != null) embed.AddField("Bulk", (string)w["bulk"], true);
			if (w["hands"] != null) embed.AddField("Hands", (string)w["hands"], true);
			if (w["damagedie"] != null && w["damagetype"] != null) embed.AddField("Damage", "1" + (string)w["damagedie"] + " " + w["damagetype"], true);
			if (!((string)w["damage"]).NullorEmpty()) embed.AddField("Damage", w["damage"],true);

			var sb = new StringBuilder();
			sb.AppendLine((string)w["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedArmor(JToken a)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)a["name"]);
			if (a["category"] != null) embed.AddField("Category", (string)a["category"], true);
			if (a["traits"] != null) embed.AddField("Traits", (string)a["traits"], true);
			if (a["price"] != null) embed.AddField("Price", (string)a["price"] + (string)a["priceunit"], true);
			if (a["bulk"] != null) embed.AddField("Bulk", (string)a["bulk"], true);
			if (a["acbonus"] != null) embed.AddField("Armor Bonus", (string)a["acbonus"], true);
			if (a["dexcap"] != null) embed.AddField("Maximum Dexterity Bonus", a["dexcap"], true);
			if (a["checkpenalty"] != null) embed.AddField("Armor Check Penalty", (string)a["checkpenalty"],true);
			if (a["strength"] != null) embed.AddField("Strength Requirement", a["strength"], true);

			var sb = new StringBuilder();
			sb.AppendLine((string)a["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
		public EmbedBuilder EmbedShield(JToken s)
		{
			var embed = new EmbedBuilder()
				.WithTitle((string)s["name"]);
			if (s["traits"] != null) embed.AddField("Traits", (string)s["traits"], true);
			if (s["price"] != null) embed.AddField("Price", (string)s["price"] + (string)s["priceunit"], true);
			if (s["bulk"] != null) embed.AddField("Bulk", (string)s["bulk"], true);
			if (s["acbonus"] != null) embed.AddField("Shield Bonus", (string)s["acbonus"], true);
			if (s["hardness"] != null) embed.AddField("Hardness", s["dexcap"], true);
			if (s["hp"] != null) embed.AddField("Health", s["hp"], true);
			if (s["hp(bt)"] != null) embed.AddField("Broken Threshold", ((string)s["hp(bt)"]).Replace((string)s["hp"],"").Replace("(","").Replace(")",""), true);

			var sb = new StringBuilder();
			sb.AppendLine((string)s["body"] ?? "No Description");

			if (sb.Length <= 1024)
			{
				embed.AddField("Description", sb.ToString());
			}
			else
			{
				var segments = sb.ToString().Split(1000).ToArray();
				for (int d = 0; d < segments.Length; d++)
				{
					embed.AddField("Description (" + (d + 1) + "/" + (segments.Length) + ")", segments[d]);
				}
			}
			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);
			return embed;
		}
	}
}
