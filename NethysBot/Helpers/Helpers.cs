using Discord.WebSocket;
using NethysBot.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace NethysBot.Helpers
{
	public static class Helpers
	{
		public static bool IsImageUrl(this string URL)
		{
			try
			{
				var req = (HttpWebRequest)HttpWebRequest.Create(URL);
				req.Method = "HEAD";
				using (var resp = req.GetResponse())
				{
					return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
							.StartsWith("image/",StringComparison.OrdinalIgnoreCase);
				}
			}
			catch
			{
				return false;
			}
		}
		public static bool NullorEmpty(this string _string)
		{
			if (_string == null) return true;
			if (_string == "") return true;
			else return false;
		}
		public static SocketTextChannel GetTextChannelByName(this SocketGuild Guild, string Name)
		{
			var results = Guild.TextChannels.Where(x => x.Name.ToLower() == Name.ToLower());
			if (results == null || results.Count() == 0) return null;
			else return results.FirstOrDefault();
		}
		/// <summary>
		/// Calculates the modifier bonus of an int
		/// 12 = 2; 8 = -1;
		/// </summary>
		/// <param name="score"></param>
		/// <returns></returns>
		public static int GetModifier(this int score)
		{
			return (int)Math.Floor((decimal)((score - 10) / 2));
		}
		/// <summary>
		/// Prints the modifier for an int.
		/// 12 = "+2"; 10 = "+0"; 8 = "-1";
		/// </summary>
		/// <param name="score"></param>
		/// <returns></returns>
		public static string PrintModifier(this  int score)
		{
			int modifier = GetModifier(score);
			return modifier >= 0 ? "+" + modifier : modifier.ToString();
		}
		/// <summary>
		/// Turn a string into a bonus string value
		/// 10 = "+10", 0 = "+0"; -5 = "-5"
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToModifierString(this int value)
		{
			return value >= 0 ? "+" + value : value.ToString();
		}
		/// <summary>
		/// Check if it's been at least an x amount of time since the last update.
		/// </summary>
		/// <param name="dt1"></param>
		/// <param name="span"></param>
		/// <returns></returns>
		public static bool Outdated(this DateTime dt1)
		{
			return (DateTime.Now - dt1) > TimeSpan.FromSeconds(3);
		}
		public static string FixLength(this string text, int length, TextPadding padding = TextPadding.After)
		{
			if (text.Length >= length) return text.Substring(0,length);

			switch (padding)
			{
				case TextPadding.After:
					string after = text.PadRight(length, '\u2002');
					return after;
				case TextPadding.Before:
					string before = text.PadLeft(length, '\u2002');
					return before;
				default:
					string def = text.PadRight(length);
					return def;
			}
		}
		/// <summary>
		/// Capitalizes the first char of a string
		/// hello -> Hello
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string Uppercase(this string text)
		{
			if(text.NullorEmpty())
			{
				return text;
			}
			else if(text.Length == 1)
			{
				return text.ToUpper();
			}
			else
			{
				return char.ToUpper(text[0]) + text.Substring(1);
			}
		}
		public static IEnumerable<string> Split(this string str, int maxChunkSize)
		{
			for (int i = 0; i < str.Length; i += maxChunkSize)
				yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
		}
		public static string ToPlacement(this int number)
		{
			int n = Math.Abs(number);
			if (n == 1) return number + "st";
			else if (n == 2) return number + "nd";
			else if (n == 3) return number + "rd";
			else return number + "th";
		}

		public static bool StartsWithVowel(this string text)
		{
			return "aeiou".IndexOf(text[0].ToString(), StringComparison.InvariantCultureIgnoreCase) >= 0;
		}
	}
	public enum TextPadding { Before, After }
	public static class Icons
	{
		public static Dictionary<int, string> d20 { get; set; } = new Dictionary<int, string>()
		{
			{20, "<:d20_20:663149799792705557>" },
			{19, "<:d20_19:663149782847586304>" },
			{18, "<:d20_18:663149770621190145>" },
			{17, "<:d20_17:663149758885396502>" },
			{16, "<:d20_16:663149470216749107>" },
			{15, "<:d20_15:663149458963300352>" },
			{14, "<:d20_14:663149447278100500>" },
			{13, "<:d20_13:663149437459234846>" },
			{12, "<:d20_12:663149424909746207>" },
			{11, "<:d20_11:663149398712123415>" },
			{10, "<:d20_10:663149389396574212>" },
			{9, "<:d20_9:663149377954775076>" },
			{8, "<:d20_8:663149293695139840>" },
			{7, "<:d20_7:663149292743032852>" },
			{6, "<:d20_6:663149290532634635>" },
			{5, "<:d20_5:663147362608480276>" },
			{4, "<:d20_4:663147362512011305>" },
			{3, "<:d20_3:663147362067415041>" },
			{2, "<:d20_2:663147361954037825>" },
			{1, "<:d20_1:663146691016523779>" }
		};
		public static Dictionary<int, string> d12 { get; set; } = new Dictionary<int, string>()
		{
			{12, "<:d12_12:663152540426174484>" },
			{11, "<:d12_11:663152540472442900>" },
			{10, "<:d12_10:663152540439019527>" },
			{9, "<:d12_9:663152540199682061>" },
			{8, "<:d12_8:663152540459728947>" },
			{7, "<:d12_7:663152540116058133>" },
			{6, "<:d12_6:663152540484894740>" },
			{5, "<:d12_5:663152540250144804>" },
			{4, "<:d12_4:663152540426305546>" },
			{3, "<:d12_3:663152540161933326>" },
			{2, "<:d12_2:663152538291404821>" },
			{1, "<:d12_1:663152538396393482>" }
		};
		public static Dictionary<int, string> d10 { get; set; } = new Dictionary<int, string>()
		{
			{10, "<:d10_10:663158741352579122>" },
			{9, "<:d10_9:663158741331476480>" },
			{8, "<:d10_8:663158741079687189>" },
			{7, "<:d10_7:663158742636036138>" },
			{6, "<:d10_6:663158741121761280>" },
			{5, "<:d10_5:663158740576632843>" },
			{4, "<:d10_4:663158740685553713>" },
			{3, "<:d10_3:663158740442415175>" },
			{2, "<:d10_2:663158740496810011>" },
			{1, "<:d10_1:663158740463255592>" }
		};
		public static Dictionary<int, string> d8 { get; set; } = new Dictionary<int, string>()
		{
			{8, "<:d8_8:663158785795162112>" },
			{7, "<:d8_7:663158785841561629>" },
			{6, "<:d8_6:663158785774190595>" },
			{5, "<:d8_5:663158785271005185>" },
			{4, "<:d8_4:663158785107296286>" },
			{3, "<:d8_3:663158785543503920>" },
			{2, "<:d8_2:663158785224867880>" },
			{1, "<:d8_1:663158784859963473>" }
		};
		public static Dictionary<int, string> d6 { get; set; } = new Dictionary<int, string>()
		{
			{6, "<:d6_6:663158852551835678>" },
			{5, "<:d6_5:663158852136599564>" },
			{4, "<:d6_4:663158856247148566>" },
			{3, "<:d6_3:663158852358766632>" },
			{2, "<:d6_2:663158852354834452>" },
			{1, "<:d6_1:663158852354572309>" }
		};
		public static Dictionary<int, string> d4 { get; set; } = new Dictionary<int, string>()
		{
			{4, "<:d4_4:663158852472274944>" },
			{3, "<:d4_3:663158852178411560>" },
			{2, "<:d4_2:663158851734077462>" },
			{1, "<:d4_1:663158851909976085>" }
		};

		public static Dictionary<string, string> Scores { get; set; } = new Dictionary<string, string>()
		{
			{"strength", "<:str:666744920794595338>" },
			{"dexterity", "<:dex:666744920588943363>" },
			{"constitution", "<:con:666744920593268757>" },
			{"intelligence", "<:int:666744920828280832>" },
			{"wisdom", "<:wis:666744920836407385>" },
			{"charisma", "<:cha:666744920438210561>" }
		};
		public static Dictionary<string, string> Sheet { get; set; } = new Dictionary<string, string>()
		{
			{"fort", "<:fortitude:666995019902615572>" },
			{"ref", "<:reflex:666995418235666432>" },
			{"will", "<:will:666995701703770121>" },
			{"ac", "<:ac:666996197583486996>" },
			{"per", "<:perception:666997096406056960>" },
			{"hp", "<:hp:666992409506349057>" },
			{"land", "<:land:667370170146226188>" },
			{"burrow", "<:burrow:667370169290457098>" },
			{"swim", "<:swim:667370169777127453>" },
			{"fly", "<:fly:667370169252839435>" },
			{"climb" , "<:climb:667370169613680650>" }
		};
		public static Dictionary<string, string> Proficiency { get; set; } = new Dictionary<string, string>()
		{
			{"u", "<:untrained:667472897656356886>" },
			{"t", "<:trained:667472897748762624>" },
			{"e", "<:expert:667472897761345559>" },
			{"m", "<:master:667472897811546157>" },
			{"l", "<:legendary:667472897777860638>" }
		};
		public static Dictionary<string, string> Actions { get; set; } = new Dictionary<string, string>()
		{
			{"0", "<:free_action:667465200697475083>" },
			{"1", "<:1_action:667465200974299146>" },
			{"2", "<:2_actions:667465200672178207>" },
			{"3", "<:3_actions:667465200814915616>" },
			{"r", "<:reaction:667465201192271943>" }
		};
		public static Dictionary<SheetType, string> SheetType { get; set; } = new Dictionary<SheetType, string>()
		{
			{ Models.SheetType.Companion,"<:companion:669987675616313385>" },
			{ Models.SheetType.Character, "<:character:669987675142357032>" }
		};
		public static Dictionary<string, string> Strike { get; set; } = new Dictionary<string, string>()
		{
			{"","<:custom:671802295260020786>" },
			{"custom","<:custom:671802295260020786>" },
			{"spell","<:spell:671803477193785365>" },
			{"melee","<:melee:671802294911762484>" },
			{"ranged","<:ranged:671802295138385930>" }
		};
	}
}
