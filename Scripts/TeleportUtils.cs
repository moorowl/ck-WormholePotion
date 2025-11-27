using Inventory;
using PlayerState;
using PugMod;
using Unity.Entities;
using Unity.NetCode;
using WormholePotion.Networking;

namespace WormholePotion {
	public static class TeleportUtils {
		public const float HoveredMarkerScale = 2f;
		public const string TeleportedToTerm = "WormholePotion:teleportedTo";
		public const string TeleportToTerm = "WormholePotion:teleportTo";

		public static void CreateClientTeleportRequest(Entity targetPlayer) {
			var ecb = API.Client.World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
			var entity = ecb.CreateEntity();
			ecb.AddComponent<SendRpcCommandRequest>(entity);
			ecb.AddComponent(entity, new TeleportRequest {
				TargetPlayer = targetPlayer
			});
		}
		
		public static bool CanTeleportTo(PlayerController sourcePlayer, PlayerController targetPlayer) {
			if (sourcePlayer == null || targetPlayer == null)
				return false;
			
			// Disallow teleporting to yourself
			if (sourcePlayer == targetPlayer)
				return false;
			
			// Disallow teleporting in guest mode
			if (sourcePlayer.adminPrivileges < 0)
				return false;
			
			// Disallow teleporting in guest mode or to a player on another team
			if (sourcePlayer.pvpMode && !sourcePlayer.IsPlayersOfSamePvPTeam(targetPlayer))
				return false;
			
			// Disallow teleporting if either player's state is locked or they're using a boat
			var sourcePlayerState = EntityUtility.GetComponentData<PlayerStateCD>(sourcePlayer.entity, sourcePlayer.world);
			var targetPlayerState = EntityUtility.GetComponentData<PlayerStateCD>(targetPlayer.entity, targetPlayer.world);
			if (sourcePlayerState.isStateLocked || targetPlayerState.isStateLocked ||  targetPlayerState.HasAnyState(PlayerStateEnum.BoatRiding))
				return false;
			
			// Disallow teleporting if you don't have a wormhole potion
			if (sourcePlayer.playerInventoryHandler == null || sourcePlayer.playerInventoryHandler.GetExistingAmountOfObject(Main.WormholePotionId) == 0)
				return false;

			return true;
		}
		
		public static bool CanTeleportTo(Entity sourcePlayer, Entity targetPlayer, in InventoryHandlerShared inventoryHandlerShared, in WorldInfoCD worldInfo, in ComponentLookup<PlayerStateCD> playerStateLookup, in ComponentLookup<FactionCD> factionLookup, in ComponentLookup<PlayerGhost> playerGhostLookup, in BufferLookup<ContainedObjectsBuffer> containedObjectsBufferLookup, in BufferLookup<InventoryBuffer> inventoryBufferLookup, in PugDatabase.DatabaseBankCD databaseBank) {
			if (sourcePlayer == Entity.Null || targetPlayer == Entity.Null)
				return false;
			
			// Disallow teleporting to yourself
			if (sourcePlayer == targetPlayer)
				return false;
			
			// Disallow teleporting in guest mode
			if (playerGhostLookup[sourcePlayer].adminPrivileges < 0)
				return false;
			
			// Disallow teleporting in guest mode or to a player on another team
			var sourcePlayerFaction = factionLookup[sourcePlayer];
			var targetPlayerFaction = factionLookup[targetPlayer];
			if (worldInfo.pvpEnabled && sourcePlayerFaction.pvpTeam != targetPlayerFaction.pvpTeam)
				return false;
			
			// Disallow teleporting if either player's state is locked or they're using a boat
			var sourcePlayerState = playerStateLookup[sourcePlayer];
			var targetPlayerState = playerStateLookup[targetPlayer];
			if (sourcePlayerState.isStateLocked || targetPlayerState.isStateLocked ||  targetPlayerState.HasAnyState(PlayerStateEnum.BoatRiding))
				return false;
			
			// Disallow teleporting if you don't have a wormhole potion
			if (InventoryUtility.GetTotalAmount(containedObjectsBufferLookup, inventoryBufferLookup, databaseBank, sourcePlayer, Main.WormholePotionId) == 0)
				return false;

			return true;
		}
	}
}