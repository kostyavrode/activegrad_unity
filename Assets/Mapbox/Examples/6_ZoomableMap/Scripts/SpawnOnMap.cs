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

		public void SpawnObjects()
		{
			if (_spawnedObjects != null)
			{
				for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
				{
					var obj = _spawnedObjects[i];
					if (obj != null)
					{
						Destroy(obj);
					}
					_spawnedObjects.RemoveAt(i);
				}
			}

			_spawnedObjects = new List<GameObject>();
			var allLocations = new List<Vector2d>();
			var allObjects = new List<GameObject>();

			int sightCount = _locationStrings != null ? _locationStrings.Length : 0;
			for (int i = 0; i < sightCount; i++)
			{
				if (pageIds == null || i >= pageIds.Length)
					continue;

				var locationString = _locationStrings[i];
				var location = Conversions.StringToLatLon(locationString);
				allLocations.Add(location);
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(location, true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				MapShadowHelper.EnableCastShadows(instance.transform);
				allObjects.Add(instance);
				SightObject so=instance.AddComponent<SightObject>();
				so.SetPageID(pageIds[i]);
			}

			int storeCount = partnerStoreLocationStrings != null ? partnerStoreLocationStrings.Length : 0;
			for (int i = 0; i < storeCount; i++)
			{
				if (partnerStoreIds == null || i >= partnerStoreIds.Length)
					continue;

				var locationString = partnerStoreLocationStrings[i];
				var location = Conversions.StringToLatLon(locationString);
				allLocations.Add(location);

				var prefab = _partnerStoreMarkerPrefab != null ? _partnerStoreMarkerPrefab : _markerPrefab;
				var instance = Instantiate(prefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(location, true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				MapShadowHelper.EnableCastShadows(instance.transform);
				allObjects.Add(instance);

				var storeObject = instance.AddComponent<PartnerStoreObject>();
				storeObject.SetStoreID(partnerStoreIds[i]);
			}

			_locations = allLocations.ToArray();
			_spawnedObjects = allObjects;
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
				spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			}
		}
	}
}