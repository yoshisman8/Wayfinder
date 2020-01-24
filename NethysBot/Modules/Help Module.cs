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
			if (Name.NullorEmpty())
			{
				var embed = new EmbedBuilder()
					.WithDescription("Wayfinder is a Pathfinder 2nd Edition bot that aims to bring all the functionality of a character sheet to discord so you can run your text-based campaigns in discord.\n"+
						"Your sheets will have to be made over at [http://character.pf2.tools](pf2.tools) and then imported here using the `!import` command.");
				var modules = commands.Modules;

				foreach(var m in modules)
				{
					var sb = new StringBuilder();
					foreach(var c in m.Commands)
					{
						sb.AppendLine("**" + c.Name + "** - " + c.Summary);
					}
					embed.AddField(m.Name, sb.ToString());
				}
				embed.AddField("Additional help", "Type !help <command> for more info on a command.");
			}
			else
			{
				var cmds = commands.Search(Context, Name);
				if (!cmds.IsSuccess)
				{
					await ReplyAsync("Could not find a command with that name.");
					return;
				}
				else
				{
					var cmd = cmds.Commands.OrderBy(x => x.Command.Name).FirstOrDefault().Command;
					var embed = new EmbedBuilder();
					string cmdname = cmd.Name + (cmd.Aliases.Count > 0 ? "|"+string.Join("|", cmd.Aliases) : "");
					string args = string.Join(" ", cmd.Parameters.Select(x => x.IsOptional?"<"+x.Name+">":"["+x.Name+"]"));
					embed.AddField("![" + cmdname + "]", cmd.Summary);
					if (cmd.Attributes.Any(x => x.GetType() == typeof(Help)))
					{
						if(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data", ((Help)cmd.Attributes.Where(x=>x.GetType() == typeof(Help)).FirstOrDefault()).Filename)))
						{
							var file = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "data", ((Help)cmd.Attributes.Where(x => x.GetType() == typeof(Help)).FirstOrDefault()).Filename));
							var json = JObject.Parse(file);
						}
					}
				}
			}
		}
	}
}
