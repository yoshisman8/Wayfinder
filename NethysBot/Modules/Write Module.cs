using Discord.Commands;
using NethysBot.Helpers;
using NethysBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NethysBot.Modules
{
	public class Write_Module : NethysBase<SocketCommandContext>
	{
		private Regex TypeRegex = new Regex(@"(\w+)\s+(\d+)");

		[Command("Health"),Alias("HP","Hit-Points")]
		public async Task ChangeHP(int value, [Remainder]string args = null)
		{
			Character c;
			args = args.ToLower();

			if (args.Contains("-c"))
			{
				c = GetCompanion();
				args = args.Replace("-c", "").Trim();
				if (c == null)
				{
					await ReplyAsync("You have no active companion.");
					return;
				}
			}
			else
			{
				c = GetCharacter();
				if (c == null)
				{
					await ReplyAsync("You have no active character.");
					return;
				}
			}

			int CurrentHp = 0;
			int MaxHp = 0;
			int TempHP = 0;
			int Damage = 0;

			JArray Json1 = await SheetService.Get(c, "damage+hptemp/");
			Damage = (string)Json1["damage"] == "null" ? 0 : (int)Json1["damage"];
			TempHP = (string)Json1["hptemp"] == "null" ? 0 : (int)Json1["hptemp"];

			JObject values = await SheetService.GetValues(c);

			MaxHp = (int)values["hp"]["value"];

			CurrentHp = MaxHp - Damage;


		}
	}
}
