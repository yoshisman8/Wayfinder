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
		
		public SheetService(LiteDatabase database)
		{
			Client = new HttpClient();
			collection = database.GetCollection<Character>("Characters");
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

			character.Type = Enum.Parse<SheetType>((string)json["data"]["type"]);

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

			var full = JObject.Parse(c.SheetCache);
			var values = JObject.Parse(c.ValuesCache);

			var embed = new EmbedBuilder()
				.WithTitle(c.Name)
				.WithUrl(url)
				.WithThumbnailUrl(c.ImageUrl);
			var sb = new StringBuilder();
			sb.AppendLine("Lv" + (string)full["level"] + (((string)full["ancestry"]).NullorEmpty()? "" : " " + (string)full["ancestry"]) + (full["classes"].HasValues ? " "+(string)full["classes"][0]["name"]:" Adventurer"));
			sb.AppendLine(Icons.Sheet["hp"] + " HP `" + ((int)values["hp"]["value"] - (int)full["damage"])+ "/" + values["hp"]["value"]+"`");
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

			var skills = from sk in full["skills"] where (string)sk["proficiency"] != "u" select sk;

			foreach (var s in skills)
			{
				if (!((string)s["lore"]).NullorEmpty())
				{
					int bonus = int.Parse((string)values[((string)s["lore"]).ToLower()]["value"] ?? "0");
					sb.Append(s["lore"] + " " + Icons.Proficiency[(string)s["proficiency"]] + " " + bonus.ToModifierString() + ", ");
				}
				else
				{
					int bonus = int.Parse((string)values[((string)s["name"]).ToLower()]["value"] ?? "0");
					sb.Append(s["name"] + " " + Icons.Proficiency[(string)s["proficiency"]] + " " + bonus.ToModifierString() + ", ");
				}
			}
			embed.AddField("Skills", sb.ToString().Trim().Substring(0, sb.Length - 2));
			sb.Clear();


			var conditions = from con in full["conditions"]
							 where con["value"] != null
							 select con;
			if(conditions.Count() > 0)
			{
				foreach(var con in conditions)
				{
					sb.AppendLine(con["name"] + " " + (int)con["value"]);
				}
				embed.AddField("Conditions", sb.ToString());
			}

			return embed.Build();
		}
	}
}
