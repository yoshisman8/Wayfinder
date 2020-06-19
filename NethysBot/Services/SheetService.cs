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
using System.Threading.Channels;
using Newtonsoft.Json;
using System.Runtime.ExceptionServices;
using System.Xml;

namespace NethysBot.Services
{
	public class SheetService
	{

		private LiteCollection<Character> collection;
		private HttpClient Client;
		private string Api = "http://character.pf2.tools/api/characters/";
		private FileManager FileManager;
		public SheetService(LiteDatabase database, FileManager _srd)
		{
			Client = new HttpClient();
			collection = database.GetCollection<Character>("Characters");
			FileManager = _srd;
		}

		/// <summary>
		/// Fetches a character from the given character.pf2.tools URL and adds it to the database.
		/// </summary>
		/// <param name="url">Url of the character</param>
		/// <returns>Parsed and updated character</returns>
		public async Task<Character> NewCharacter(string url, SocketCommandContext context)
		{
			var regex = new Regex(@"(\w*\W*)?\?(\w*)\-?");

			if (!regex.IsMatch(url))
			{
				throw new Exception("This is not a valid character.pf2.tools url. Makesure you copy the full url!");
			}

			var match = regex.Match(url);

			var id = match.Groups[2].Value;

			Character character = new Character()
			{
				Owner = context.User.Id,
				RemoteId = id
			};

			HttpResponseMessage response = await Client.GetAsync(Api + id);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);
			
			if (json.ContainsKey("error"))
			{
				throw new Exception("This is not a valid character ID or the character is not set to public.");
			}

			character.LastUpdated = DateTime.Now;


			character.Type = Enum.Parse<SheetType>(((string)json["data"]["type"]).Uppercase());

			character.Name = (string)json["data"]["name"] ?? "Unnamed Character";

			var notes = json["data"]["customnotes"];

			if (notes != null && notes.HasValues)
			{
				foreach (var n in notes.Where(x => (string)x["uiid"] == "character"))
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.ImageUrl = (string)n["body"];
						break;
					}
				}
				foreach (var n in notes.Where(x => (string)x["uiid"] == "companions"))
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.FamImg = (string)n["body"];
						break;
					}
				}

			}

			var familiar = json["data"]["familiars"][0];

			if (familiar["name"]!= null)
			{
				character.Familiar = (string)familiar["name"];
			}
			else
			{
				character.Familiar = null;
			}

			collection.Insert(character);
			collection.EnsureIndex("character", "LOWER($.Name)");
			collection.EnsureIndex(x => x.RemoteId);
			collection.EnsureIndex(x => x.Type);
			collection.EnsureIndex(x => x.Owner);

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
	
			character.LastUpdated = DateTime.Now;

			character.Type = Enum.Parse<SheetType>(((string)json["data"]["type"]).Uppercase());

			character.Name = (string)json["data"]["name"]??"Unnamed Character";

			var notes = json["data"]["customnotes"];

			if (notes != null && notes.HasValues)
			{
				foreach (var n in notes.Where(x => (string)x["uiid"] == "character"))
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.ImageUrl = (string)n["body"];
						break;
					}
				}
				foreach (var n in notes.Where(x => (string)x["uiid"] == "companions"))
				{
					if (((string)n["body"]).IsImageUrl())
					{
						character.FamImg = (string)n["body"];
						break;
					}
				}
			}

			var familiar = json["data"]["familiars"][0];

			if (familiar["name"] != null)
			{
				character.Familiar = (string)familiar["name"];
			}
			else
			{
				character.Familiar = null;
			}

			if (json["values"] != null && json["values"].HasValues)
			{
				character.Values = json["values"].ToString();
				character.ValuesLastUpdated = DateTime.Now;
			}

			collection.Update(character);

			return character;
		}
		public async Task<JObject> GetFullSheet(Character c)
		{
			HttpResponseMessage response = await Client.GetAsync(Api + c.RemoteId);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			return (JObject)json["data"];
		}
		public async Task<JObject> GetValues(Character c)
		{
			HttpResponseMessage response = await Client.GetAsync(Api + c.RemoteId + "/values");

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if ((json["data"] == null || !json["data"].HasValues) && c.Values.NullorEmpty()) return null;
			else if((json["data"] == null || !json["data"].HasValues) && !c.Values.NullorEmpty())
			{
				return JObject.Parse(c.Values);
			}
			else
			{
				c.Values = json["data"].ToString();
				c.ValuesLastUpdated = DateTime.Now;

				collection.Update(c);

				return (JObject)json["data"];
			}
		}
		public async Task<JArray> Get(Character c, string endpoint)
		{
			HttpResponseMessage response = await Client.GetAsync(Api + c.RemoteId + "/"+endpoint);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if ((json["data"] == null || !json["data"].HasValues)) return null;

			else return (JArray)json["data"];

		}
		
		/// <summary>
		/// Gets the character's full sheet, syncs the sheet when you do so.
		/// </summary>
		/// <param name="character"> The character </param>
		/// <returns> the Embed </returns>
		public async Task<Embed> GetSheet(Character c, SocketCommandContext context = null)
		{
			string url = "https://character.pf2.tools/?" + c.RemoteId;

			//c = await SyncCharacter(c);
			
			var full = await GetFullSheet(c);

			if (full == null || !full.HasValues)
			{
				return new EmbedBuilder()
					.WithTitle("Error, Character not found.")
					.WithDescription("Seems like we can't fetch this character's data in pf2.tools. This could mean that the site is down or the character has been deleted.")
					.Build();
			}
			var values = await GetValues(c);

			if(values == null || !values.HasValues)
			{
				return new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl(url)
					.WithDescription("Seems like we cannot fetch "+c.Name+"'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.")
					.Build();
			}

			var embed = new EmbedBuilder()
				.WithTitle(c.Name)
				.WithUrl(url)
				.WithThumbnailUrl(c.ImageUrl);

			var sb = new StringBuilder();
			sb.AppendLine("Lv" + (string)full["level"] + (((string)full["ancestry"]).NullorEmpty() ? "" : " " + (string)full["ancestry"]) + (full["classes"].HasValues ? " " + (string)full["classes"][0]["name"] : " Adventurer"));
			sb.AppendLine(Icons.Sheet["hp"] + " HP `" + ((int)(values?["hp"]["value"]??0) - (int)(full["damage"] ?? 0)) + "/" + (values?["hp"]["value"] ?? 0) + "`");
			sb.AppendLine(Icons.Sheet["ac"] + " AC `" + (values?["armor class"]["value"]??"Unknown") + "`");
			sb.AppendLine(Icons.Sheet["per"] + " Perception `" + ((int)(values?["perception"]["bonus"]??0)).ToModifierString() + "` (DC " + (values["perception dc"]["value"]??"Unknown") + ")");

			embed.WithDescription(sb.ToString());
			sb.Clear();

			if (full["classes"].HasValues)
			{
				var classes = from cl in full["classes"]
							  select cl;
				foreach (var cl in classes)
				{
					string ability = ((string)cl["ability"]).NullorEmpty() ? "" : Icons.Scores[(string)cl["ability"]] + " ";
					sb.AppendLine(ability + ((string)cl["name"]).Uppercase() + " " + Icons.Proficiency[(string)cl["proficiency"]] + (c.Type == SheetType.Character?" (Class DC: " + (int)(values?[((string)cl["name"]).ToLower()+" dc"]["value"]??0) + ")":""));
				}
				embed.AddField("Classes", sb.ToString());
				sb.Clear();
			}

			if (values == null)
			{
				sb.Append("`Could not fetch values from pf2.tools. Open your sheet online and change any value to refresh the values.`");
			}
			else
			{
				switch (c.Type)
				{
					case SheetType.Character:
						sb.AppendLine(Icons.Scores["strength"] + " `" + (((int)values["strength"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["intelligence"] + " `" + ((int)values["intelligence"]["value"]).PrintModifier() + "`");
						sb.AppendLine(Icons.Scores["dexterity"] + " `" + (((int)values["dexterity"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["wisdom"] + " `" + ((int)values["wisdom"]["value"]).PrintModifier() + "`");
						sb.AppendLine(Icons.Scores["constitution"] + " `" + (((int)values["constitution"]["value"]).PrintModifier() + "` ").FixLength(4) + Icons.Scores["charisma"] + " `" + ((int)values["charisma"]["value"]).PrintModifier() + "`");
						break;
					case SheetType.Companion:
						sb.AppendLine(Icons.Scores["strength"] + " `" + (((int)(full["scores"][0]["mod"] ?? 0)).ToModifierString() + "` ").FixLength(4) + Icons.Scores["intelligence"] + " `" + ((int)(full["scores"][3]["mod"] ?? 0)).ToModifierString() + "`");
						sb.AppendLine(Icons.Scores["dexterity"] + " `" + (((int)(full["scores"][1]["mod"] ?? 0)).ToModifierString() + "` ").FixLength(4) + Icons.Scores["wisdom"] + " `" + ((int)(full["scores"][4]["mod"] ?? 0)).ToModifierString() + "`");
						sb.AppendLine(Icons.Scores["constitution"] + " `" + (((int)(full["scores"][2]["mod"] ?? 0)).ToModifierString() + "` ").FixLength(4) + Icons.Scores["charisma"] + " `" + ((int)(full["scores"][5]["mod"] ?? 0)).ToModifierString() + "`");
						break;
				}
			}
			embed.AddField("Abilities", sb.ToString(), true);
			sb.Clear();

			sb.AppendLine(Icons.Sheet["fort"] + " `" + ((int)(values?["fortitude"]?["bonus"]??0) - (int)(values["fortitude"]["penalty"]??0)).ToModifierString() + "` (DC: "+values["fortitude dc"]["value"]+")");
			sb.AppendLine(Icons.Sheet["ref"] + " `" + ((int)(values?["reflex"]?["bonus"]??0) - (int)(values["reflex"]["penalty"] ?? 0)).ToModifierString() + "` (DC: " + values["reflex dc"]["value"] + ")");
			sb.AppendLine(Icons.Sheet["will"] + " `" + ((int)(values?["will"]?["bonus"]??0) - (int)(values["will"]["penalty"] ?? 0)).ToModifierString() + "` (DC: " + values["will dc"]["value"] + ")");

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

			sb.Append(Icons.Sheet["land"] + " " + (full["speeds"][0]["value"] != null ? full["speeds"][0]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["swim"] + " " + (full["speeds"][1]["value"] != null ? full["speeds"][1]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["climb"] + " " + (full["speeds"][2]["value"] != null ? full["speeds"][2]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["fly"] + " " + (full["speeds"][3]["value"] != null ? full["speeds"][3]["value"] + " ft" : "—") + " ");
			sb.Append(Icons.Sheet["burrow"] + " " + (full["speeds"][4]["value"] != null ? full["speeds"][4]["value"] + " ft" : "—") + " ");

			embed.AddField("Speeds", sb.ToString());
			sb.Clear();

			var skills = full["skills"].OrderBy(x => x["name"]).ToList();
			int sktotal = skills.Count();
			int index = 0;

			double cycles = Math.Ceiling(sktotal / 6.0);

			for (int i = 0; i < cycles; i++)
			{
				for (int j = 0; j < 6 && index < sktotal; j++)
				{
					if (!((string)skills[index]["lore"]).NullorEmpty())
					{
						int bonus = int.Parse((string)values?[((string)skills[index]["lore"]).ToLower()]["bonus"] ?? "0") - int.Parse((string)values?[((string)skills[index]["lore"]).ToLower()]["penalty"] ?? "0");
						string icon = (string)skills[index]["ability"] ?? "intelligence";
						sb.AppendLine(Icons.Scores[icon] + " " + ((string)skills[index]["lore"]).Substring(0, Math.Min(9, ((string)skills[index]["lore"]).Length)).Uppercase() + " " + Icons.Proficiency[(string)skills[index]["proficiency"]] + " " + bonus.ToModifierString());
					}
					else if(!((string)skills[index]["name"]).NullorEmpty() && !((string)skills[index]["ability"]).NullorEmpty())
					{
						int bonus = int.Parse((string)values?[((string)skills[index]["name"]).ToLower()]["bonus"] ?? "0") - int.Parse((string)values?[((string)skills[index]["name"]).ToLower()]["penalty"] ?? "0");
						sb.AppendLine(Icons.Scores[(string)skills[index]["ability"]] + " " + ((string)skills[index]["name"]).Substring(0, Math.Min(9, ((string)skills[index]["name"]).Length)).Uppercase() + " " + Icons.Proficiency[(string)skills[index]["proficiency"]] + " " + bonus.ToModifierString());
					}
					index++;
				}
				embed.AddField("Skills", sb.ToString(), true);
				sb.Clear();
			}

			Random randonGen = new Random();
			Color randomColor = new Color(randonGen.Next(255), randonGen.Next(255),
			randonGen.Next(255));
			embed.WithColor(randomColor);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			embed.WithFooter(c.ValuesLastUpdated.Outdated() ? "⚠️ Couldn't retrieve updated values. Data might not be accurate" : "Last updated: " +c.LastUpdated.ToString());

			return embed.Build();
		}
		

		#region Feats
		public async Task<Embed> GetFeat(Character c, string name, SocketCommandContext context = null)
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

			var embed = FileManager.EmbedFeat(feat);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (feat["src"] != null)
			{
				embed.WithFooter((string)feat["src"]);
				embed.WithUrl((string)feat["src"]);
			}

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			return embed.Build();
		}	
		public async Task<Embed> GetAllFeats(Character c, SocketCommandContext context = null)
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

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			var sb = new StringBuilder();

			foreach(var f in feats)
			{
				var act = (string)f["actions"];

				if (act != null)
				{
					if (Icons.Actions.TryGetValue(act, out string icon))
					{
						act = icon;
					}
				}
				sb.AppendLine("• "+(f["name"]??"Unnamed Feat") + " (" + ((string)f["subtype"] ?? "Feat").Uppercase() + " " + f["level"]+") "+act??"");
			}

			embed.WithDescription(sb.ToString());

			return embed.Build();
		}
		#endregion

		#region Features
		public async Task<Embed> GetFeature(Character c, string name, SocketCommandContext context = null)
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

			var embed = FileManager.EmbedFeature(f);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			return embed.Build();
		}	
		public async Task<Embed> GetAllFeatures(Character c, SocketCommandContext context = null)
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

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
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
		public async Task<Embed> GetAction(Character c, string name, SocketCommandContext context = null)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/activities");
			var request2 = await Client.GetAsync(Api + c.RemoteId + "/feats");

			request.EnsureSuccessStatusCode();
			request2.EnsureSuccessStatusCode();


			string responsebody = await request.Content.ReadAsStringAsync();
			string responsebody2 = await request2.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);
			var json2 = JObject.Parse(responsebody2);

			if (!json["data"].HasValues && !json2["data"].HasValues) return null;

			var features = from fs in json["data"]
						   where ((string)fs["name"]).ToLower().StartsWith(name.ToLower())
						   orderby fs["name"]
						   select fs;
			var feats = from fs in json2["data"]
						where fs["actions"] != null && ((string)fs["name"]).ToLower().StartsWith(name.ToLower())
						orderby fs["name"]
						select fs;

			if (features.Count() <= 0 && feats.Count() <= 0) return null;

			var f = features.Count() == 0 ? feats.First() : features.First();

			var embed = FileManager.EmbedAction(f);

			embed.WithThumbnailUrl(c.ImageUrl);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			return embed.Build();
		}
		public async Task<Embed> GetAllActions(Character c, SocketCommandContext context = null)
		{
			var request = await Client.GetAsync(Api + c.RemoteId + "/activities");
			var request2 = await Client.GetAsync(Api + c.RemoteId + "/feats");

			request.EnsureSuccessStatusCode();
			request2.EnsureSuccessStatusCode();


			string responsebody = await request.Content.ReadAsStringAsync();
			string responsebody2 = await request2.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);
			var json2 = JObject.Parse(responsebody2);
			var afeats = json2["data"].Where(x => x["actions"] != null);

			if (!json["data"].HasValues && afeats.Count() == 0) return null;

			var feats = json["data"].Children();
			

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Actions")
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
			foreach (var a in afeats)
			{
				var act = (string)a["actions"];

				if (act != null)
				{
					if (Icons.Actions.TryGetValue(act, out string icon))
					{
						act = icon;
					}
				}
				sb.AppendLine("• " + (a["name"] ?? "Unnamed Feat") + " " + act ?? "");

			}
			embed.WithDescription(sb.ToString());
			
			return embed.Build();
		}
		#endregion

		#region Items
		public async Task<Embed> GetItem(Character c, string Name, SocketCommandContext context = null)
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

			var embed = FileManager.EmbedItem(i);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			return embed.Build();
		}	
		public async Task<Embed> Inventory(Character c, SocketCommandContext context = null)
		{
			var request = await Client.GetAsync(Api + c.RemoteId+"/items");

			request.EnsureSuccessStatusCode();

			string responsebody = await request.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			if (!json["data"].HasValues) return null;

			var full = await GetFullSheet(c);

			var pp = full["pp"] ?? 0;
			var gp = full["gp"] ?? 0;
			var sp = full["sp"] ?? 0;
			var cp = full["cp"] ?? 0;

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Inventory")
				.AddField("Currency","CP "+cp+" | SP "+ sp+" | GP "+gp+" | PP "+pp)
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

			if (json["data"]!=null && json["data"].HasValues)
			{
				var items = json["data"].Children();
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
		public async Task<Embed> GetSpell(Character c, string Name, SocketCommandContext context)
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

			var embed = FileManager.EmbedSpell(s);

			if (c.Color != null)
			{
				embed.WithColor(new Color(c.Color[0], c.Color[1], c.Color[2]));
			}

			return embed.Build();
		}
		public async Task<Embed> GetAllSpells(Character c, SocketCommandContext context)
		{
			var reqspell = await Client.GetAsync(Api + c.RemoteId + "/spells");
			reqspell.EnsureSuccessStatusCode();
			string respspell = await reqspell.Content.ReadAsStringAsync();

			var reqclasses = await Client.GetAsync(Api + c.RemoteId + "/traditions");
			reqclasses.EnsureSuccessStatusCode();
			string resclasses = await reqclasses.Content.ReadAsStringAsync();

			var reqclasses2 = await Client.GetAsync(Api + c.RemoteId + "/classes");
			reqclasses2.EnsureSuccessStatusCode();
			string resclasses2 = await reqclasses2.Content.ReadAsStringAsync();

			var reqvalues = await Client.GetAsync(Api + c.RemoteId + "/values");
			reqvalues.EnsureSuccessStatusCode();
			string resvalues = await reqvalues.Content.ReadAsStringAsync();


			var jsonspells = JObject.Parse(respspell);
			var json = JObject.Parse(resclasses);
			var json2 = JObject.Parse(resclasses2);
			json.Merge(json2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
			var values = JObject.Parse(resvalues)["data"];

			if (values == null || !values.HasValues)
			{
				return new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
					.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.")
					.Build();
			}

			var classes = from cls in json["data"]
						  where !((string)cls["tradition"]).NullorEmpty()
						  select cls;

			if (classes.Count() == 0) return null;

			var spells = jsonspells["data"].Children();

			var embed = new EmbedBuilder()
				.WithTitle(c.Name + "'s Spells")
				.WithThumbnailUrl(c.ImageUrl);

			var sb = new StringBuilder();

			foreach (var cl in classes)
			{
				if (values[((string)cl["name"]).ToLower()] == null) continue;
				bool skip = true;
				for(int i = 0;i <= 10;i++)
				{
					if ((int)cl["spell" + i] > 0) skip = false;
				}
				if (skip) continue;
				string ability = ((string)cl["ability"]).NullorEmpty() ? "" : Icons.Scores[(string)cl["ability"]] + " ";

				sb.AppendLine(ability + ((string)cl["tradition"]).Uppercase()+ " " + Icons.Proficiency[(string)cl["proficiency"]]);
				sb.AppendLine("Spell Attack `" + ((int)(values[((string)cl["name"]).ToLower()]["bonus"]??0)).ToModifierString() + "`");
				sb.AppendLine("DC `" + (values?[((string)cl["name"]).ToLower()]["value"]??"Unknown") + "`");

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
				int slots = (from cl in json["data"]
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
				if (sb.Length >= 1000)
				{
					var segments = sb.ToString().Split(1000).ToArray();
					for (int sg = 0; sg < segments.Length; sg++)
					{
						embed.AddField(" Cantrips [" + slots + "]", segments[sg]);
					}
				}
				else
				{
					embed.AddField("Cantrips [" + slots + "]", sb.ToString().TrimEnd().Substring(0, sb.Length - 2));
				}
				sb.Clear();
			}

			for (int i = 1; i < 9; i++)
			{
				var sps = from sp in spells where sp["level"]!=null &&
						  (string)sp["type"]=="spell" && 
						  (int)sp["level"] == i 
						  select sp;
				if (sps.Count() == 0) continue;

				int slots = (from cl in json["data"]
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
				if(sb.Length >= 1000)
				{
					var segments = sb.ToString().Split(1000).ToArray();
					for (int sg = 0; sg < segments.Length; sg++)
					{
						embed.AddField(i.ToPlacement() + " Level [" + slots + "]", segments[sg]);
					}
				}
				else
				{
					embed.AddField(i.ToPlacement() + " Level ["+slots+"]", sb.ToString().TrimEnd().Substring(0, sb.Length-2));
				}
				sb.Clear();
			}

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

			return embed.Build();
		}
		#endregion

		#region Familiar
		public async Task<Embed> ShowFamiliar(Character c, SocketCommandContext context)
		{
			var req = await Client.GetAsync(Api + c.RemoteId + "/familiars");
			req.EnsureSuccessStatusCode();
			string resp = await req.Content.ReadAsStringAsync();

			JObject json = (JObject)JObject.Parse(resp)["data"][0];

			if (json == null || ((string)json["name"]).NullorEmpty()) return null;

			var req2 = await Client.GetAsync(Api + c.RemoteId + "/values");
			req2.EnsureSuccessStatusCode();
			string resp2 = await req2.Content.ReadAsStringAsync();

			JToken values = JObject.Parse(resp2)["data"];

			if (values == null || !values.HasValues)
			{
				return new EmbedBuilder()
					.WithTitle("Click here")
					.WithUrl("https://character.pf2.tools/?" + c.RemoteId)
					.WithDescription("Seems like we cannot fetch " + c.Name + "'s values. This is due to the fact values are only updated when you open the sheet in pf2.tools. To fix this, click the link above to generate those values.")
					.Build();
			}
			string name = (string)json["name"];
			var embed = new EmbedBuilder()
				.WithTitle(name)
				.WithThumbnailUrl(c.FamImg);
			var sb = new StringBuilder();

			sb.AppendLine(Icons.Sheet["hp"] + " HP `" + ((int)(values["famhp"]["bonus"] ?? 0) - (int)(json["damage"] ?? 0)) + "/" + (values["famhp"]["bonus"] ?? 0) + "`");
			sb.AppendLine(Icons.Sheet["ac"] + " AC `" + (values["famac "+name]["value"] ?? "Unknown") + "`");
			sb.AppendLine(Icons.Sheet["per"] + " Perception `" + ((int)(values["famperception " + name]["bonus"] ?? 0)).ToModifierString() + "`");
			sb.AppendLine("Resistances: \n" + json["resist"]);

			embed.AddField("Stats", sb.ToString(), true);
			sb.Clear();

			sb.AppendLine(Icons.Sheet["fort"] + " `" + ((int)(values?["famfort " + name]?["bonus"] ?? 0)).ToModifierString() + "`");
			sb.AppendLine(Icons.Sheet["ref"] + " `" + ((int)(values?["famref " + name]?["bonus"] ?? 0)).ToModifierString() + "`");
			sb.AppendLine(Icons.Sheet["will"] + " `" + ((int)(values?["famwill " + name]?["bonus"] ?? 0)).ToModifierString() + "`");
			sb.AppendLine("Weakensses: \n" + json["weakness"]);

			embed.AddField("Defenses", sb.ToString(), true);
			sb.Clear();

			sb.AppendLine("Acrobatics `" + ((int)values["famacrobatics " + name]["bonus"]).ToModifierString()+"`");
			sb.AppendLine("Stealth `" + ((int)values["famstealth " + name]["bonus"]).ToModifierString() + "`");
			sb.AppendLine("Stealth `" + ((int)values["famperception " + name]["bonus"]).ToModifierString() + "`");
			sb.AppendLine("Other `" + ((int)values["famother " + name]["bonus"]).ToModifierString() + "`");

			embed.AddField("Skill Bonuses", sb.ToString(),true);
			sb.Clear();

			foreach(var x in json["abilities"].Where(x=> (bool)x["active"]))
			{
				embed.AddField((string)x["name"] ?? "Unnamed Ability", ((string)x["body"]).NullorEmpty()? "No Description":x["body"]);
			}

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
			
			
			return embed.Build();
		}
		#endregion
	}
}
