using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using NethysBot.Models;
using System.Text.RegularExpressions;
using LiteDB;
using System.Net;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Discord.Commands;
using NethysBot.Helpers;
using System.Threading.Tasks.Dataflow;
using Discord;
using System.Data;
using System.Runtime.InteropServices;

namespace NethysBot.Services
{
	public class SheetService
	{

		private LiteCollection<Character> collection;
		private HttpClient Client;
		private string Api = "http://character.pf2.tools/api/characters/";
		private SRD SRD;
		public SheetService(LiteDatabase database, SRD _srd)
		{
			Client = new HttpClient();
			collection = database.GetCollection<Character>("Characters");
			SRD = _srd;
		}

		/// <summary>
		/// Fetches a character from the given character.pf2.tools URL and adds it to the database.
		/// </summary>
		/// <param name="url">Url of the character</param>
		/// <returns>Parsed and updated character</returns>
		public async Task<Character> NewCharacter(string url, SocketCommandContext context = null)
		{
			var regex = new Regex(@"(\w*\W*)?\?(\w*)\-?");

			if (!regex.IsMatch(url))
			{
				throw new Exception("This is not a valid character.pf2.tools url. Makesure you copy the full url!");
			}

			var match = regex.Match(url);

			var id = match.Groups[2].Value;

			if (!collection.Exists(x => x.RemoteId == id))
			{
				var c = new Character()
				{
					RemoteId = id
				};

				if (context != null)
				{
					c.Owners.Add(context.User.Id);
				}

				collection.Insert(c);
			}

			Character character = collection.FindOne(x => x.RemoteId == id);

			HttpResponseMessage response = await Client.GetAsync(Api + id);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);
			
			if (json.ContainsKey("error"))
			{
				throw new Exception("This is not a valid character ID or the character is not set to public.");
			}

			character.SheetCache = json["data"].ToString();

			character.ValuesCache = json["values"].ToString();

			character.LastUpdated = DateTime.Now;

			character.Type = Enum.Parse<SheetType>(((string)json["data"]["type"]).Uppercase());

			if (context != null) 
			{
				if (!character.Owners.Contains(context.User.Id)) character.Owners.Add(context.User.Id);
			}

			var data = JObject.Parse(character.SheetCache);

			character.Name = data.ContainsKey("name") ? (string)data["name"] : "Unnamed Character";
			
			if (data.ContainsKey("customnotes") && data["customnotes"].HasValues)
			{
				var notes = from n in data["customnotes"]
							where (string)n["uiid"] == "character"
							select n;
				foreach(var n in notes)
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.ImageUrl = (string)n["body"];
						break;
					}
				}
			}

			collection.Update(character);
			collection.EnsureIndex(x => x.Name.ToUpper());
			collection.EnsureIndex(x => x.RemoteId);
			collection.EnsureIndex(x => x.Type);
			collection.EnsureIndex(x => x.Owners);

			return collection.FindOne(x => x.RemoteId == id);
		}

		/// <summary>
		/// Syncs a character with the remote version
		/// </summary>
		/// <param name="Character"></param>
		/// <returns>The Synced character</returns>
		public async Task<Character> SyncCharacter(Character character)
		{
			HttpResponseMessage response = await Client.GetAsync(Api + character.RemoteId);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			character.SheetCache = json["data"].ToString();

			character.ValuesCache = json["values"].ToString();

			character.LastUpdated = DateTime.Now;

			character.Type = Enum.Parse<SheetType>((string)json["data"]["type"]);

			var data = JObject.Parse(character.SheetCache);

			character.Name = data.ContainsKey("name") ? (string)data["name"] : "Unnamed Character";

			if (data.ContainsKey("customnotes") && data["customnotes"].HasValues)
			{
				var notes = from n in data["customnotes"]
							where (string)n["uiid"] == "character"
							select n;
				foreach (var n in notes)
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.ImageUrl = (string)n["body"];
						break;
					}
				}
			}


			collection.Update(character);

			return character;
		}

		/// <summary>
		/// Syncs a character's calculated values with the remote version
		/// </summary>
		/// <param name="Character"></param>
		/// <returns>The Synced character</returns>
		public async Task<Character> SyncValues(Character Character)
		{
			HttpResponseMessage response = await Client.GetAsync(Api+ Character.RemoteId + "/values");

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			Character.ValuesCache = json["data"].ToString();

			collection.Update(Character);

			return Character;
		}
		/// <summary>
		/// Gets the character's full sheet, syncs the sheet when you do so.
		/// </summary>
		/// <param name="character"> The character </param>
		/// <returns> the Embed </returns>
		public async Task<Embed> GetSheet(Character c)
		{
			string url = "https://character.pf2.tools/?" + c.RemoteId;

			// c = await SyncCharacter(c);

			var full = JObject.Parse(c.SheetCache);
			var values = JObject.Parse(c.ValuesCache);

			var embed = new EmbedBuilder()
				.WithTitle(c.Name)
				.WithUrl(url)
				.WithThumbnailUrl(c.ImageUrl);
			
			var sb = new StringBuilder();
			sb.AppendLine("Lv" + (string)full["level"] + (((string)full["ancestry"]).NullorEmpty()? "" : " " + (string)full["ancestry"]) + (full["classes"].HasValues ? " "+(string)full["classes"][0]["name"]:" Adventurer"));
			sb.AppendLine(Icons.Sheet["hp"] + " HP `" + ((int)values["hp"]["value"] - (int)(full["damage"]??0))+ "/" + values["hp"]["value"]+"`");
			sb.AppendLine(Icons.Sheet["ac"] + " AC `" + values["armor class"]["value"]+"`");
			sb.AppendLine(Icons.Sheet["per"] + " Perception `" + ((int)values["perception"]["bonus"]).ToModifierString() + "` (DC " + values["perception"]["value"] + ")");

			embed.WithDescription(sb.ToString());
			sb.Clear();

			if (full["classes"].HasValues)
			{
				var classes = from cl in full["classes"]
							  select cl;
				foreach (var cl in classes)
				{
					string ability = ((string)cl["ability"]).NullorEmpty() ? "" : Icons.Scores[(string)cl["ability"]] + " ";
					sb.AppendLine(ability + (string)cl["name"] + " " + Icons.Proficiency[(string)cl["proficiency"]] + " (Class DC: " + ((int)values[((string)cl["name"]).ToLower()]["bonus"] + 10) + ")");
				}
				embed.AddField("Classes", sb.ToString());
				sb.Clear();
			}

			sb.AppendLine(Icons.Scores["strength"] + " `" + (((int)values["strength"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["intelligence"] + " `" + ((int)values["intelligence"]["value"]).PrintModifier() + "`");
			sb.AppendLine(Icons.Scores["dexterity"] + " `" + (((int)values["dexterity"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["wisdom"] + " `" + ((int)values["wisdom"]["value"]).PrintModifier() + "`");
			sb.AppendLine(Icons.Scores["constitution"] + " `" + (((int)values["constitution"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["charisma"] + " `" + ((int)values["charisma"]["value"]).PrintModifier() + "`");

			embed.AddField("Abilities", sb.ToString(),true);
			sb.Clear();

			sb.AppendLine(Icons.Sheet["fort"] + " `" + ((int)values["fortitude"]["bonus"]).ToModifierString()+"`");
			sb.AppendLine(Icons.Sheet["ref"] + " `" + ((int)values["reflex"]["bonus"]).ToModifierString()+"`");
			sb.AppendLine(Icons.Sheet["will"] + " `" + ((int)values["will"]["bonus"]).ToModifierString()+"`");

			embed.AddField("Defenses", sb.ToString(), true);
			sb.Clear();

			var conditions = from con in full["conditions"]
							 where con["value"] != null
							 select con;
			if (conditions.Count() > 0)
			{
				foreach (var con in conditions)
				{
					sb.AppendLine(con["name"] + " " + (int)con["value"]);
				}
				embed.AddField("Conditions", sb.ToString(), true);

				sb.Clear();
			}

			sb.Append(Icons.Sheet["land"] + " " + (full["speeds"][0]["value"] != null ? full["speeds"][0]["value"] + " ft" : "—") + " " );
			sb.Append(Icons.Sheet["swim"] + " " + (full["speeds"][1]["value"] != null ? full["speeds"][1]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["climb"] + " " + (full["speeds"][2]["value"] != null ? full["speeds"][2]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["fly"] + " " + (full["speeds"][3]["value"] != null ? full["speeds"][3]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["burrow"] + " " + (full["speeds"][4]["value"] != null ? full["speeds"][4]["value"] + " ft" : "—") + " ");

			embed.AddField("Speeds", sb.ToString());
			sb.Clear();

			var skills = full["skills"].OrderBy(x=>x["name"]).ToList();
			int sktotal = skills.Count();
			int index = 0;

			double cycles = Math.Ceiling(sktotal / 6.0);
			
			for (int i = 0; i < cycles; i++)
			{
				for (int j = 0; j < 6 && index < sktotal; j++)
				{
					if (!((string)skills[index]["lore"]).NullorEmpty())
					{
						int bonus = int.Parse((string)values[((string)skills[index]["lore"]).ToLower()]["value"] ?? "0");
						sb.AppendLine(Icons.Scores[(string)skills[index]["ability"]] + " " + ((string)skills[index]["lore"]).Substring(0,Math.Min(9, ((string)skills[index]["lore"]).Length)).Uppercase() + " " + Icons.Proficiency[(string)skills[index]["proficiency"]] + " " + bonus.ToModifierString());
					}
					else
					{
						int bonus = int.Parse((string)values[((string)skills[index]["name"]).ToLower()]["value"] ?? "0");
						sb.AppendLine(Icons.Scores[(string)skills[index]["ability"]] + " " + ((string)skills[index]["name"]).Substring(0, Math.Min(9, ((string)skills[index]["name"]).Length)).Uppercase() + " " + Icons.Proficiency[(string)skills[index]["proficiency"]] + " " + bonus.ToModifierString());
					}
					index++;
				}
				embed.AddField("Skills", sb.ToString(), true);
				sb.Clear();
			}

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			embed.WithFooter("Last synced: " + c.LastUpdated.ToString());

			return embed.Build();
		}

		#region Feats
		public async Task<Embed> GetFeat(Character c, string name)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/feats");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var feats = from f in json["data"]
						where ((string)f["name"]).ToLower().StartsWith(name.ToLower())
						orderby (string)f["name"]
						select f;

			if (feats.Count() <= 0) return null;

			var feat = feats.First();

			var embed = SRD.EmbedFeat(feat);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (feat["src"] != null)
			{
				embed.WithFooter((string)feat["src"]);
				embed.WithUrl((string)feat["src"]);
			}

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			return embed.Build();
		}	
		public async Task<Embed> GetAllFeats(Character c)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/feats");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var feats = json["data"].Children();

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Feats")
				.WithThumbnailUrl(c.ImageUrl);
			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			var sb = new StringBuilder();

			foreach(var f in feats)
			{
				sb.AppendLine("• "+(f["name"]??"Unnamed Feat") + " (" + ((string)f["subtype"] ?? "Feat").Uppercase() + " " + f["level"]+")");
			}

			embed.WithDescription(sb.ToString());

			return embed.Build();
		}
		#endregion

		#region Features
		public async Task<Embed> GetFeature(Character c, string name)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/features");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var features = from fs in json["data"]
						where ((string)fs["name"]).ToLower().StartsWith(name.ToLower())
						orderby (string)fs["name"]
						select fs;

			if (features.Count() <= 0) return null;

			var f = features.First();

			var embed = SRD.EmbedFeature(f);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			return embed.Build();
		}	
		public async Task<Embed> GetAllFeatures(Character c)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/features");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var feats = json["data"].Children();

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Features")
				.WithThumbnailUrl(c.ImageUrl);
			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			var sb = new StringBuilder();

			foreach (var f in feats)
			{
				sb.AppendLine("• " + (f["name"] ?? "Unnamed Feature") + " (" + ((string)f["subtype"] ?? "Feature").Uppercase() + " " + f["level"]+")");
			}

			embed.WithDescription(sb.ToString());

			return embed.Build();
		}
		#endregion

		#region Actions
		public async Task<Embed> GetAction(Character c, string name)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/activities");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var features = from fs in json["data"]
						   where ((string)fs["name"]).ToLower().StartsWith(name.ToLower())
						   orderby fs["name"]
						   select fs;

			if (features.Count() <= 0) return null;

			var f = features.First();

			var embed = SRD.EmbedAction(f);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}
			
			return embed.Build();
		}
		public async Task<Embed> GetAllActions(Character c)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/activities");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var feats = json["data"].Children();

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Actions")
				.WithThumbnailUrl(c.ImageUrl);
			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			var sb = new StringBuilder();

			foreach (var f in feats)
			{
				var act = (string)f["actions"];

				if (act != null)
				{
					if (Icons.Actions.TryGetValue(act, out string icon))
					{
						act = icon;
					}
				}
				sb.AppendLine("• " + (f["name"] ?? "Unnamed Activity") + " " + act??"");
			}

			embed.WithDescription(sb.ToString());
			
			return embed.Build();
		}
		#endregion

		#region Items
		public async Task<Embed> GetItem(Character c, string Name)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/items");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var items = from it in json["data"]
						where it["name"] != null && ((string)it["name"]).ToLower().StartsWith(Name.ToLower())
						orderby (string)it["name"]
						select it;
			if (items.Count() == 0) return null;

			var i = items.FirstOrDefault();

			var embed = SRD.EmbedItem(i);

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}
			return embed.Build();
		}	
		public async Task<Embed> Inventory(Character c)
		{
			var request = await Client.GetAsync(Api + c.RemoteId+"/items");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var full = JObject.Parse(c.SheetCache);

			var pp = full["pp"] ?? 0;
			var gp = full["gp"] ?? 0;
			var sp = full["sp"] ?? 0;
			var cp = full["cp"] ?? 0;

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Inventory")
				.AddField("Currency","CP "+cp+" | SP "+ sp+" | GP "+gp+" | PP "+pp)
				.WithThumbnailUrl(c.ImageUrl);

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			if (json["data"]!=null && json["data"].HasValues)
			{
				var items = json["data"]["items"].Children();
				var sb = new StringBuilder();
				foreach (var i in items)
				{
					sb.AppendLine("• " + (i["name"] ?? "Unnamed Item") + " [" + (i["type"] ?? "Item") + " " + (i["level"]??1) + "] x"+(i["quantity"]??1));
				}

				embed.AddField("Items", sb.ToString());
			}

			return embed.Build();
		}
		#endregion

		#region Spells
		public async Task<Embed> GetSpell(Character c, string Name)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/spells");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var spells = from it in json["data"]
						 where it["name"] != null && ((string)it["name"]).ToLower().StartsWith(Name.ToLower())
						 orderby (string)it["name"]
						 select it;
			if (spells.Count() == 0) return null;

			var s = spells.FirstOrDefault();

			var embed = SRD.EmbedSpell(s);

			return embed.Build();
		}
		public async Task<Embed> GetAllSpells(Character c)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/spells");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var jsonspells = JObject.Parse(responsebody);
			var json = JObject.Parse(c.SheetCache);
			var values = JObject.Parse(c.ValuesCache);

			var classes = from cls in json["classes"]
						  where cls["tradition"] != null
						  select cls;

			if (classes.Count() == 0) return null;

			var spells = jsonspells["data"].Children();

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Spells")
				.WithThumbnailUrl(c.ImageUrl);

			var sb = new StringBuilder();

			foreach (var cl in classes)
			{
				string ability = ((string)cl["ability"]).NullorEmpty() ? "" : Icons.Scores[(string)cl["ability"]] + " ";

				sb.AppendLine(ability + ((string)cl["tradition"]).Uppercase()+ " " + Icons.Proficiency[(string)cl["proficiency"]]);
				sb.AppendLine("Spell Attack `" + ((int)values[((string)cl["name"]).ToLower()]["bonus"]).ToModifierString() + "`");
				sb.AppendLine("DC `" + ((int)values[((string)cl["name"]).ToLower()]["value"]) + "`");

				embed.AddField(((string)cl["name"]??"Unnamed class"), sb.ToString(),true);
				sb.Clear();
			}

			var focus = from sp in spells where (string)sp["type"] == "focus" select sp;

			if (focus.Count() > 0)
			{
				foreach (var s in focus)
				{
					var act = (string)s["actions"];

					if (act != null)
					{
						if (Icons.Actions.TryGetValue(act, out string icon))
						{
							act = icon;
						}
					}
					sb.Append((s["name"] ?? "Unnamed Spell") + " " + act+", ");
				}
				string f = ((string)json["focusmax"]).NullorEmpty() ? "" : " [" + (json["focus"] ?? 0) + "/" + json["focusmax"]+"]";
				embed.AddField("Focus"+f, sb.ToString().TrimEnd().Substring(0,sb.Length-2));
				sb.Clear();
			}

			var rituals = from sp in spells where (string)sp["type"] == "ritual" select sp;

			if (rituals.Count() > 0)
			{
				foreach (var s in focus)
				{
					var act = (string)s["actions"];

					if (act != null)
					{
						if (Icons.Actions.TryGetValue(act, out string icon))
						{
							act = icon;
						}
					}
					sb.Append((s["name"] ?? "Unnamed Spell") + " " + act + ", ");
				}
				string f = ((string)json["focusmax"]).NullorEmpty() ? "" : " [" + (json["focus"] ?? 0) + "/" + json["focusmax"] + "]";
				embed.AddField("Rituals" + f, sb.ToString().TrimEnd().Substring(0, sb.Length - 2));
				sb.Clear();
			}

			var lv0 = from sp in spells where sp["cantrip"] != null select sp;

			if(lv0.Count() > 0)
			{
				int slots = (from cl in json["classes"]
							 where cl["spell0"] != null
							 select (int)cl["spell0"]).Sum();
				foreach (var s in lv0)
				{
					var casts = s["cast"]!=null? string.Join(", ",s["cast"]) : "-";
					var act = (string)s["actions"];

					if (act != null)
					{
						if (Icons.Actions.TryGetValue(act, out string icon))
						{
							act = icon;
						}
					}
					sb.Append((s["name"] ?? "Unnamed Cantrip") + " " + act+", ");
				}

				embed.AddField("Cantrips ["+slots+"]", sb.ToString().TrimEnd().Substring(0, sb.Length-2));
				sb.Clear();
			}

			for (int i = 1; i < 9; i++)
			{
				var sps = from sp in spells where sp["level"]!=null &&
						  (string)sp["type"]=="spell" && 
						  (int)sp["level"] == i 
						  select sp;
				if (sps.Count() == 0) continue;

				int slots = (from cl in json["classes"]
							 where cl["spell" + i] != null
							 select (int)cl["spell" + i]).Sum();


				foreach (var s in sps)
				{
					var casts = s["cast"] != null ? string.Join(", ", s["cast"]) : "-";
					var act = (string)s["actions"];

					if (act != null)
					{
						if (Icons.Actions.TryGetValue(act, out string icon))
						{
							act = icon;
						}
					}
					sb.Append((s["name"] ?? "Unnamed Spell") + " " + act+", ");
				}
				embed.AddField(i.ToPlacement() + " Level ["+slots+"]", sb.ToString().TrimEnd().Substring(0, sb.Length-2));
				sb.Clear();
			}

			if (c.Color == null)
			{
				Random randonGen = new Random();
				Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
				randonGen.Next(255));
				embed.WithColor(randomColor);
			}
			else
			{
				embed.WithColor(c.Color[0], c.Color[1], c.Color[2]);
			}

			return embed.Build();
		}
		#endregion

	}
}
