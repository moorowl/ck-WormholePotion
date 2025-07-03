using Unity.Collections;
using Unity.NetCode;

namespace WormholePotion.Networking {
	public struct TeleportMessageRequest : IRpcCommand {
		public FixedString32Bytes SourcePlayerName;
		public FixedString32Bytes TargetPlayerName;
		public byte Platform;
		public ulong PlatformId;
	}
}