using System.Collections;
using HarmonyLib;
using Inventory;
using PlayerState;
using Pug.UnityExtensions;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace WormholePotion.Patches {
	[HarmonyPatch]
	public static class MapMarkerUIElement_Patch {
		private static void TryTeleportTo(PlayerController targetPlayer) {
			var sourcePlayer = Manager.main.player;
			if (!TeleportUtils.CanTeleportTo(sourcePlayer, targetPlayer))
				return;

			EntityUtility.PlayEffectEventClient(new EffectEventCD {
				effectID = EffectID.PortalTeleport,
				entity = sourcePlayer.entity,
			});
			TeleportUtils.CreateClientTeleportRequest(targetPlayer.entity);

			if (Manager.ui.isShowingMap)
				Manager.ui.OnMapToggle();
		}
		
		[HarmonyPatch(typeof(MapMarkerUIElement), nameof(MapMarkerUIElement.GetHoverTitle))]
		[HarmonyPostfix]
		private static void GetHoverTitle(MapMarkerUIElement __instance, TextAndFormatFields __result) {
			if (Manager.ui.mapUI.IsShowingBigMap && __instance.markerType == MapMarkerType.Player && TeleportUtils.CanTeleportTo(Manager.main.player, __instance.player ?? Manager.main.player))
				__result.text = TeleportUtils.TeleportToTerm;
		}

		[HarmonyPatch(typeof(MapMarkerUIElement), nameof(MapMarkerUIElement.UpdateColor))]
		[HarmonyPostfix]
		private static void UpdateColor(MapMarkerUIElement __instance) {
			if (__instance.markerType == MapMarkerType.Player) {
				__instance.transform.localScale = Vector3.one;
				
				if (Manager.ui.mapUI.IsShowingBigMap && Manager.ui.currentSelectedUIElement == __instance && TeleportUtils.CanTeleportTo(Manager.main.player, __instance.player ?? Manager.main.player)) {
					__instance.transform.localScale = new Vector3(
						__instance.transform.localScale.x * TeleportUtils.HoveredMarkerScale,
						__instance.transform.localScale.y * TeleportUtils.HoveredMarkerScale,
						1f
					);
				}
			}
		}

		[HarmonyPatch(typeof(MapMarkerUIElement), nameof(MapMarkerUIElement.OnLeftClicked))]
		[HarmonyPostfix]
		private static void OnLeftClicked(MapMarkerUIElement __instance, bool mod1, bool mod2) {
			if (Manager.ui.mapUI.IsShowingBigMap && __instance.markerType == MapMarkerType.Player)
				TryTeleportTo(__instance.player ?? Manager.main.player);
		}
	}
}