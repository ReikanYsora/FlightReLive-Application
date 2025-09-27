using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.OpenMapTile
{
    public class BuildingPool : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private GameObject _openMapTileZonePrefab;
        [SerializeField] private int _initialSize = 10000;
        private Queue<GameObject> _pool = new();
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                GameObject obj = Instantiate(_openMapTileZonePrefab);
                obj.transform.SetParent(transform);
                _pool.Enqueue(obj);
            }
        }
        #endregion

        #region METHODS
        public GameObject Get()
        {
            if (_pool.Count > 0)
            {
                GameObject obj = _pool.Dequeue();
                return obj;
            }

            GameObject newObj = Instantiate(_openMapTileZonePrefab);

            return newObj;
        }

        public void Return(GameObject obj)
        {
            Destroy(obj.GetComponent<MeshFilter>().sharedMesh);
            _pool.Enqueue(obj);
        }
        #endregion
    }
}
