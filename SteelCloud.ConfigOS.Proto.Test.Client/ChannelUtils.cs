using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace SteelCloud.ConfigOS.Proto.Test.Client
{
    public static class ChannelUtils
    {
        public static Channel CreateInsecureChannel(string pAddress)
        {
            Channel channel = new Channel(pAddress, ChannelCredentials.Insecure);
            return channel;
        }
        public static GrpcChannel CreateAuthChannel(string pToken, string pAddress)
        {
            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                if (!string.IsNullOrEmpty(pToken))
                {
                    metadata.Add("Authorization", $"Bearer {pToken}");
                }
                return Task.CompletedTask;
            });

            var channel = GrpcChannel.ForAddress($"https://{pAddress}", new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
            return channel;
        }
    }
}