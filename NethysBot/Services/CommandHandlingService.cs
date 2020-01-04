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

namespace NethysBot.Services
{
	class CommandHandlingService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private IServiceProvider _provider;
		private readonly IConfiguration _config;
		private readonly LoggingService _logService;

		public Dictionary<ulong, ulong> Cache { get; set; } = new Dictionary<ulong, ulong>();
		public CommandHandlingService(LoggingService Logger, IConfiguration config, IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
		{
			_discord = discord;
			_commands = commands;
			_provider = provider;
			_config = config;
			_logService = Logger;

			_discord.MessageReceived += MessageReceived;
			_discord.MessageDeleted += OnMessageDeleted;
			_discord.MessageUpdated += OnMessageUpdated;
		}

		public async Task<Server> GetOrCreateServer(ulong Id)
		{
			var col = Program.Database.GetCollection<Server>("Servers");

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

			if (Regex.IsMatch(message.Content, @"\[\[(.*?)\]\]"))
			{
				try
				{
					var rolls = Regex.Matches(message.Content, @"\[\[(.*?)\]\]");
					var sb = new StringBuilder();
					foreach (Match x in rolls)
					{
						var die = Roller.Roll(x.Groups[1].Value);
						sb.AppendLine("[" + die.Expression + "] " + die.ToString().Split("=>")[1] + " ⇒ **" + die.Value + "**.");
					}
					await message.Channel.SendMessageAsync(message.Author.Mention + "\n" + sb.ToString());
				}
				catch
				{
					await message.AddReactionsAsync(new Emoji[] { new Emoji("🎲"), new Emoji("❔") });
				}
			}

			int argPos = 0;

			if(context.Guild != null)
			{
				if (!message.HasStringPrefix("!", ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

				await HandleCommand(context, argPos, _provider);
			}
			else
			{
				var Guild = await GetOrCreateServer(context.Guild.Id);

				if (!message.HasStringPrefix(Guild.Prefix, ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

				await HandleCommand(context, argPos, _provider);
			}

			if (Guild != null && !message.HasStringPrefix(Guild.Prefix, ref argPos) && ( !message.HasMentionPrefix(_discord.CurrentUser, ref argPos) || !message.HasStringPrefix("!",ref argPos) )) return;

			var result = await _commands.ExecuteAsync(context, argPos, _provider);

			if (result.Error.HasValue && (result.Error.Value != CommandError.UnknownCommand))
			{
				Console.WriteLine(result.Error + "\n" + result.ErrorReason);
			}
			if (result.Error.HasValue && result.Error.Value == CommandError.ObjectNotFound)
			{
				var msg = await context.Channel.SendMessageAsync("Sorry. " + result.ErrorReason);
				Cache.Add(context.Message.Id, msg.Id);
			}

		}

		public async Task HandleCommand(SocketCommandContext Context, int argPos, IServiceProvider provider)
		{
			var result = await _commands.ExecuteAsync(Context, argPos, provider);

			if (result.Error.HasValue)
			{
				switch (result.Error.Value)
				{
					case CommandError.BadArgCount:
						break;

				}
			}
		}
	}
}
