using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;

namespace Taiwu_Foresight
{
    public partial class Foresight
	{
		static string ToInfo(string[] text, int msgLevel = 1)
		{
			var result = "";
			foreach(var item in text)
				result+=ToInfo(item, msgLevel);
			return	result;
		}
		static string ToInfo(string text, int msgLevel=1)
		{
			var levelabs = Math.Abs(msgLevel);
			var result = "";
			if (levelabs == 1)
				result += $"<color=#grey>·{text}</color>\n";//align会改变整行
			else if (levelabs == 2)
				result += $"<color=#grey>\t·{text}</color>\n";
			else if (levelabs == 3)
				result += $"<color=#grey>\t\t·{text}</color>\n";
			else if (levelabs == 4)
				result += $"<color=#grey>\t\t\t·{text}</color>\n";
			else
				result += $"<color=#grey>\t\t·{text}</color>\n";
			return result;
		}

	}
}
