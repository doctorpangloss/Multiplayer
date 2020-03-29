using System.Collections.Generic;
using System.Linq;
using HiddenSwitch.Networking.Peers;
using HiddenSwitch.Networking.Peers.Internal;
using UniRx;
using UniRx.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;

#endif

namespace HiddenSwitch.Networking.Unity
{
    public class MultiDisplayMultiplayerController : UIBehaviour
    {
        public enum NetworkingType
        {
            NoNetworking,
            LocalHosted
        }

        [SerializeField] private NetworkingType m_NetworkingType;
        [SerializeField] private string m_Url = "http://localhost:8001";
        [SerializeField] private PeerController m_PeerControllerPrefab;
        [Range(1, 2)] [SerializeField] private int m_PlayerCount;

        [SerializeField] private bool m_MatchmakeOnStart;

        [Tooltip(
            "This vector specifies how much to displace a game scene controller to isolate it from everything else")]
        [SerializeField]
        private Vector3 m_IsolationDisplacement;

        private IList<PeerController> displayControllers { get; } = new List<PeerController>();

        protected override void Start()
        {
            switch (m_NetworkingType)
            {
                case NetworkingType.NoNetworking:
                    StartMatchmaking();
                    break;
                case NetworkingType.LocalHosted:
                    var serverPeer = new NetworkedServerPeer(m_Url);
                    serverPeer.AddTo(this);

                    serverPeer.AwakeAsObservable()
                        .Take(1)
                        .Subscribe(ignored => { StartMatchmaking(); })
                        .AddTo(this);
                    break;
            }
        }

        private void StartMatchmaking()
        {
            for (var i = 0; i < m_PlayerCount; i++)
            {
                var controller = Instantiate(m_PeerControllerPrefab, i * m_IsolationDisplacement,
                    Quaternion.identity);

                if (controller.camera != null)
                {
                    controller.camera.targetDisplay = i;
                }

                if (controller.canvas != null)
                {
                    controller.canvas.targetDisplay = i;
                    controller.canvas.sortingOrder = i;
                }

                if (Display.displays.Length > i)
                {
                    Display.displays[i].Activate();
                }


                switch (m_NetworkingType)
                {
                    case NetworkingType.LocalHosted:
                        controller.peer = new NetworkedClientPeer(m_Url);
                        break;
                    case NetworkingType.NoNetworking:
                        controller.peer = new ApplicationDomainPeer();
                        break;
                }

                controller.peer.AwakeAsObservable()
                    .DoOnError(ex => Debug.LogError(ex.Message))
                    .Subscribe().AddTo(this);
                displayControllers.Add(controller);
            }

            if (m_MatchmakeOnStart)
            {
                foreach (var controller in displayControllers)
                {
                    controller.peer.Matchmake()
                        .ObserveOnMainThread()
                        .Subscribe(res => { })
                        .AddTo(this);
                }
            }
        }

#if UNITY_EDITOR
        [SerializeField] private EventSystem m_EventSystem;
        private int m_LastTargetDisplay = -1;

        protected override void OnValidate()
        {
        }

        private void Update()
        {
            var mouseFocusedWindow = EditorWindow.focusedWindow;
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");

            var displayId = 0;
            if (type.IsInstanceOfType(mouseFocusedWindow))
            {
                var displayField = type.GetField("m_TargetDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
                if (displayField == null)
                {
                    return;
                }
                displayId = (int) displayField.GetValue(mouseFocusedWindow);
            }

            if (displayId != m_LastTargetDisplay)
            {
                var hasFocus =
                    typeof(EventSystem).GetField("m_HasFocus", BindingFlags.NonPublic | BindingFlags.Instance);
                hasFocus.SetValue(m_EventSystem, true);
                for (var i = 0; i < displayControllers.Count; i++)
                {
                    displayControllers[i].canvas.sortingOrder = i == displayId ? 1 : 0;
                }
            }

            m_LastTargetDisplay = displayId;
        }
#endif
    }
}