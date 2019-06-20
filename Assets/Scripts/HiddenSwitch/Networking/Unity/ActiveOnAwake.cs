using UnityEngine;
using UnityEngine.EventSystems;

namespace HiddenSwitch.Networking.Unity
{
    public class ActiveOnAwake : UIBehaviour
    {
        [SerializeField] private bool m_SetActive;

        protected override void Awake()
        {
            gameObject.SetActive(m_SetActive);
        }
    }
}