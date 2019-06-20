using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HiddenSwitch.Networking.Unity
{
    public class DisplayAwareGraphicRaycaster : BaseRaycaster
    {
        [SerializeField] private GraphicRaycaster m_GraphicRaycaster;
        [SerializeField] private Camera m_Camera;

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            m_GraphicRaycaster.Raycast(eventData, resultAppendList);
            foreach (var res in resultAppendList)
            {
                Debug.Log(res.sortingLayer);
            }
        }

        public override Camera eventCamera => m_Camera;
    }
}