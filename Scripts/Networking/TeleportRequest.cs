using Unity.Entities;
using Unity.NetCode;

namespace WormholePotion.Networking {
	public struct TeleportRequest : IRpcCommand {
		public Entity TargetPlayer;
	}
}