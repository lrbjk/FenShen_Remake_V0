using System.Collections.Generic;
using UnityEngine;

namespace FenShen.CombatPrototype
{
    public class CombatPrototypeDecoy : MonoBehaviour
    {
        public float lifetime = 2.5f;

        private float _dieAt;

        public static readonly List<CombatPrototypeDecoy> ActiveDecoys = new List<CombatPrototypeDecoy>();

        void OnEnable()
        {
            _dieAt = Time.time + lifetime;
            ActiveDecoys.Add(this);
        }

        void OnDisable()
        {
            ActiveDecoys.Remove(this);
        }

        void Update()
        {
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
            }
        }
    }
}
