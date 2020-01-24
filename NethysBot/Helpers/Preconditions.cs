using Discord.Commands;
using LiteDB;
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
	public class EnsureUser : PreconditionAttribute
	{
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			LiteDatabase db = (LiteDatabase)services.GetService(typeof(LiteDatabase));

			var col = db.GetCollection<User>("Users");

			if(!col.Exists(x=>x.Id == context.User.Id.ToString()))
			{
				col.Insert(new User()
				{
					Id = context.User.Id.ToString()
				});
			}
			return Task.FromResult(PreconditionResult.FromSuccess());
		}
	}


}
