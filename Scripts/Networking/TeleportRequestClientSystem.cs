using System.Collections.Generic;
using I2.Loc;
using Pug.UnityExtensions;
using Unity.Entities;
using Unity.NetCode;

namespace WormholePotion.Networking {
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
	public partial class TeleportRequestClientSystem : PugSimulationSystemBase {
		public struct TeleportMessage {
			public string Text;
			public byte Platform;
			public ulong PlatformId;
		}

		public Queue<TeleportMessage> ReceivedMessages = new();

		protected override void OnUpdate() {
			var ecb = CreateCommandBuffer();
			var networkCommSystem = World.GetExistingSystemManaged<NetworkCommSystem>();

			if (networkCommSystem == null)
				return;

			Entities
				.WithAll<ReceiveRpcCommandRequest>()
				.ForEach((Entity requestEntity, in TeleportMessageRequest request) => {
					ReceivedMessages.Enqueue(new TeleportMessage {
						Text = string.Format(LocalizationManager.GetTranslation(TeleportUtils.TeleportedToTerm), request.SourcePlayerName.ToString(), request.TargetPlayerName.ToString()),
						Platform = request.Platform,
						PlatformId = request.PlatformId
					});

					ecb.DestroyEntity(requestEntity);
				})
				.WithoutBurst()
				.Run();
		}
	}
}