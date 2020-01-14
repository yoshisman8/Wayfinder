using Discord.Commands;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Helpers
{
	/// <summary>
	/// Ensures an user file for the command issuer exists
	/// </summary>
	class EnsureUser : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			var col = Program.Database.GetCollection<User>("Users");

			if(!col.Exists(x=>x.Id == context.User.Id))
			{
				col.Insert(new User()
				{
					Id = context.User.Id
				});
			}
			return Task.FromResult(PreconditionResult.FromSuccess());
		}
	}
}
