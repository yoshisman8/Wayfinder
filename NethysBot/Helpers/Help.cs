using System;
using System.Collections.Generic;
using System.Text;

namespace NethysBot.Helpers
{
	public class Help : Attribute
	{
		public string Filename { get; private set; }
		public Help(string filename)
		{
			Filename = filename+".json";
		}
	}
}
