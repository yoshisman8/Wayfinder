using Antlr4.Runtime.Misc;
using Discord;
using Discord.Commands;
using NethysBot.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	[Name("Customization")]
	public class CustomizationModule : NethysBase<SocketCommandContext>
	{
		[Command("Color")] 
		[Summary("Changes the color of the embed in your character's sheet.")]
		public async Task SetColor(string color, string comp = null)
		{
			if (color.StartsWith("#"))
			{
				color = color.Remove(0, 1);
			}


			if (uint.TryParse(color, NumberStyles.HexNumber, null, out uint colorvalue))
			{
				var col = new Color(colorvalue);

				var u = GetUser();
				if(!comp.NullorEmpty() && comp == "-c")
				{
					if (u.Companion == null)
					{
						await ReplyAsync("You have no active companion to assign this color to.");
						return;
					}
					else
					{
						var c = GetCompanion();
						c.Color = new int[3] { col.R, col.G, col.B };
						UpdateCharacter(c);
						await ReplyAsync("Changed " + c.Name + "'s color.");
					}
				}
				else
				{
					if (u.Character == null)
					{
						await ReplyAsync("You have no active character to assign this color to.");
						return;
					}
					else
					{
						var c = GetCharacter();
						c.Color = new int[3] { col.R, col.G, col.B };
						UpdateCharacter(c);
						await ReplyAsync("Changed " + c.Name + "'s color.");
					}
				}
			}
			else
			{
				await ReplyAsync("Invalid color code. The color must be a hex code (ie: #DE664D).");
			}
		}
	}
}
