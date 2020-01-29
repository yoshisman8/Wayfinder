using Antlr4.Runtime.Dfa;
using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NethysBot.Modules
{
	public class HelpModule : NethysBase<SocketCommandContext>
	{
		public FileManager document { get; set; }

		[Command("Help"), Alias("h","Commands")]
		[Summary("Shows this message.")]
		public async Task Help([Remainder]string Command = null)
		{
			if (Command.NullorEmpty())
			{
				
				var embed = new EmbedBuilder()
					.WithDescription("Wayfinder is a Pathfinder 2nd Edition utility bot designed to facilitate online play.\n"+
					"This bot is meant to be used in conjuction of [character.pf2.tools](http://character.pf2.tools) as all sheets have to be imported from there.\n"+
					"Invite Wayfinder to your server [here](https://discordapp.com/api/oauth2/authorize?client_id=663127829621506068&permissions=288832&scope=bot)!");

				foreach(var c in document.Categories)
				{
					
					var sb = new StringBuilder();
					foreach(var cmd in document.Commands.Where(x=> (string)x["Category"] == c))
					{
						sb.AppendLine("**" + cmd["Name"] + "**: " + cmd["Summary"]);
					}
					embed.AddField(c, sb.ToString());
				}
				embed.AddField("More Help", "Type `!Help <command>` to get more info on a specific command.");
				var DMs = await Context.User.GetOrCreateDMChannelAsync();
				if(Context.Guild != null)
				{
					await ReplyAsync("A list of commands has been sent to your private messages.");
				}
				await DMs.SendMessageAsync(" ", false, embed.Build());
			}
			else
			{
				var cmds = document.Commands.Where(x=>((string)x["Name"]).ToLower().StartsWith(Command.ToLower())).OrderBy(x=>x["Name"]);

				if (cmds.Count() == 0)
				{
					await ReplyAsync("No command called \""+Command+"\" found.");
					return;
				}
				else
				{
					var cmd = cmds.FirstOrDefault();

					var embed = new EmbedBuilder()
						.AddField((string)cmd["Usage"], (string)cmd["Summary"]);
					if(cmd["Extra"] != null)
					{
						foreach (JObject e in cmd["Extra"])
						{
							var n = e.Properties().Select(x => x.Name).First();
							embed.AddField(n, (string)e[n]);
						}
					}
					var DMs = await Context.User.GetOrCreateDMChannelAsync();
					if (Context.Guild != null)
					{
						await ReplyAsync("You've been sent help to your private messages.");
					}
					await DMs.SendMessageAsync(" ", false, embed.Build());
				}
			}
		}
	}
}
