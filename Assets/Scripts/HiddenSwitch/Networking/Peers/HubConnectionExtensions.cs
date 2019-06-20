using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace HiddenSwitch.Networking.Peers
{
    public static class HubConnectionExtensions
    {
        public static string GetConnectionId(this HubConnection hubConnection)
        {
            const string connectionStateField = "_connectionState";
            const string connectionStateConnectionProperty = "Connection";
            const string connectionStateFieldType = "ConnectionState";
            var connectionState = typeof(HubConnection).GetField(connectionStateField,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(hubConnection);
            var connectionStateClass = typeof(HubConnection)
                .GetNestedType(connectionStateFieldType, BindingFlags.NonPublic);
            var connectionContext = connectionStateClass.GetProperty(connectionStateConnectionProperty,
                        BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(connectionState)
                as ConnectionContext;
            return connectionContext.ConnectionId;
        }

        public static string Path()
        {
            return "/i";
        }
    }
}