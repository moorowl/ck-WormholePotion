using System.Linq;
using HarmonyLib;
using PugMod;
using WormholePotion.Networking;
// ReSharper disable InconsistentNaming

namespace WormholePotion.Patches {
	[HarmonyPatch]
	public static class ChatWindow_Patch {
		private static readonly MemberInfo MiRenderText = typeof(ChatWindow).GetMembersChecked().FirstOrDefault(x => x.GetNameChecked() == "RenderText");
		
		[HarmonyPatch(typeof(ChatWindow), "Update")]
		[HarmonyPostfix]
		private static void Update(ChatWindow __instance) {
			if (!__instance.messageTextParent.gameObject.activeSelf)
				return;
			
			var teleportRequestClientSystem = API.Client.World.GetExistingSystemManaged<TeleportRequestClientSystem>();
			if (teleportRequestClientSystem == null)
				return;

			while (teleportRequestClientSystem.ReceivedMessages.TryDequeue(out var message)) {
				var text = message.Text;
				var platformId = message.PlatformId;

				if (message.Platform != 0) {
					Manager.platform.parentalControlManager.CommunicationAllowed(false, success => {
						if (!success)
							return;
					
						Manager.platform.platformImpl.IsUserBlocked(new PlatformUserID(platformId), isBlocked => {
							if (isBlocked)
								return;
						
							Manager.platform.parentalControlManager.RestrictInput(text, filteredText => API.Reflection.Invoke(MiRenderText, __instance, filteredText));
						});
					});	
				} else {
					API.Reflection.Invoke(MiRenderText, __instance, message.Text);
				}
			}
		}
	}
}