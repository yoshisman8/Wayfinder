using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using LiteDB;
using NethysBot.Models;
using NethysBot.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Helpers
{
	public class NethysBase<T> : InteractiveBase<T>
		where T : SocketCommandContext
	{
		public CommandHandlingService Command { get; set; }

		public SheetService SheetService { get; set; }
		public LiteDatabase Database { get; set; }

		public async Task<RestUserMessage> ReplyAsync(string Content, Embed Embed = null, bool isTTS = false)
		{
			if (Command.Cache.TryGetValue(Context.Message.Id, out ulong id))
			{
				var msg = (RestUserMessage)await Context.Channel.GetMessageAsync(id);
				if (msg == null)
				{
					return await Context.Channel.SendMessageAsync(Content, isTTS, Embed);
				}
				else
				{
					await msg.ModifyAsync(x => x.Content = Content);
					await msg.ModifyAsync(x => x.Embed = Embed);
					return msg;
				}
			}
			else
			{
				var msg = await Context.Channel.SendMessageAsync(Content, isTTS, Embed);
				Command.Cache.Add(Context.Message.Id, msg.Id);
				return msg;
			}
		}

		/// <summary>
		/// Gets the current user's active character, returns null when there is no active character
		/// </summary>
		/// <returns>The character, or Null when no active character is set.</returns>
		public Character GetCharacter()
		{
			var user = GetUser();

			return user.Character;
		}
		public Character GetCompanion()
		{
			var user = GetUser();
			return user.Companion;
		}
		/// <summary>
		/// Gets the current user's characters and companions.
		/// </summary>
		/// <returns></returns>
		public List<Character> GetAllCharacter()
		{
			var user = GetUser();

			var Characters = Database.GetCollection<Character>("Characters");

			var chars = Characters.Find(x => x.Owners.Contains(user.Id));

			return chars.ToList();
		}

		/// <summary>
		/// Get Current user.
		/// </summary>
		/// <returns>The user</returns>
		public User GetUser()
		{
			var Users = Database.GetCollection<User>("Users");

			if(!Users.Exists(x => x.Id == Context.User.Id))
			{
				Users.Insert(new User()
				{
					Id = Context.User.Id
				});
			}

			var user = Users.Include(x => x.Character).Include(x => x.Companion).FindOne(x => x.Id == Context.User.Id);

			return user;
		}

		public void UpdateUser(User user)
		{
			var Users = Database.GetCollection<User>("Users");

			Users.Update(user);
		}
	}
}
