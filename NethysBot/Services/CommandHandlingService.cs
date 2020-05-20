using Dice;
using NethysBot;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NethysBot.Models;
using Discord.Net.Udp;
using System.Reflection.Metadata;
using LiteDB;
using System.Security.Cryptography;
using System.Linq;
using NethysBot.Helpers;

namespace NethysBot.Services
{
	public class CommandHandlingService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;
		private readonly IConfiguration _config;
		private readonly LoggingService _logService;
		private LiteDatabase _database;
		private Regex inlinereged = new Regex(@"\[\[(.+)\]\]");

		public Dictionary<ulong, ulong> Cache { get; set; } = new Dictionary<ulong, ulong>();
		public CommandHandlingService(LoggingService Logger, IConfiguration config, IServiceProvider provider, DiscordSocketClient discord, CommandService commands, LiteDatabase database)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_config = config;
			_logService = Logger;
			_database = database;

			_discord.MessageReceived += MessageReceived;
			_discord.MessageDeleted += OnMessageDeleted;
			_discord.MessageUpdated += OnMessageUpdated;
		}

		public Server GetOrCreateServer(ulong Id)
		{
			var col = _database.GetCollection<Server>("Servers");

			if(!col.Exists(x=>x.Id == Id))
			{
				col.Insert(new Server()
				{
					Id = Id,
					Prefix = "!"
				});
			}

			return col.FindOne(x => x.Id == Id);
		}

		public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
		{
			var OldMsg = await _OldMsg.DownloadAsync();
			if (OldMsg == null || NewMsg == null) return;
			if (OldMsg.Source != MessageSource.User || NewMsg.Source != MessageSource.User) return;

			if (Cache.TryGetValue(NewMsg.Id, out var CacheMsg))
			{
				var reply = await Channel.GetMessageAsync(CacheMsg);
				await reply.DeleteAsync();
			}
			await MessageReceived(NewMsg);
		}

		public async Task OnMessageDeleted(Cacheable<IMessage, ulong> _msg, ISocketMessageChannel channel)
		{
			var msg = await _msg.GetOrDownloadAsync();
			if (msg == null || msg.Source != MessageSource.User) return;
		}

		public async Task InitializeAsync(IServiceProvider provider)
		{
			_provider = provider;
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
			// Add additional initialization code here...
		}

		private async Task MessageReceived(SocketMessage rawMessage)
		{
			// Ignore system messages and messages from bots
			if (!(rawMessage is SocketUserMessage message)) return;
			if (message.Source != MessageSource.User) return;

			var context = new SocketCommandContext(_discord, message);

			int argPos = 0;

			if(context.Guild == null)
			{
				if (!message.HasStringPrefix(_config["prefix"], ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) 
				{
					await HandleInline(context);
					return; 
				}

				await HandleCommand(context, argPos, _provider);
			}
			else
			{
				var Guild = GetOrCreateServer(context.Guild.Id);

				if (!message.HasStringPrefix(Guild.Prefix, ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
				{
					await HandleInline(context);
					return;
				}

				await HandleCommand(context, argPos, _provider);
			}

		}

		public async Task HandleCommand(SocketCommandContext Context, int argPos, IServiceProvider provider)
		{
			var result = await _commands.ExecuteAsync(Context, argPos, provider);

			if (result.Error.HasValue)
			{
				
			}
		}
		public async Task HandleInline(SocketCommandContext context)
		{
			SocketUserMessage message = context.Message;
			if (message.Content.NullorEmpty()) return;

			if (inlinereged.IsMatch(message.Content))
			{
				string tempalte = message.Content;
				foreach (var match in inlinereged.Matches(message.Content).ToList())
				{
					try
					{
						var dice = Roller.Roll(match.Groups[1].Value);

						tempalte = tempalte.Replace(match.Value, "[" + Helpers.Helpers.ParseResult(dice) + " = `" + dice.Value + "`]");
					}
					catch
					{
						continue;
					}
				}

				var embed = new EmbedBuilder()
					.WithCurrentTimestamp()
					.WithDescription(tempalte)
					.WithAuthor(context.User);
				try
				{
					await context.Message.DeleteAsync();
				}
				catch
				{

				}
				await context.Channel.SendMessageAsync(" ", false, embed.Build());
			}
		}
	}
}
