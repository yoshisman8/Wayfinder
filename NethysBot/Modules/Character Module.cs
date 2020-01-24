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
using Microsoft.VisualBasic;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks.Dataflow;

namespace NethysBot.Modules
{
	[Name("Character")]
	public class Character_Module : NethysBase<SocketCommandContext>
	{

		[Command("Import")]
		[Summary("Import a character or companion from [pf2.tools](http://character.pf2.tools/).")]
		public async Task Import([Remainder] string url)
		{
			var user = GetUser();
			var msg = await ReplyAsync("Importing character...");
			try
			{
				var character = await SheetService.NewCharacter(url, Context);

				switch (character.Type)
				{
					case Models.SheetType.Character:
						user.Character = character;
						await msg.ModifyAsync(x=>x.Content="The character `" + character.Name + "` (id `" + character.RemoteId + "`) has been successfully imported and has been assiged as your active character!");
						break;
					case Models.SheetType.Companion:
						user.Companion = character;
						await msg.ModifyAsync(x=>x.Content= "The companion `" + character.Name + "` (id `" + character.RemoteId + "`) has been successfully imported and has been assiged as your active companion!");
						break;
				}

				UpdateUser(user);
			}
			catch(Exception e)
			{
				await msg.ModifyAsync(x=>x.Content = e.Message);
			}

		}

		[Command("Character"), Alias("Char","Sheet")]
		[Summary("Display your active character's sheet.")]
		public async Task GetSheet()
		{
			var c = GetCharacter();

			if(c == null)
			{
				await ReplyAsync("You have no active character.");
				return;
			}

			var msg = await ReplyAsync("Loading sheet...");

			var embed = await SheetService.GetSheet(c);

			await msg.ModifyAsync(x => x.Embed = embed);
			await msg.ModifyAsync(x=> x.Content = " ");
		}

		[Command("Companion"), Alias("Comp")]
		[Summary("Display your companion's character sheet.")]
		public async Task Companion([Remainder] string Name = null)
		{
			var c = GetCompanion();

			if (c == null)
			{
				await ReplyAsync("You have no active companion.");
				return;
			}

			var msg = await ReplyAsync("Loading sheet...");

			var embed = await SheetService.GetSheet(c);

			await msg.ModifyAsync(x => x.Embed = embed);
			await msg.ModifyAsync(x => x.Content = " ");
		}

		[Command("Active")]
		[Summary("See who your active character is, or change it to a different one.")]
		public async Task ActiveChar([Remainder] string Name = null)
		{
			if (Name.NullorEmpty())
			{
				var c = GetCharacter();
				var all = GetAllSheets();
				
				if (all == null)
				{
					await ReplyAsync("You have no characters or companions.");
					return;
				}

				var sb = new StringBuilder("This are " + Context.User.Username + "'s characters and companions");

				foreach(var i in all)
				{
					if (c == i) sb.AppendLine(Icons.SheetType[i.Type]+ " **" + i.Name + "** (Active)");
					else sb.AppendLine(Icons.SheetType[i.Type] + " " + i.Name);
				}

				await ReplyAsync(sb.ToString());
				return;
			}

			var chars = GetAllSheets();

			var results = chars.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

			if(results.Count() == 0)
			{
				await ReplyAsync("You have no character or companion whose name starts with that.");
				return;
			}

			if(results.Count() == 1)
			{
				var u = GetUser();

				var c = results.FirstOrDefault();
				switch (c.Type)
				{
					case Models.SheetType.Character:
						u.Character = c;
						await ReplyAsync("The character `" + c.Name + "` is now your active character!");
						break;
					case Models.SheetType.Companion:
						u.Companion = c;
						await ReplyAsync("The companion `" + c.Name + "` is now your active companion!");
						break;
				}
				UpdateUser(u);
				return;
			}
			else
			{
				var sb = new StringBuilder();
				for (int i = 0; i < results.Count(); i++)
				{
					sb.AppendLine("`[" + i + "`] " +Icons.SheetType[results.ElementAt(i).Type]+ " " + results.ElementAt(i).Name);
				}
				var msg = await ReplyAsync("Multiple sheets were found, please specify which one you wish to assign as active:\n" + sb.ToString());

				var reply = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

				if(reply == null)
				{
					await msg.ModifyAsync(x=> x.Content = "Timed out on selection.");
					return;
				}
				if(int.TryParse(reply.Content,out int index))
				{
					if(index >= results.Count())
					{
						await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
						return;
					}
					else
					{
						var c = results.ElementAt(index);
						var u = GetUser();
						switch (c.Type)
						{
							case Models.SheetType.Character:
								u.Character = c;
								await ReplyAsync("The character `" + c.Name + "` is now your active character!");
								break;
							case Models.SheetType.Companion:
								u.Companion = c;
								await ReplyAsync("The companion `" + c.Name + "` is now your active companion!");
								break;
						}
						UpdateUser(u);
						return;
					}
				}
				else
				{
					await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
					return;
				}
			}

		}

		
		[Command("Delete")]
		[Summary("Deletes a character sheet.")]
		public async Task delete([Remainder] string Name)
		{
			var chars = GetAllSheets();

			var results = chars.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));

			if (results.Count() == 0)
			{
				await ReplyAsync("You have no companions or characters whose name starts with that.");
				return;
			}

			if (results.Count() == 1)
			{
				var u = GetUser();

				var c = results.FirstOrDefault();

				if(c == u.Character)
				{
					u.Character = null;
					
					UpdateUser(u);
				}
				if(c == u.Companion)
				{
					u.Companion = null;
					UpdateUser(u);
				}
				c.Owners.Remove(Context.User.Id);
				UpdateCharacter(c);
				await ReplyAsync("Deleted character `" + c.Name + "`.");
				return;
			}
			else
			{
				var sb = new StringBuilder();
				for (int i = 0; i < results.Count(); i++)
				{
					sb.AppendLine("`[" + i + "`] " + results.ElementAt(i).Name);
				}
				var msg = await ReplyAsync("Multiple sheets were found, please specify which one you wish to delete:\n" + sb.ToString());

				var reply = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

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
						var c = results.ElementAt(index);
						var u = GetUser();
						u.Character = c;
						UpdateUser(u);
						return;
					}
				}
				else
				{
					await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
					return;
				}
			}
		}

		[Command("Sync"), Alias("Update")]
		[Summary("Sync your current character and companion to match the version on Characters.pf2.tools.")]
		public async Task Sync()
		{
			var c = GetCharacter();
			var comp = GetCompanion();

			if (c == null && comp == null)
			{
				await ReplyAsync("You have no active character or companion.");
				return;
			}
			var msg = await ReplyAsync("Updating...");
			List<string> names = new List<string>();
			if(c != null)
			{
				await SheetService.SyncCharacter(c);
				names.Add(c.Name);
			}
			if (comp != null)
			{
				await SheetService.SyncCharacter(comp);
				names.Add(comp.Name);
			}
			
			await msg.ModifyAsync(x => x.Content = "Synce " + string.Join(" and ",names) + " with their latest remote version.");
		}
	}
}
