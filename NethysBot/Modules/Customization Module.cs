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
		[Command("Color")] [Help("color")]
		[Summary("Changes the color of the embed in your character's sheet.")]
		public async Task SetColor(string color, string comp = null)
		{
			if (uint.TryParse(color.Remove('#'), NumberStyles.HexNumber, null, out uint colorvalue))
			{
				var col = new Color(colorvalue);

				var u = GetUser();
				if(comp == "-c")
				{
					if (u.Companion == null)
					{
						await ReplyAsync("You have no active companion to assign this color to.");
						return;
					}
					else
					{
						var c = GetCompanion();
						var i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
						c.Owners[i].Color = new int[3] { col.R, col.G, col.B };
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
						var i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
						c.Owners[i].Color = new int[3] { col.R, col.G, col.B };
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
		[Command("Image"),Alias("Thumbail")] [Help("image")]
		[Summary("Overrides the thumbain image for your sheet.")]
		public async Task image(string ImageUrl, string comp)
		{
			if (ImageUrl.IsImageUrl())
			{
				var u = GetUser();
				if (comp == "-c")
				{
					if (u.Companion == null)
					{
						await ReplyAsync("You have no active companion to assign this color to.");
						return;
					}
					else
					{
						var c = GetCompanion();
						var i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
						c.Owners[i].ImageUrl = ImageUrl;
						UpdateCharacter(c);
						await ReplyAsync("Changed " + c.Name + "'s thumbnail image.");
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
						var i = c.Owners.FindIndex(x => x.Id == Context.User.Id);
						c.Owners[i].ImageUrl = ImageUrl;
						UpdateCharacter(c);
						await ReplyAsync("Changed " + c.Name + "'s thumbnail image.");
					}
				}
			}
			else
			{
				await ReplyAsync("Invalid url/Not an image url.");
			}
		}
	}
}
