using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace NethysBot.Modules
{
	public class SRD : NethysBase<SocketCommandContext>
	{
		public FileManager FileManager { get; set; }
		public enum Category { Item, Feat, Action, Spell, Trait, Background };
		[Command("Wiki"), Alias("SRD")]
		public async Task search(Category category, [Remainder] string Query)
		{
			IEnumerable<JToken> results = null;
			switch (category)
			{
				case Category.Action:
					results = FileManager.Actions.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
				case Category.Feat:
					results = FileManager.Feats.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
				case Category.Item:
					results = FileManager.Features.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
				case Category.Spell:
					results = FileManager.Spells.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
				case Category.Trait:
					results = FileManager.Traits.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
				case Category.Background:
					results = FileManager.Backgrounds.Where(x => ((string)x["name"]).ToLower().StartsWith(Query.ToLower()));
					break;
			}
			if (results.Count() == 0)
			{
				await ReplyAsync("Sorry, could not find any " + category.ToString() + " named \"" + Query + "\"");
				return;
			}
			else if (results.Count() > 1)
			{
				results = results.Take(Math.Min(5, results.Count()));

				IEnumerable<string> names = results.Select(x => (string)x["name"]);

				var sb = new StringBuilder("Multiple results were found:\n");

				for (int i = 0; i < results.Count(); i++)
				{
					sb.AppendLine("`[" + i + "]` " + results.ToArray()[i]["Name"]);
				}
				var msg = await ReplyAsync(sb.ToString());
				var reply = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
				if (reply == null)
				{
					await msg.ModifyAsync(x => x.Content = "Timed out on selection.");
					return;
				}
				if (int.TryParse(reply.Content, out int index))
				{
					if (index >= results.Count())
					{
						await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
						return;
					}
					else
					{
						Embed embed = null;
						switch (category)
						{
							case Category.Action:
								embed = FileManager.EmbedAction(results.ElementAt(index)).Build();
								break;
							case Category.Feat:
								embed = FileManager.EmbedFeat(results.ElementAt(index)).Build();
								break;
							case Category.Item:
								embed = FileManager.EmbedItem(results.ElementAt(index)).Build();
								break;
							case Category.Spell:
								embed = FileManager.EmbedSpell(results.ElementAt(index)).Build();
								break;
							case Category.Trait:
								embed = FileManager.EmbedAction(results.ElementAt(index)).Build();
								break;
							case Category.Background:
								embed = FileManager.EmbedAction(results.ElementAt(index)).Build();
								break;
						}
						return;
					}
				}
			}
		}
	}
}
