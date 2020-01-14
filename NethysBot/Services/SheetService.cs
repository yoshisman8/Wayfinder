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

namespace NethysBot.Services
{
	class SheetService
	{

		private LiteCollection<Character> collection = Program.Database.GetCollection<Character>("Characters");
		private HttpClient Client;
		private string Api = "http://character.pf2.tools/api/characters/";
		
		public SheetService(IConfiguration config)
		{
			Client = new HttpClient();
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

			var id = match.Groups[4].Value;

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

			character.Name = (string)json["name"];

			if (context != null) 
			{
				if (character.Owners.Contains(context.User.Id)) character.Owners.Add(context.User.Id);
			}

			if (json.ContainsKey("customnotes"))
			{
				var notes = from n in json["customnotes"]
							where (string)n["uuid"] == "character"
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
		public async Task<Character> SyncCharacter(Character Character)
		{
			HttpResponseMessage response = await Client.GetAsync(Api + Character.RemoteId);

			response.EnsureSuccessStatusCode();

			string responsebody = await response.Content.ReadAsStringAsync();

			var json = JObject.Parse(responsebody);

			Character.SheetCache = json["data"].ToString();

			Character.ValuesCache = json["values"].ToString();

			Character.LastUpdated = DateTime.Now;

			Character.Name = json.ContainsKey("name") ? (string)json["name"] : "Unnamed Character";

			Character.Type = Enum.Parse<SheetType>((string)json["type"]);

			collection.Update(Character);

			return Character;
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
		public async Task<Embed> GetSheet(Character character)
		{
			Character c = await SyncCharacter(character);
			string url = "https://character.pf2.tools/?" + c.RemoteId;

			var full = JObject.Parse(c.SheetCache);
			var values = JObject.Parse(c.ValuesCache);

			var embed = new EmbedBuilder()
				.WithTitle("[" + c.Name + "](" + url + ")")
				.WithThumbnailUrl(c.ImageUrl);
			var sb = new StringBuilder();

			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["strength"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");
			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["dexterity"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");
			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["constitution"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");
			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["intelligence"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");
			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["wisdom"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");
			sb.AppendLine(Dictionaries.Scores["str"] + " " + (string)values["charisma"]["value"] + " (" + ((int)values["strength"]["value"]).PrintModifier() + ")");

		}
	}
}
