using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.SpecialEffect;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using GameData.Domains;
using System.Reflection;
using UICommon.Character;
using CharacterDataMonitor;
using Config;
using System.IO;
using UICommon.Character.Elements;
using System.Threading;
using UnityEngine;
using System.Text.RegularExpressions;
using GameData.Domains.TaiwuEvent.DisplayEvent;

namespace Taiwu_Foresight
{
    [PluginConfig("Foresight", "xyzkljl1", "1.0.0")]
    public partial class Foresight : TaiwuRemakePlugin
    {
        public static bool On = false;
        Harmony harmony;
        public override void Dispose()
        {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(Foresight));
        }
        public override void OnModSettingUpdate()
        {
            //不需要showUseless
            ModManager.GetSetting(ModIdStr, "On", ref On);
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
        //TODO 如何更自动的找到Option_Key和选项的对应关系？如何避免OptionKey变化？
        //根据选项返回远见内容，返回null表示未处理，返回""和返回非空字符串同样表示结果有效
        public static string GetEventText(EventOptionInfo event_info)
        {
            //OptionContent是文本
            //OptionKey形如Option_14116135，在上一级的TaiwuEvent_XXX的构造函数中的EventOptions
            //我就是要用if不用switch,啦啦啦
            var optionKey=event_info.OptionKey;
            var result = "";
            if (optionKey == "Option_698382396")//bad63f08-115a-45aa-970c-fa203dd85e2b Option_12 : （背恩绝情……）
            {
                //Character.ApplyBreakupWithBoyOrGirlFriend
                result += ToInfo("结束我方对对方的爱慕关系")
                    + ToInfo("降低我方心情")
                    + ToInfo("可能结束对方对我方的爱慕关系")
                    + ToInfo("降低对方心情", 2)
                    + ToInfo("双向降低好感")
                    + ToInfo("产生秘闻")
                    + ToInfo("我方可能产生爱慕需求")
                    + ToInfo("可能变为敌人");
            }
            else
                return null;
            return result;
        }
        //选项左侧的问号，只有一个，如果原有文本则接在后面
        [HarmonyPostfix, HarmonyPatch(typeof(UI_EventWindow), "ProcessSpecialEventOption")]
        public static void ProcessSpecialEventOptionPatch(UI_EventWindow __instance, EventOptionInfo info, Refers refers)
        {
            if (!On)
                return;
            if (!__instance)
                return;
            if (refers == null)
                return;
            var tip = refers.CGet<MouseTipDisplayer>("OptionHelp");
            if(tip != null)
            {
                var text=GetEventText(info);
                if(text == null)//为""时仍然替换
                    return ;
                if (tip.PresetParam == null)
                    tip.PresetParam = new string[2]{"远见发动",text};
                else if (tip.PresetParam.Length > 1)
                {
                    tip.PresetParam[1] += "\n\n"+text;
                    tip.PresetParam[0] = "(远见)"+ tip.PresetParam[0];
                }
                tip.NeedRefresh = true;
                tip.gameObject.SetActive(true);
                /*
                UnityEngine.Debug.Log("SSS");
                UnityEngine.Debug.Log(info);
                UnityEngine.Debug.Log(info.OptionContent);
                UnityEngine.Debug.Log(info.OptionKey);
                UnityEngine.Debug.Log("EEE");*/
            }
        }
    }
}
