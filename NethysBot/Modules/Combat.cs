using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	[Group("Battle"), Alias("Combat","B")]
	public class Combat : NethysBase<SocketCommandContext>
	{
		[Command("New"), Alias("N","Start","Create")]
		[RequireContext(ContextType.Guild)]
		public async Task NewBattle()
		{

			var b = GetBattle(Context.Channel.Id);

			if (b.Active)
			{
				await ReplyAsync("There's already a battle happening in this channel. Use `!Battle End` to end the current combat before starting a new one.");
				return;
			}

			b.Active = true;
			b.Participants = new List<Participant>();
			b.Director = Context.User.Id;
			UpdateBattle(b);

			await ReplyAsync(Context.User.Mention + " has started a battle in <#" + Context.Channel.Id + ">! Use `!Join` To join using your active character or `!AddNPC [Name] <[Intiative]` to add an NPC.");
		}


		private Battle GetBattle(ulong channel)
		{
			var col = Database.GetCollection<Battle>("Battles");
			if (col.Exists(x => x.Channel == channel))
			{
				return col.FindOne(x => x.Channel == channel);
			}
			else
			{
				var b = new Battle()
				{
					Channel = channel,
				};
				col.Insert(b);
				col.EnsureIndex(x => x.Channel);
				return col.FindOne(x => x.Channel == channel);
			}
		}
		private void UpdateBattle(Battle b)
		{
			var col = Database.GetCollection<Battle>("Battles");
			col.Update(b);
		}
	}
}
