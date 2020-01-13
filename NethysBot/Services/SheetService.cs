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
		/// Fetches a character from the given character.pf2.tools URL.
		/// </summary>
		/// <param name="url">Url of the character</param>
		/// <returns>Parsed and updated character</returns>
		public async Task<Character> GetCharacter(string url, SocketCommandContext context = null)
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

				if (context != null) c.Owner = context.User.Id;

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

			collection.Update(character);

			return character;
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

			Character.Name = (string)json["name"];

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
	}
}
