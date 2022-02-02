using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using SteelCloud.ConfigOS.Protos;

namespace SteelCloud.ConfigOS.Proto.Test.Client
{
    class Program
    {
        private static string address = "127.0.0.1:30051";
        private static bool _isAuth = false;
        private static string _bearerToken = String.Empty;
        private static GrpcChannel _authChannel;

        
        public static async Task Main(string[] args)
        {
            //DO Authorization
            while (_isAuth == false)
            {            
                Channel channel = new Channel(address, ChannelCredentials.Insecure);
                var client = new ClientStartupService.ClientStartupServiceClient(channel);
                Console.Write("Username: ");
                var username = Console.ReadLine();
                Console.Write("Password: ");
                var password = Console.ReadLine();
                await DoAuthentication(username, password, client);
            }

            //DO Client Data
            if (_isAuth)
            {
                _authChannel = CreateAuthChannel(_bearerToken);
                var authClientDataServiceClient = new ClientDataService.ClientDataServiceClient(_authChannel);
                var authClientStartupServiceClient = new ClientStartupService.ClientStartupServiceClient(_authChannel);

                Console.WriteLine("1. GetTreeItemData - Endpoint");
                Console.WriteLine("2. GetTreeItemData - Group");
                Console.WriteLine("3. BeginEdit - Endpoint");
                Console.WriteLine("4. BeginEdit - Group");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("5. CheckServerConnection");
                Console.WriteLine("6. CheckAssociation");
                Console.WriteLine("7. GetLicenseState");
                Console.WriteLine("8. GetTree");
                Console.WriteLine("X. Quit");
                
                
                var input = new ConsoleKeyInfo();
                while (input.Key != ConsoleKey.X)
                {
                    
                    input = Console.ReadKey();
                    switch (input.Key)
                    {
                        case ConsoleKey.D1:
                            Console.WriteLine(" - GetTreeItemData - Endpoint");
                            await DoGetTreeItemData(authClientDataServiceClient, DataRequestType.Endpoint);
                            break;
                        case ConsoleKey.D2:
                            Console.WriteLine(" - GetTreeItemData - Group");
                            await DoGetTreeItemData(authClientDataServiceClient, DataRequestType.Group);
                            break;
                        case ConsoleKey.D3:
                            Console.WriteLine(" - BeginEdit - Endpoint");
                            await DoBeginEdit(authClientDataServiceClient, DataRequestType.Endpoint);
                            break;
                        case ConsoleKey.D4:
                            Console.WriteLine(" - BeginEdit - Group");
                            await DoBeginEdit(authClientDataServiceClient, DataRequestType.Group);
                            break;
                        
                        
                        case ConsoleKey.D5:
                            Console.WriteLine(" - CheckServerConnection");
                            await DoCheckServerConnection(authClientStartupServiceClient);
                            break;
                        case ConsoleKey.D6:
                            Console.WriteLine(" - CheckAssociation");
                            await DoCheckAssociation(authClientStartupServiceClient);
                            break;
                        case ConsoleKey.D7:
                            Console.WriteLine(" - GetLicenseState");
                            await DoGetLicenseState(authClientStartupServiceClient);
                            break;
                        case ConsoleKey.D8:
                            Console.WriteLine(" - GetTree");
                            await DoGetTree(authClientStartupServiceClient);
                            break;
                        case ConsoleKey.X:
                            Console.WriteLine(" - Shut Down");
                            break;
                    }
                }

                _authChannel.ShutdownAsync().Wait();
            }
            Console.ReadKey();
        }


        private static async Task DoCheckServerConnection(ClientStartupService.ClientStartupServiceClient client)
        {
            var reply = client.CheckServerConnection(new Empty());
        }
        private static async Task DoCheckAssociation(ClientStartupService.ClientStartupServiceClient client)
        {
            var request = new G_PairCheckRequest()
            {
                Pubkey = ByteString.CopyFrom("TEST STRING", Encoding.Unicode)
            };
            var reply = client.CheckAssociation(request);
        }
        private static async Task DoGetLicenseState(ClientStartupService.ClientStartupServiceClient client)
        {
            var reply = client.GetLicenseState(new Empty());
        }
        private static async Task DoGetTree(ClientStartupService.ClientStartupServiceClient client)
        {
            var reply = client.GetTree(new Empty());
        }
        
        private static async Task DoAuthentication(string pUser, string pPass, ClientStartupService.ClientStartupServiceClient client)
        {
            if (pUser == null) throw new ArgumentNullException(nameof(pUser));
            if (pPass == null) throw new ArgumentNullException(nameof(pPass));
            

            var request = new G_AuthRequest()
            {
                Username = pUser, Password = pPass
            };
            var reply = client.CheckCredentials(request);
                
            if (reply.ResponseCase == G_AuthResponse.ResponseOneofCase.Success)
            {
                Console.WriteLine("User Authenticated");
                Console.WriteLine($"-- Roles: {reply.Success.UserRoles.ToString()} ");
                _isAuth = true;
                _bearerToken = reply.Success.BearerToken;
            }
            else
            {
                Console.WriteLine("Authenticated Failed");
            }
        }
       
        private static async Task DoGetTreeItemData(ClientDataService.ClientDataServiceClient client, DataRequestType type)
        {            
            var request = new G_TreeItemDataRequest()
            {
                Id = 1
            };
            switch (type)
            {
                case DataRequestType.Endpoint:
                    request.Endpointrequest = new G_GetEndpointRequest(){ };
                    break;
                case DataRequestType.Group:
                    request.Grouprequest = new G_GetGroupRequest(){ };
                    break;
            }
        }

        private static async Task DoBeginEdit(ClientDataService.ClientDataServiceClient client, DataRequestType type)
        {
            var request = new G_EditRequest()
            {
                Oid = 1
            };
            switch (type)
            {
                case DataRequestType.Endpoint:
                    request.Endpointrequest = new G_EndpointEditRequest() { };
                    break;
                case DataRequestType.Group:
                    request.Grouprequest = new G_GroupEditRequest() { };
                    break;
            }
            var reply = await client.BeginEditAsync(request);
        }

        private static GrpcChannel CreateAuthChannel(string? p_token)
        {
            var credentials = CallCredentials.FromInterceptor((context, metadata) =>
            {
                if (!string.IsNullOrEmpty(p_token))
                {
                    metadata.Add("Authorization", $"Bearer {p_token}");
                }
                return Task.CompletedTask;
            });

            var channel = GrpcChannel.ForAddress($"https://{address}", new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
            });
            return channel;
        }
    }
}