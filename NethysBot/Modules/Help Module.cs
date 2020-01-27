using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NethysBot.Modules
{
	[Name("Uncategrized")]
	public class HelpModule : NethysBase<SocketCommandContext>
	{
		public readonly CommandService commands;

		[Command("Help"), Alias("h")]
		[Summary("Shows this message.")]
		public async Task Help([Remainder]string Name = null)
		{
			
		}
	}
}
