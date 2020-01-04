using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Services
{
	class LoggingService
	{
		private readonly DiscordSocketClient _discord;
		private readonly CommandService _commands;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger _discordLogger;
		private readonly ILogger _commandsLogger;

		public LoggingService(DiscordSocketClient discord, CommandService commands, ILoggerFactory loggerFactory)
		{
			_discord = discord;
			_commands = commands;

			_discordLogger = _loggerFactory.CreateLogger("discord");
			_commandsLogger = _loggerFactory.CreateLogger("commands");

			_discord.Log += LogDiscord;
			_commands.Log += LogCommand;
		}
		public Task Log(LogLevel logLevel, string message)
		{
			_discordLogger.Log(logLevel, message);
			return Task.CompletedTask;
		}

		private Task LogDiscord(LogMessage message)
		{
			_discordLogger.Log(
				LogLevelFromSeverity(message.Severity),
				0,
				message,
				message.Exception,
				(_1, _2) => message.ToString(prependTimestamp: false));
			return Task.CompletedTask;
		}

		private Task LogCommand(LogMessage message)
		{
			// Return an error message for async commands
			if (message.Exception is CommandException command)
			{
				// Don't risk blocking the logging task by awaiting a message send; ratelimits!?
				var _ = command.Context.Channel.SendMessageAsync($"Error: {command.Message}");
			}

			_commandsLogger.Log(
				LogLevelFromSeverity(message.Severity),
				0,
				message,
				message.Exception,
				(_1, _2) => message.ToString(prependTimestamp: false));
			return Task.CompletedTask;
		}

		private static LogLevel LogLevelFromSeverity(LogSeverity severity)
			=> (LogLevel)(Math.Abs((int)severity - 5));
	}
}
