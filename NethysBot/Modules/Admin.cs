using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	public class Admin : NethysBase<SocketCommandContext>
	{
		[Command("Prefix")] [RequireContext(ContextType.Guild)]
		[RequireUserPermission(Discord.ChannelPermission.ManageChannels)]
		public async Task changeprefix([Remainder] string prefix)
		{
			var col = Database.GetCollection<Server>("Servers");

			var s = col.FindOne(x => x.Id == Context.Guild.Id);

			s.Prefix = prefix[0].ToString();

			col.Update(s);

			await ReplyAsync("Changed the bot's prefix for this server to `" + s.Prefix + "`.");
		}
	}
}
