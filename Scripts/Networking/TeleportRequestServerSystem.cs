using Inventory;
using PlayerState;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace WormholePotion.Networking {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct TeleportRequestServerSystem : ISystem {
	    public void OnCreate(ref SystemState state) {
		    state.RequireForUpdate<WorldInfoCD>();
		    state.RequireForUpdate<PugDatabase.DatabaseBankCD>();
		    state.RequireForUpdate<SkillTalentsTableCD>();
		    state.RequireForUpdate<UpgradeCostsTableCD>();
		    state.RequireForUpdate<InventoryAuxDataSystemDataCD>();
		    state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
	    }

	    private InventoryHandlerShared _inventoryHandlerShared;
	    private EntityArchetype _teleportedMessageArchetype;
	    private byte _platform;
	    private ulong _platformId;

	    public void OnStartRunning(ref SystemState state) {
		    _inventoryHandlerShared = new InventoryHandlerShared(
			    ref state,
			    SystemAPI.GetSingleton<PugDatabase.DatabaseBankCD>(),
			    SystemAPI.GetSingleton<SkillTalentsTableCD>(),
			    SystemAPI.GetSingleton<UpgradeCostsTableCD>(),
			    SystemAPI.GetSingleton<InventoryAuxDataSystemDataCD>()
		    );
		    _teleportedMessageArchetype = state.EntityManager.CreateArchetype(typeof(TeleportMessageRequest), typeof(SendRpcCommandRequest));
		    _platform = (byte) Manager.platform.Platform;
		    _platformId = Manager.platform.platformImpl.GetPlatformUserID().GetPlatformOnlineId();
	    }

	    public void OnUpdate(ref SystemState state) {
			var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
			
		    SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime);
		    _inventoryHandlerShared.Update(ref state, ecb, networkTime);
            
		    var playerStateLookup = SystemAPI.GetComponentLookup<PlayerStateCD>();
		    var factionLookup = SystemAPI.GetComponentLookup<FactionCD>();
		    var playerGhostLookup = SystemAPI.GetComponentLookup<PlayerGhost>();
		    var containedObjectsLookup = SystemAPI.GetBufferLookup<ContainedObjectsBuffer>();
		    var databaseBank = SystemAPI.GetSingleton<PugDatabase.DatabaseBankCD>();
		    var worldInfo = SystemAPI.GetSingleton<WorldInfoCD>();
		    var wormholePotionId = Main.WormholePotionId;
		    
		    foreach (var (requestSource, request, requestEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<TeleportRequest>>().WithEntityAccess()) {
			    var sourcePlayer = SystemAPI.GetComponent<CommandTarget>(requestSource.ValueRO.SourceConnection).targetEntity;
			    var targetPlayer = request.ValueRO.TargetPlayer;
			    if (sourcePlayer == Entity.Null || targetPlayer == Entity.Null)
				    return;

			    if (!TeleportUtils.CanTeleportTo(sourcePlayer, targetPlayer, _inventoryHandlerShared, worldInfo, playerStateLookup, factionLookup, playerGhostLookup, containedObjectsLookup, databaseBank))
				    return;

			    if (!SystemAPI.HasComponent<PlayerStateCD>(sourcePlayer) || !SystemAPI.HasComponent<TeleportingStateCD>(sourcePlayer) || !SystemAPI.HasComponent<LocalTransform>(targetPlayer))
				    return;
			    
			    var playerState = SystemAPI.GetComponentRW<PlayerStateCD>(sourcePlayer);
			    var teleportingState = SystemAPI.GetComponentRW<TeleportingStateCD>(sourcePlayer);
			    playerState.ValueRW.SetNextState(PlayerStateEnum.Teleporting, nextStateLocked: true);
			    teleportingState.ValueRW.targetPosition = SystemAPI.GetComponentRO<LocalTransform>(targetPlayer).ValueRO.Position;

			    InventoryUtility.ConsumeObject(_inventoryHandlerShared, sourcePlayer, wormholePotionId, 1);

			    var sourcePlayerName = SystemAPI.GetComponentRO<PlayerCustomizationCD>(sourcePlayer).ValueRO.customization.name;
			    var targetPlayerName = SystemAPI.GetComponentRO<PlayerCustomizationCD>(targetPlayer).ValueRO.customization.name;
			    
			    var teleportedMessageEntity = ecb.CreateEntity(_teleportedMessageArchetype);
			    ecb.SetComponent(teleportedMessageEntity, new TeleportMessageRequest {
				    SourcePlayerName = sourcePlayerName,
				    TargetPlayerName = targetPlayerName,
				    Platform = _platform,
				    PlatformId = _platformId
			    });

			    ecb.DestroyEntity(requestEntity);
		    }
	    }
    }
}