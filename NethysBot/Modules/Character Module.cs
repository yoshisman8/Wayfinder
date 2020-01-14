using Discord.Addons.Interactive;
using Discord.Commands;
using LiteDB;
using NethysBot.Helpers;
using NethysBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	class Character_Module : NethysBase<SocketCommandContext>
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

				Users.Update(user);
			}
			catch(Exception e)
			{
				await ReplyAsync(Context.User.Mention + ", " + e.Message);
			}

		}
	}
}
