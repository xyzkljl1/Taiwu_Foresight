using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
				result += $"<color=#white>·{text}</color>\n";//align会改变整行
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
		public static FieldType GetPrivateField<FieldType>(object instance, string field_name)
		{
			Type type = instance.GetType();
			FieldInfo field_info = type.GetField(field_name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			return (FieldType)field_info.GetValue(instance);
		}
		public static void SetPrivateField<FieldType>(object instance, string field_name, FieldType value)
		{
			Type type = instance.GetType();
			FieldInfo field_info = type.GetField(field_name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			field_info.SetValue(instance, value);
		}
		public static void SetPublicField<FieldType>(object instance, string field_name, FieldType value)
		{
			Type type = instance.GetType();
			FieldInfo field_info = type.GetField(field_name, System.Reflection.BindingFlags.Instance);
			field_info.SetValue(instance, value);
		}

	}
}
