using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Ai;
using GameData.Domains.Character.AvatarSystem;
using GameData.Domains.Combat;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Domains.SpecialEffect;
using GameData.Domains.TaiwuEvent;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.GameDataBridge;
using GameData.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TaiwuModdingLib.Core.Plugin;

namespace Taiwu_foresight_backend
{
    [PluginConfig("Taiwu_foresight_backend", "xyzkljl1", "1.0.0")]
    public partial class ForesightBackend : TaiwuRemakePlugin
    {
        public readonly static ushort MY_MAGIC_NUMBER_GET_PANTU = 7643;
        public static bool On;
        Harmony harmony;
        public static readonly List<string> AdventureParameterKeys=new List<string>
        {
            "slip",
            "surrender",
            "dies",
            "successRate",
            "perSuccessRate",
            "neiQiDegree1",
            "neiQiDegree2",
            "neiQiDegree3",
            "D0",
            "minValue",
            "perValue",
            "triggerCounter",
            "step",
            "perPoison",
            "getItem",
            "successRateEx",
            "valueNeed"

        };
        public override void Dispose()
        {
            if (harmony != null)
                harmony.UnpatchSelf();
        }
        public override void Initialize()
        {
            AdaptableLog.Info("ForesightBackend:Init");
            harmony = Harmony.CreateAndPatchAll(typeof(ForesightBackend));
        }
        public override void OnModSettingUpdate()
        {
            DomainManager.Mod.GetSetting(ModIdStr, "On", ref On);
            AdaptableLog.Info(String.Format("ForesightBackend:Load Setting, ForesightBackend {0}", On ? "开启" : "关闭"));
        }
        //借用CharacterDomain的响应
        [HarmonyPrefix, HarmonyPatch(typeof(CharacterDomain), "CallMethod")]
        public static bool CharacterDomainCallMethodPatch(CharacterDomain __instance,ref int __result, Operation operation, RawDataPool argDataPool, RawDataPool returnDataPool, DataContext context)
        {
            if(!On)
                return true;
            if(operation.MethodId== MY_MAGIC_NUMBER_GET_PANTU)
            {
                AdaptableLog.Info("Foresight:Response Query");
                var results = new List<int> ();
                foreach (var key in AdventureParameterKeys)
                    results.Add(EventHelper.GetAdventureParameter(key));//-1表示未取到
                var sectId = EventHelper.GetCurrentAdventureSiteInitData();
                results.Add(sectId);
                __result = GameData.Serializer.Serializer.Serialize(results, returnDataPool);
                return false;
            }
            return true;
        }
        /*
        [HarmonyPrefix, HarmonyPatch(typeof(EventArgBox), "GetAdventureMajorCharacter")]
        public static void GetAdventureMajorCharacterPatch(EventArgBox __instance, int group, int index)
        {
            //debug用
            var charId =0;
            Dictionary<int, Character> dictionary = (Dictionary<int, Character>)Traverse.Create(DomainManager.Character).Field("_objects").GetValue();
            foreach (Character character in dictionary.Values)
            if(character.GetCreatingType()!=2&&character.GetCreatingType()!=3&&character.GetId()!=DomainManager.Taiwu.GetTaiwuCharId())
            {
                    charId=character.GetId();
                    break;
            }
            __instance.Set(string.Format("MajorCharacter_{0}_{1}", group, index), charId);
        }*/

    }
}
