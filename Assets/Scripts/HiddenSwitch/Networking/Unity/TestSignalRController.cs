using System.Threading.Tasks;
using HiddenSwitch.Networking.Peers;
using HiddenSwitch.Networking.Peers.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HiddenSwitch.Networking.Unity
{
    public class TestSignalRController : UIBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            var testApp = SignalRServer<TestHub>.Create<TestInjectedClass>();

            testApp
                .StartAsync()
                .ToObservable()
                .ContinueWith(v1 =>
                {
                    var hubConnection = new HubConnectionBuilder()
                        .WithUrl("http://localhost:8001/i")
                        .Build();

                    return hubConnection
                        .StartAsync()
                        .ToObservable()
                        .Select(ignored => hubConnection)
                        .Do(hub => { Debug.Log($"connectionId={hub.GetConnectionId()}"); });
                })
                .SelectMany(connection => connection
                    .InvokeAsync<string>(nameof(TestHub.TestInjectedClassCall))
                    .ToObservable())
                .Do(res => { Debug.Log($"res={res}"); })
                .Take(1)
                .Subscribe()
                .AddTo(this);

            testApp.AddTo(this);
        }


        public class TestInjectedClass
        {
            private IHubContext<TestHub> hub { get; }
            public string Suffix { get; set; } = "test2";

            public TestInjectedClass(IHubContext<TestHub> hub)
            {
                this.hub = hub;
            }
        }

        public class TestHub : Hub
        {
            private readonly TestInjectedClass m_TestInjectedClass;

            public TestHub(TestInjectedClass testInjectedClass)
            {
                m_TestInjectedClass = testInjectedClass;
            }

            public async Task<string> Test(string input)
            {
                return await Task.FromResult(input + "-ok");
            }

            public async Task<string> TestInjectedClassCall()
            {
                return await Task.FromResult($"{Context.ConnectionId}-{m_TestInjectedClass.Suffix}");
            }
        }
    }
}