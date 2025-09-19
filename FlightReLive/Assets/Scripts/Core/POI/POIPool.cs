using System.Collections.Generic;
using UnityEngine;

namespace FlightReLive.Core.POI
{
    public class POIPool : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private GameObject _poiPrefab;
        [SerializeField] private int _initialSize = 100;
        private Queue<GameObject> _pool = new();
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                GameObject obj = GameObject.Instantiate(_poiPrefab, _mainCanvas.transform);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
        #endregion

        #region METHODS
        public GameObject Get()
        {
            GameObject obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = Instantiate(_poiPrefab, _mainCanvas.transform);
            }

            obj.SetActive(true);

            return obj;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
        #endregion
    }
}
