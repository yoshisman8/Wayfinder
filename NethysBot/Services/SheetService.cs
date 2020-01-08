using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using NethysBot.Models;
using System.Text.RegularExpressions;
using LiteDB;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace NethysBot.Services
{
	class SheetService
	{

		private LiteCollection<Character> collection = Program.Database.GetCollection<Character>("Characters");
		private HttpClient Client;
		private string Url;
		
		public SheetService(IConfiguration config)
		{
			Client = new HttpClient();
			Url = config["apiurl"];
		}

		/// <summary>
		/// Fetches a character from the given character.pf2.tools URL.
		/// </summary>
		/// <param name="url">Url of the character</param>
		/// <returns>Parsed and updated character</returns>
		public async Task<Character> GetCharacter(string url)
		{
			var regex = new Regex(@"(\W*https?:\/\/\W*)?(\w*character\w*)\.(\w*pf2\w*)\.(\w*tools\w*)\/\?(\w{8})");

			if (!regex.IsMatch(url))
			{
				throw new Exception("Invalid Url");
			}

			var match = regex.Match(url);

			var id = match.Groups[4].Value;

			if (!collection.Exists(x => x.RemoteId == id))
			{
				collection.Insert(new Character()
				{
					RemoteId = id
				});
			}

			Character character = collection.FindOne(x => x.RemoteId == id);

			// TODO: API get request
			// Process: 
			// 1) Obtain JSON from API
			// 2) Store JSON into Cache
			// 3) Send the character for Updating

		}

		/// <summary>
		/// Updates a character
		/// </summary>
		/// <param name="Character"></param>
		/// <returns></returns>
		public async Task<Character> ParseCharacter(Character Character)
		{
			// TODO: Parse JSON obtained from the API
		}
	}
}
