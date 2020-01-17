using Discord.Addons.Interactive;
using Discord.Commands;
using LiteDB;
using NethysBot.Helpers;
using NethysBot.Services;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO.Pipes;

namespace NethysBot.Modules
{
	[Name("Sheet Management")]
	public class Character_Module : NethysBase<SocketCommandContext>
	{

		[Command("Import")]
		[Summary("Import a character or companion from [pf2.tools](http://character.pf2.tools/).")]
		public async Task Import([Remainder] string url)
		{
			var user = GetUser();

			try
			{
				var character = await SheetService.NewCharacter(url, Context);

				switch (character.Type)
				{
					case Models.SheetType.character:
						user.Character = character;
						await ReplyAsync("The character `" + character.Name + "` (id `" + character.RemoteId + "`) has been successfully imported and has been assiged as your active character!");
						break;
					case Models.SheetType.companion:
						user.Companion = character;
						await ReplyAsync("The companion `" + character.Name + "` (id `" + character.RemoteId + "`) has been successfully imported and has been assiged as your active companion!");
						break;
				}

				UpdateUser(user);
			}
			catch(Exception e)
			{
				await ReplyAsync(Context.User.Mention + ", " + e.Message);
			}

		}

		[Command("Sheet")]
		[Summary("Display your character sheet.")]
		public async Task GetSheet()
		{
			var c = GetCharacter();

			if(c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			var embed = await SheetService.GetSheet(c);

			await ReplyAsync("", embed);
		}

		[Command("Character"), Alias("Active","Char")]
		[Summary("See who your active character is, or change it to a different one.")]
		public async Task ActiveChar([Remainder] string Name = null)
		{
			if (Name.NullorEmpty())
			{
				var c = GetCharacter();

				string current = c != null ? "Active Character: "+c.Name : "You have no active character.";

				await ReplyAsync(current);
				return;
			}

			var chars = GetAllCharacter();

			var results = chars.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

			if(results.Count() == 0)
			{
				await ReplyAsync("You have no characters whose name starts with that.");
				return;
			}

			if(results.Count() == 1)
			{
				var u = GetUser();

				var c = results.FirstOrDefault();

				switch (c.Type)
				{
					case SheetType.character:
						u.Character = c;
						break;
					case SheetType.companion:
						u.Companion = c;
						break;
				}
			}

		}

		[Command("Sync"), Alias("Update")]
		[Summary("Sync your current character to match the version on Characters.pf2.tools.")]
		public async Task Sync()
		{
			var c = GetCharacter();

			if (c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			await SheetService.SyncCharacter(c);

			await ReplyAsync("Character Updated.");
		}
	}
}
