using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;
using SteelCloud.ConfigOS.Protos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;

namespace SteelCloud.ConfigOS.Proto.Test.Server
{
    public class Program
    {
        const int Port = 30051;
        private static UserMgmt _userMgmt = new UserMgmt();
        private static readonly JwtSecurityTokenHandler m_jwtTokenHandler = new JwtSecurityTokenHandler();
        private static readonly SymmetricSecurityKey m_securityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());

        public static void Main(string[] args)
        {
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Ports    = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Services.Add(ClientStartupService.BindService(new ClientStartImpl()) );
            server.Services.Add(ClientDataService.BindService(new ClientDataImpl()) );
            server.Start();

            Console.WriteLine("Listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }

        class ClientDataImpl : ClientDataService.ClientDataServiceBase
        {
            [Authorize(Roles = "ADMIN, POWER_USER")]
            public override Task<G_EditResponse> BeginEdit(G_EditRequest request, ServerCallContext context)
            {
                try
                {
                    return Task.FromResult(new G_EditResponse() { });
                }
                catch (Exception e)
                {
                    return Task.FromResult(new G_EditResponse()
                    {
                        Errormessage = new G_EditErrorMessage() {Message = e.Message}
                    });
                }

            }

            [Authorize(Roles = "READER")]
            public override Task<G_TreeItemDataResponse> GetTreeItemData(G_TreeItemDataRequest request, ServerCallContext context)
            {
                var reply = new G_TreeItemDataResponse();
                switch (request.RequestCase)
                {
                    case G_TreeItemDataRequest.RequestOneofCase.Endpointrequest:
                        reply.Endpointresponse = new G_GetEndpointResponse() { };
                        break;
                    case G_TreeItemDataRequest.RequestOneofCase.Grouprequest:
                        reply.Groupresponse = new G_GetGroupResponse() { };
                        break;
                }
                return Task.FromResult(reply);
            }
        }

        class ClientStartImpl : ClientStartupService.ClientStartupServiceBase
            {
                public override Task<G_AuthResponse> CheckCredentials(G_AuthRequest request, ServerCallContext context)
                {
                    var repo = _userMgmt.UserRepo();
                    var authUser = repo.FirstOrDefault(o => o.Username == request.Username && o.Password == request.Password);

                    List<Claim> claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.Name, request.Username));
                    
                    if (authUser != null)
                    {
                        
                        foreach (var role in authUser.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                        }
                        var credentials = new SigningCredentials(m_securityKey, SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken(
                            "BroadcastServer",
                            "ExampleClients",
                            claims.ToArray(),
                            expires: DateTime.Now.AddSeconds(60),
                            signingCredentials: credentials);

                        var reply = new G_AuthSuccess{BearerToken = m_jwtTokenHandler.WriteToken(token)};
                        foreach (var claim in claims)
                        {
                            if (claim.Type == ClaimTypes.Role)
                            {
                                reply.UserRoles.Add(claim.Value);
                            }
                        }

                        return Task.FromResult(new G_AuthResponse() {Success = reply});
                    }
                    else
                    {
                        return Task.FromResult(new G_AuthResponse() {Failure = new Empty()});
                    }
                }
                [Authorize]
                public override Task<Empty> CheckServerConnection(Empty request, ServerCallContext context)
                {
                    return Task.FromResult<Empty>(new Empty());
                }

                [Authorize]
                public override Task<G_PairCheckResponse> CheckAssociation(G_PairCheckRequest request, ServerCallContext context)
                {
                    var reply = new G_PairCheckResponse();
                    try
                    {
                        reply.Success = new PairCheckSuccess();
                    }
                    catch (Exception e)
                    {
                        reply.Failure = new PairCheckFailure() {Message = e.Message};
                    }
                    return Task.FromResult<G_PairCheckResponse>(reply);
                }

                [Authorize]
                public override Task<G_LicenseResponse> GetLicenseState(Empty request, ServerCallContext context)
                {
                    return Task.FromResult<G_LicenseResponse>(new G_LicenseResponse()
                    {
                        StartDate = DateTime.UtcNow.ToString(CultureInfo.CurrentCulture), 
                        EndDate = DateTime.UtcNow.AddYears(1).ToString(CultureInfo.CurrentCulture),
                        Assignee = "",
                        Purchaser = "",
                        MetaData = "",
                        LicenseSerialHash = ""
                    });
                }

                [Authorize]
                public override Task<G_GetTreeDataResponse> GetTree(Empty request, ServerCallContext context)
                {
                    return Task.FromResult<G_GetTreeDataResponse>(new G_GetTreeDataResponse()
                    {
                        Items = {  }
                    });
                }


        }
    }
}