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
using System.Xml;

namespace NethysBot.Modules
{
	[Name("Character")]
	public class CharacterModule : NethysBase<SocketCommandContext>
	{

		[Command("Import")]
		[Summary("Import a character or companion from [pf2.tools](http://character.pf2.tools).")]
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
						user.Character = character.RemoteId;
						await msg.ModifyAsync(x=>x.Content="The character `" + character.Name + "` (id `" + character.RemoteId + "`) has been successfully imported and has been assiged as your active character!");
						break;
					case Models.SheetType.Companion:
						user.Companion = character.RemoteId;
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

		[Command("Character"), Alias("Char")]
		[Summary("Display your active character's sheet or change your active character.")]
		public async Task GetSheet([Remainder] string Name = null)
		{
			if (Name.NullorEmpty())
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
			else
			{
				var chars = GetAllSheets().Where(x=>x.Type == SheetType.Character && x.Name.ToLower().StartsWith(Name.ToLower()));

				if(chars.Count() == 0)
				{
					await ReplyAsync("You have no characters! Import one by using the `!import` command.");
					return;
				}
				else if(chars.Count() == 1)
				{
					var u = GetUser();
					var c = chars.FirstOrDefault();
					u.Character = c.RemoteId;
					UpdateUser(u);

					await ReplyAsync("Changed your active character to " + c.Name + ".", await SheetService.GetSheet(c, Context));
					return;
				}
				else
				{
					var sb = new StringBuilder();
					for (int i = 0; i < chars.Count(); i++)
					{
						sb.AppendLine("`[" + i + "`] " + Icons.SheetType[chars.ElementAt(i).Type] + " " + chars.ElementAt(i).Name);
					}
					var msg = await ReplyAsync("Multiple characters were found, please specify which one you wish to assign as your active character:\n" + sb.ToString());

					var reply = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

					if (reply == null)
					{
						await msg.ModifyAsync(x => x.Content = "Timed out on selection.");
						return;
					}
					if (int.TryParse(reply.Content, out int index))
					{
						if (index >= chars.Count())
						{
							await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
							return;
						}
						else
						{
							var c = chars.ElementAt(index);
							var u = GetUser();
							u.Character = c.RemoteId;
							UpdateUser(u);
							await ReplyAsync("Changed your active character to " + c.Name + ".", await SheetService.GetSheet(c, Context));
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
		}

		[Command("Companion"), Alias("Comp")]
		[Summary("Display your active companion's sheet or change your active companion.")]
		public async Task Companion([Remainder] string Name = null)
		{
			if (Name.NullorEmpty())
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
			else
			{
				var chars = GetAllSheets().Where(x => x.Type == SheetType.Companion && x.Name.ToLower().StartsWith(Name.ToLower()));

				if (chars.Count() == 0)
				{
					await ReplyAsync("You have no companions! Import one by using the `!import` command.");
					return;
				}
				else if (chars.Count() == 1)
				{
					var u = GetUser();
					var c = chars.FirstOrDefault();
					u.Character = c.RemoteId;
					UpdateUser(u);

					await ReplyAsync("Changed your active companion to " + c.Name + ".", await SheetService.GetSheet(c, Context));
					return;
				}
				else
				{
					var sb = new StringBuilder();
					for (int i = 0; i < chars.Count(); i++)
					{
						sb.AppendLine("`[" + i + "`] " + Icons.SheetType[chars.ElementAt(i).Type] + " " + chars.ElementAt(i).Name);
					}
					var msg = await ReplyAsync("Multiple companions were found, please specify which one you wish to assign as your active companion:\n" + sb.ToString());

					var reply = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

					if (reply == null)
					{
						await msg.ModifyAsync(x => x.Content = "Timed out on selection.");
						return;
					}
					if (int.TryParse(reply.Content, out int index))
					{
						if (index >= chars.Count())
						{
							await msg.ModifyAsync(x => x.Content = "Invalid choice, operation cancelled.");
							return;
						}
						else
						{
							var c = chars.ElementAt(index);
							var u = GetUser();
							u.Companion = c.RemoteId;
							UpdateUser(u);
							await ReplyAsync("Changed your active companion to " + c.Name + ".", await SheetService.GetSheet(c, Context));
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
		}

		[Command("Sheets"),Alias("List")]
		[Summary("List all of your character and companion sheets.")]
		public async Task ActiveChar()
		{
			var c = GetCharacter();
			var p = GetCompanion();
			var all = GetAllSheets();
				
			if (all == null || all.Count() == 0 )
			{
				await ReplyAsync("You have no characters or companions.");
				return;
			}

			var sb = new StringBuilder("This are " + Context.User.Username + "'s characters and companions:\n");

			foreach(var i in all)
			{
				if (c?.RemoteId == i.RemoteId || p?.RemoteId == i.RemoteId) sb.AppendLine(Icons.SheetType[i.Type]+ " **" + i.Name + "** (Active)");
				else sb.AppendLine(Icons.SheetType[i.Type] + " " + i.Name);
			}

			await ReplyAsync(sb.ToString());
			return;
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

				if(c.RemoteId == u.Character)
				{
					u.Character = null;
					
					UpdateUser(u);
				}
				if(c.RemoteId == u.Companion)
				{
					u.Companion = null;
					UpdateUser(u);
				}
				int i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
				c.Owners.RemoveAt(i);
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

						if (c.RemoteId == u.Character)
						{
							u.Character = null;

							UpdateUser(u);
						}
						if (c.RemoteId == u.Companion)
						{
							u.Companion = null;
							UpdateUser(u);
						}
						int i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
						c.Owners.RemoveAt(i);
						UpdateCharacter(c);
						await ReplyAsync("Deleted character `" + c.Name + "`.");
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
