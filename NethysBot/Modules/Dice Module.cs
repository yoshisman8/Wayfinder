using Discord.Commands;
using NethysBot.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	class Dice_Module : NethysBase<SocketCommandContext>
	{
		private Regex DiceRegex = new Regex(@"(\d?[dD]\d)+\s*((\+|\-|)?\s*(\d+)?)*");
		private Regex AttributeRegex = new Regex(@"(\{(\w+)\})");


		[EnsureUser] [Command("Roll"), Alias("R","Dice")]
		[Summary("")]
		public async Task Roll([Remainder] string Expression)
		{

		}
	}
}
