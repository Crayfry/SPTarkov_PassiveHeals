using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using UnityEngine;

namespace PassiveHeals
{
    [BepInPlugin("com.passiveheals.passiveheals", "PassiveHeals", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Player player;

        internal static ConfigEntry<bool> healAll;
        internal static ConfigEntry<float> healValue;
        internal static ConfigEntry<float> healCooldown;

        public static ManualLogSource LogSource;

        private float nextHealTime;

        public void Awake()
        {
            healAll = Config.Bind("Main Section", "Heal All Body Parts", false,
                                  new ConfigDescription(("Heal Every Body Part Passively. If disabled, lowest body part is healed."),
                                  new AcceptableValueRange<bool>(false, true)));

            healValue = Config.Bind("Main Section", "Heal Amount", 1f,
                                  new ConfigDescription(("How much healing is applied."),
                                  new AcceptableValueRange<float>(0f, 1000f)));

            healCooldown = Config.Bind("Main Section", "Heal Cooldown", 10f,
                                  new ConfigDescription(("The delay when the next healing is applied in seconds."),
                                  new AcceptableValueRange<float>(0f, 1000f)));

            LogSource = Logger;
            LogSource.LogInfo("PassiveHeals plugin loaded.");
        }

        private void FixedUpdate()
        {
            if (player == null && Singleton<GameWorld>.Instance?.MainPlayer)
                player = Singleton<GameWorld>.Instance?.MainPlayer;

            if (player != null && player.ActiveHealthController != null && Time.time >= nextHealTime)
            {
                if (healAll.Value)
                    HealAllBodyParts(healValue.Value);
                else
                    HealLowestBodyPart(healValue.Value);
                nextHealTime = Time.time + healCooldown.Value;
            }
        }

        private void HealLowestBodyPart(float value)
        {
            HealBodyPart(GetLowestHPBodyPart(), value);
        }

        private void HealAllBodyParts(float value)
        {
            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
                if (bodyPart != EBodyPart.Common)
                    player.ActiveHealthController.ChangeHealth(bodyPart, value, new DamageInfo());
            LogSource.LogInfo("Healed all body parts for " + value);
        }

        private void HealBodyPart(EBodyPart bodyPart, float value)
        {
            player.ActiveHealthController.ChangeHealth(bodyPart, value, new DamageInfo());
            LogSource.LogInfo("Healed " + bodyPart + " for " + value);
        }

        private EBodyPart GetLowestHPBodyPart()
        {
            EFT.HealthSystem.ValueStruct lowest = player.ActiveHealthController.GetBodyPartHealth(EBodyPart.Head);
            EBodyPart lowestBody = EBodyPart.Head;
            foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
            {
                if (bodyPart != EBodyPart.Common)
                {
                    EFT.HealthSystem.ValueStruct current = player.ActiveHealthController.GetBodyPartHealth(bodyPart);
                    if (!current.AtMinimum && current.Current != 0f && current.Normalized < lowest.Normalized)
                    {
                        lowest = player.ActiveHealthController.GetBodyPartHealth(bodyPart);
                        lowestBody = bodyPart;
                    }
                }
            }
            return lowestBody;
        }
    }
}
