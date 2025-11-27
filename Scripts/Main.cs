using System.Linq;
using PugMod;
using Unity.Entities;
using UnityEngine;
using WormholePotion.Networking;
using RangeInt = Pug.UnityExtensions.RangeInt;

namespace WormholePotion {
	public class Main : IMod {
		public const string Version = "1.0.2";
		public const string InternalName = "WormholePotion";
		public const string DisplayName = "Wormhole Potion";

		internal static ObjectID WormholePotionId { get; private set; }
		
		public void EarlyInit() {
			Debug.Log($"[{DisplayName}] Mod version: {Version}");
		}

		public void Init() {
			WormholePotionId = API.Authoring.GetObjectID("WormholePotion:WormholePotion");

			// Add recipe to the Distillery Table
			API.Authoring.OnObjectTypeAdded += (entity, authoring, manager) => {
				var objectData = manager.GetComponentData<ObjectDataCD>(entity);
				if (objectData.objectID != ObjectID.LaboratoryWorkbench)
					return;

				var canCraftObjects = manager.GetBuffer<CanCraftObjectsBuffer>(entity);
				InsertCraftableObject(canCraftObjects, new CanCraftObjectsBuffer {
					objectID = WormholePotionId,
					amount = 3
				});
			};

			InjectLoot(LootTableID.WorldChest, WormholePotionId, 0.025f, 1, 2);
			InjectLoot(LootTableID.CavelingDestructbile, WormholePotionId, 0.02f);
			InjectLoot(LootTableID.LargeAncientDestructible, WormholePotionId, 0.02f);
			InjectLoot(LootTableID.WorldChestNature, WormholePotionId, 0.05f, 1, 2);
			InjectLoot(LootTableID.MoldDungeonChest, WormholePotionId, 0.06f, 1, 2);
			InjectLoot(LootTableID.SeaBiomeChest, WormholePotionId, 0.0025f, 1, 1);
			InjectLoot(LootTableID.CityDungeonChest, WormholePotionId, 0.0025f, 1, 2);
			InjectLoot(LootTableID.CityDestructible, WormholePotionId, 0.0025f);
			InjectLoot(LootTableID.LargeCityDestructible, WormholePotionId, 0.0025f);
			InjectLoot(LootTableID.DesertDungeonChest, WormholePotionId, 0.05f, 1, 2);
			InjectLoot(LootTableID.LavaDungeonChest, WormholePotionId, 0.05f, 1, 2);
		}

		public void Shutdown() { }

		public void ModObjectLoaded(Object obj) { }

		public void Update() { }
		
		private static void InjectLoot(LootTableID lootTableId, ObjectID objectId, float weight, int minAmount = 1, int maxAmount = 1) {
			var lootTable = Manager.mod.LootTable.FirstOrDefault(x => x.id == lootTableId);
			lootTable?.lootInfos.Add(new LootInfo {
				objectID = objectId,
				weight = weight,
				amount = new RangeInt {
					min = minAmount,
					max = maxAmount
				},
				accumulatedDropChance = 0f // this only seems to be used for the guaranteed roll
			});
		}

		private static void InsertCraftableObject(DynamicBuffer<CanCraftObjectsBuffer> buffer, CanCraftObjectsBuffer craftableObject) {
			for (var i = 0; i < buffer.Length; i++) {
				var existingCraftableObject = buffer[i];
				if (existingCraftableObject.objectID != ObjectID.None)
					continue;
				
				buffer[i] = craftableObject;
				return;
			}

			buffer.Add(craftableObject);
		}
	}
}