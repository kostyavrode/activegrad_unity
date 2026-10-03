namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;

	public class SpawnOnMap : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		[Geocode]
		public string[] _locationStrings;

		public int[] pageIds;
		[Geocode]
		public string[] partnerStoreLocationStrings;
		public int[] partnerStoreIds;
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 100f;

		[SerializeField]
		GameObject _markerPrefab;
		[SerializeField]
		GameObject _partnerStoreMarkerPrefab;

		List<GameObject> _spawnedObjects;

		public void Spawn()
		{
			_locations = new Vector2d[_locationStrings.Length];
			_spawnedObjects = new List<GameObject>();
			for (int i = 0; i < _locationStrings.Length; i++)
			{
				var locationString = _locationStrings[i];
				_locations[i] = Conversions.StringToLatLon(locationString);
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				_spawnedObjects.Add(instance);
			}
		}

		// Маркеры по ключу ("S<pageId>" / "P<storeId>"): SightsUpdater вызывает SpawnObjects каждые 15–30 с,
		// и пересоздание всех маркеров (с партиклами и эффектами) давало заметный рывок на телефонах.
		// Теперь существующие маркеры переиспользуются, создаются только новые, удаляются только пропавшие.
		readonly Dictionary<string, GameObject> _spawnedByKey = new Dictionary<string, GameObject>();

		public void SpawnObjects()
		{
			var allLocations = new List<Vector2d>();
			var allObjects = new List<GameObject>();
			var aliveKeys = new HashSet<string>();

			int sightCount = _locationStrings != null ? _locationStrings.Length : 0;
			for (int i = 0; i < sightCount; i++)
			{
				if (pageIds == null || i >= pageIds.Length)
					continue;

				var location = Conversions.StringToLatLon(_locationStrings[i]);
				var key = "S" + pageIds[i];
				if (!aliveKeys.Add(key))
					continue;

				var instance = GetOrCreate(key, _markerPrefab, out var created);
				PlaceMarker(instance, location);
				if (created)
				{
					SightObject so = instance.AddComponent<SightObject>();
					so.SetPageID(pageIds[i]);
				}

				allLocations.Add(location);
				allObjects.Add(instance);
			}

			int storeCount = partnerStoreLocationStrings != null ? partnerStoreLocationStrings.Length : 0;
			for (int i = 0; i < storeCount; i++)
			{
				if (partnerStoreIds == null || i >= partnerStoreIds.Length)
					continue;

				var location = Conversions.StringToLatLon(partnerStoreLocationStrings[i]);
				var key = "P" + partnerStoreIds[i];
				if (!aliveKeys.Add(key))
					continue;

				var prefab = _partnerStoreMarkerPrefab != null ? _partnerStoreMarkerPrefab : _markerPrefab;
				var instance = GetOrCreate(key, prefab, out var created);
				PlaceMarker(instance, location);
				if (created)
				{
					var storeObject = instance.AddComponent<PartnerStoreObject>();
					storeObject.SetStoreID(partnerStoreIds[i]);
				}

				allLocations.Add(location);
				allObjects.Add(instance);
			}

			// Удаляем только маркеры, которых больше нет в списке.
			var staleKeys = new List<string>();
			foreach (var pair in _spawnedByKey)
			{
				if (!aliveKeys.Contains(pair.Key))
					staleKeys.Add(pair.Key);
			}
			for (int i = 0; i < staleKeys.Count; i++)
			{
				var obj = _spawnedByKey[staleKeys[i]];
				if (obj != null)
					Destroy(obj);
				_spawnedByKey.Remove(staleKeys[i]);
			}

			_locations = allLocations.ToArray();
			_spawnedObjects = allObjects;
		}

		GameObject GetOrCreate(string key, GameObject prefab, out bool created)
		{
			if (_spawnedByKey.TryGetValue(key, out var existing) && existing != null)
			{
				created = false;
				return existing;
			}

			var instance = Instantiate(prefab);
			instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			MapShadowHelper.EnableCastShadows(instance.transform);
			_spawnedByKey[key] = instance;
			created = true;
			return instance;
		}

		void PlaceMarker(GameObject instance, Vector2d location)
		{
			instance.transform.localPosition = _map.GeoToWorldPosition(location, true);
			CurvedWorldController.Bend(instance.transform);
			instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
		}

		private void Update()
		{
			if (_spawnedObjects == null)
			{
				return;
			}
			int count = _spawnedObjects.Count;
			for (int i = 0; i < count; i++)
			{
				var spawnedObject = _spawnedObjects[i];
				var location = _locations[i];
				spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
				// Маркер опускается вместе с «загнутой» картой (шейдер StylizedMatcap), иначе висит в воздухе.
				CurvedWorldController.Bend(spawnedObject.transform);
				spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			}
		}
	}
}