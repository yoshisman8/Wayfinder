using Discord.Commands;
using NethysBot.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	[Name("Lookup Module")]
	public class Lookup : NethysBase<SocketCommandContext>
	{

		[Command("Feat"), Alias("F","Feats")]
		[Summary("Lookup a feat on your active character. Or list all of them if used with no arguments.")]
		public async Task getfeat([Remainder] string Name = null)
		{
			var c = GetCharacter();

			if (c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			if (Name.NullorEmpty())
			{
				var fs = await SheetService.GetAllFeats(c);
				if (fs == null)
				{
					await ReplyAsync(c.Name + " has no feats.");
					return;
				}
				await ReplyAsync("", fs);
				return;
			}

			var f = await SheetService.GetFeat(c,Name);
			if (f == null)
			{
				await ReplyAsync(c.Name + " has no feat that start with that name.");
				return;
			}
			await ReplyAsync("", f);
			return;
		}
		[Command("Feature"), Alias("Features")]
		[Summary("Lookup a feature on your active character. Or list all of them if used with no arguments.")]
		public async Task getfeature([Remainder] string Name = null)
		{
			var c = GetCharacter();

			if (c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			if (Name.NullorEmpty())
			{
				var fs = await SheetService.GetAllFeatures(c);
				if (fs == null)
				{
					await ReplyAsync(c.Name + " has no features.");
					return;
				}
				await ReplyAsync("", fs);
				return;
			}

			var f = await SheetService.GetAbility(c, Name);
			if (f == null)
			{
				await ReplyAsync(c.Name + " has no feature that start with that name.");
				return;
			}
			await ReplyAsync("", f);
			return;
		}

		[Command("Inventory"), Alias("I","Item","Items")]
		public async Task Inventory([Remainder] string Name = null)
		{
			var c = GetCharacter();

			if (c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			if (Name.NullorEmpty())
			{
				var fs = await SheetService.Inventory(c);
				if (fs == null)
				{
					await ReplyAsync(c.Name + " has no Items.");
					return;
				}
				await ReplyAsync("", fs);
				return;
			}

			var f = await SheetService.GetItem(c, Name);
			if (f == null)
			{
				await ReplyAsync(c.Name + " has no Item that start with that name.");
				return;
			}
			await ReplyAsync("", f);
			return;
		}
	}
}
