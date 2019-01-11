using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.Client;
using Loom.Google.Protobuf;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Protobuf;
using Newtonsoft.Json;
using Plugins.AsyncAwaitUtil.Source;
using UnityEngine;
using System.Text;

namespace Loom.ZombieBattleground.BackendCommunication
{
    public class BackendFacade : IService
    {
        private int _subscribeCount;
        private IRpcClient _reader;
        private IContractCallProxy _contractCallProxy;
        private Func<Contract, IContractCallProxy> _contractCallProxyFactory;

        public delegate void ContractCreatedEventHandler(Contract oldContract, Contract newContract);

        public delegate void PlayerActionDataReceivedHandler(byte[] bytes);

        public event ContractCreatedEventHandler ContractCreated;

        public BackendEndpoint BackendEndpoint { get; set; }

        public Contract Contract { get; private set; }

        public bool IsConnected => Contract != null &&
            Contract.Client.ReadClient.ConnectionState == RpcConnectionState.Connected &&
            Contract.Client.WriteClient.ConnectionState == RpcConnectionState.Connected;

        public ILogger Logger { get; set; } = Debug.unityLogger;

        public bool EnableRpcLogging { get; set; } = false;

        public IContractCallProxy ContractCallProxy => _contractCallProxy;

        public BackendFacade(BackendEndpoint backendEndpoint, Func<Contract, IContractCallProxy> contractCallProxyFactory = null)
        {
            BackendEndpoint = backendEndpoint;
            _contractCallProxyFactory = contractCallProxyFactory;
        }

        public void Init()
        {
            Logger?.Log("Auth Host: " + BackendEndpoint.AuthHost);
            Logger?.Log("Reader Host: " + BackendEndpoint.ReaderHost);
            Logger?.Log("Writer Host: " + BackendEndpoint.WriterHost);
            Logger?.Log("Vault Host: " + BackendEndpoint.VaultHost);
            Logger?.Log("Card Data Version: " + BackendEndpoint.DataVersion);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Contract?.Client?.Dispose();
            _contractCallProxy?.Dispose();
        }

        public async Task CreateContract(
            byte[] privateKey,
            Action<DAppChainClient> onClientCreatedCallback = null,
            IDAppChainClientCallExecutor chainClientCallExecutor = null
            )
        {
            byte[] publicKey = CryptoUtils.PublicKeyFromPrivateKey(privateKey);
            Address callerAddr = Address.FromPublicKey(publicKey);

            ILogger logger = EnableRpcLogging ? Logger ?? NullLogger.Instance : NullLogger.Instance;

            IRpcClient writer =
                RpcClientFactory
                    .Configure()
                    .WithLogger(logger)
                    .WithWebSocket(BackendEndpoint.WriterHost)
                    .Create();

            _reader =
                RpcClientFactory
                    .Configure()
                    .WithLogger(logger)
                    .WithWebSocket(BackendEndpoint.ReaderHost)
                    .Create();

            DAppChainClient client = new DAppChainClient(
                writer,
                _reader,
                callExecutor: chainClientCallExecutor
                )
            {
                Logger = logger
            };

            client.TxMiddleware = new TxMiddleware(new ITxMiddlewareHandler[]
            {
                new NonceTxMiddleware(publicKey, client),
                new SignedTxMiddleware(privateKey)
            });

            client.Configuration.AutoReconnect = false;
            onClientCreatedCallback?.Invoke(client);

            await client.ReadClient.ConnectAsync();
            await client.WriteClient.ConnectAsync();
            Address contractAddr = await client.ResolveContractAddressAsync("ZombieBattleground");
            Contract oldContract = Contract;
            Contract = new Contract(client, contractAddr, callerAddr);

            _contractCallProxy = _contractCallProxyFactory?.Invoke(Contract) ?? new DefaultContractCallProxy(Contract);
            ContractCreated?.Invoke(oldContract, Contract);
        }

        #region Card Collection

        private const string GetCardCollectionMethod = "GetCollection";

        public async Task<GetCollectionResponse> GetCardCollection(string userId)
        {
            GetCollectionRequest request = new GetCollectionRequest
            {
                UserId = userId
            };

            return await _contractCallProxy.StaticCallAsync<GetCollectionResponse>(GetCardCollectionMethod, request);
        }

        #endregion

        #region Card Library

        private const string GetCardLibraryMethod = "ListCardLibrary";

        public async Task<ListCardLibraryResponse> GetCardLibrary()
        {
            ListCardLibraryRequest request = new ListCardLibraryRequest
            {
                Version = BackendEndpoint.DataVersion
            };

            return await _contractCallProxy.StaticCallAsync<ListCardLibraryResponse>(GetCardLibraryMethod, request);
        }

        #endregion

        #region Deck Management

        private const string GetAiDecksDataMethod = "GetAIDecks";

        private const string GetDeckDataMethod = "ListDecks";

        private const string DeleteDeckMethod = "DeleteDeck";

        private const string AddDeckMethod = "CreateDeck";

        private const string EditDeckMethod = "EditDeck";

        public async Task<ListDecksResponse> GetDecks(string userId)
        {
            ListDecksRequest request = new ListDecksRequest
            {
                UserId = userId
            };

            return await _contractCallProxy.StaticCallAsync<ListDecksResponse>(GetDeckDataMethod, request);
        }

        public async Task<GetAIDecksResponse> GetAiDecks()
        {
            GetAIDecksRequest request = new GetAIDecksRequest
            {
                Version = BackendEndpoint.DataVersion
            };

            return await _contractCallProxy.StaticCallAsync<GetAIDecksResponse>(GetAiDecksDataMethod, request);
        }

        public async Task DeleteDeck(string userId, long deckId)
        {
            DeleteDeckRequest request = new DeleteDeckRequest
            {
                UserId = userId,
                DeckId = deckId
            };

            await _contractCallProxy.CallAsync(DeleteDeckMethod, request);
        }

        public async Task EditDeck(string userId, Data.Deck deck)
        {
            EditDeckRequest request = new EditDeckRequest
            {
                UserId = userId,
                Deck = deck.ToProtobuf(),
                Version = BackendEndpoint.DataVersion
            };

            await _contractCallProxy.CallAsync(EditDeckMethod, request);
        }

        public async Task<long> AddDeck(string userId, Data.Deck deck)
        {
            CreateDeckRequest request = new CreateDeckRequest
            {
                UserId = userId,
                Deck = deck.ToProtobuf(),
                Version = BackendEndpoint.DataVersion
            };

            CreateDeckResponse createDeckResponse = await _contractCallProxy.CallAsync<CreateDeckResponse>(AddDeckMethod, request);
            return createDeckResponse.DeckId;
        }

        #endregion

        #region Heroes

        private const string HeroesList = "ListHeroes";

        public async Task<ListHeroesResponse> GetHeroesList(string userId)
        {
            ListHeroesRequest request = new ListHeroesRequest
            {
                UserId = userId
            };

            return await _contractCallProxy.StaticCallAsync<ListHeroesResponse>(HeroesList, request);
        }

        private const string GlobalHeroesList = "ListHeroLibrary";

        public async Task<ListHeroLibraryResponse> GetGlobalHeroesList()
        {
            ListHeroLibraryRequest request = new ListHeroLibraryRequest
            {
                Version = BackendEndpoint.DataVersion
            };

            return await _contractCallProxy.StaticCallAsync<ListHeroLibraryResponse>(GlobalHeroesList, request);
        }

        #endregion

        #region Login

        private const string CreateAccountMethod = "CreateAccount";

        public async Task SignUp(string userId)
        {
            UpsertAccountRequest req = new UpsertAccountRequest
            {
                Version = BackendEndpoint.DataVersion,
                UserId = userId
            };

            await _contractCallProxy.CallAsync(CreateAccountMethod, req);
        }

        #endregion

        #region Auth

        private const string UserInfoEndPoint = "/user/info";

        private const string loginEndPoint = "/auth/email/login";

        private const string signupEndPoint = "/auth/email/game_signup";

        private const string forgottenPasswordEndPoint = "/auth/mlink/generate";

        private const string createVaultTokenEndPoint = "/auth/loom-userpass/create_token";

        private const string accessVaultEndPoint = "/entcubbyhole/loomauth";

        private const string createVaultTokenForNon2FAUsersEndPoint = "/auth/loom-simple-userpass/create_token";

        public async Task<UserInfo> GetUserInfo(string accessToken)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Url = BackendEndpoint.AuthHost + UserInfoEndPoint;
            webrequestCreationInfo.Headers.Add("authorization", "Bearer " + accessToken);

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{nameof(GetUserInfo)} failed with error code {httpResponseMessage.StatusCode}");

            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(
                httpResponseMessage.ReadToEnd(),

                // FIXME: backend should return valid version numbers at all times
                new VersionConverterWithFallback(Version.Parse(Constants.CurrentVersionBase))
            );

            return userInfo;
        }

        public async Task<LoginData> InitiateLogin(string email, string password)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.POST;
            webrequestCreationInfo.Url = BackendEndpoint.AuthHost + loginEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            LoginRequest loginRequest = new LoginRequest();
            loginRequest.email = email;
            loginRequest.password = password;
            webrequestCreationInfo.Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loginRequest));
            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");
            webrequestCreationInfo.Headers.Add("authority", "auth.loom.games");

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{nameof(InitiateLogin)} failed with error code {httpResponseMessage.StatusCode}");

            Debug.Log(httpResponseMessage.ReadToEnd());
            LoginData loginData = JsonConvert.DeserializeObject<LoginData>(
                httpResponseMessage.ReadToEnd());
            return loginData;
        }

        public async Task<RegisterData> InitiateRegister(string email, string password)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.POST;
            webrequestCreationInfo.Url = BackendEndpoint.AuthHost + signupEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            LoginRequest loginRequest = new LoginRequest();
            loginRequest.email = email;
            loginRequest.password = password;
            webrequestCreationInfo.Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loginRequest));
            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");
            webrequestCreationInfo.Headers.Add("authority", "auth.loom.games");

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            Debug.Log(httpResponseMessage.ToString());

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{nameof(InitiateLogin)} failed with error code {httpResponseMessage.StatusCode}");

            RegisterData registerData = JsonConvert.DeserializeObject<RegisterData>(
                httpResponseMessage.ReadToEnd());
            return registerData;
        }

        public async Task<bool> InitiateForgottenPassword(string email)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Url = BackendEndpoint.AuthHost + forgottenPasswordEndPoint + "?email=" + email +"&kind=signup";

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception(
                    $"{nameof(InitiateForgottenPassword)} failed with error code {httpResponseMessage.StatusCode}");
                    
            return true;
        }

        public async Task<CreateVaultTokenData> CreateVaultToken(string otp, string accessToken)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.POST;
            webrequestCreationInfo.Url = BackendEndpoint.VaultHost + createVaultTokenEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            VaultTokenRequest vaultTokenRequest = new VaultTokenRequest();
            vaultTokenRequest.authy_token = otp;
            vaultTokenRequest.access_token = accessToken;

            webrequestCreationInfo.Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vaultTokenRequest));
            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            Debug.Log(httpResponseMessage.ToString());

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{nameof(CreateVaultToken)} failed with error code {httpResponseMessage.StatusCode}");

            CreateVaultTokenData vaultTokenData = JsonConvert.DeserializeObject<CreateVaultTokenData>(
                httpResponseMessage.ReadToEnd());
            return vaultTokenData;
        }

        public async Task<CreateVaultTokenData> CreateVaultTokenForNon2FAUsers(string accessToken)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.POST;
            webrequestCreationInfo.Url = BackendEndpoint.VaultHost + createVaultTokenForNon2FAUsersEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            VaultTokenNon2FARequest vaultTokenRequest = new VaultTokenNon2FARequest();
            vaultTokenRequest.access_token = accessToken;

            webrequestCreationInfo.Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vaultTokenRequest));
            Debug.Log(JsonConvert.SerializeObject(vaultTokenRequest));
            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            Debug.Log(httpResponseMessage.ReadToEnd());

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{nameof(CreateVaultTokenForNon2FAUsers)} failed with error code {httpResponseMessage.StatusCode}");

            CreateVaultTokenData vaultTokenData = JsonConvert.DeserializeObject<CreateVaultTokenData>(
                httpResponseMessage.ReadToEnd());
            return vaultTokenData;
        }

        public async Task<GetVaultDataResponse> GetVaultData(string vaultToken)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.GET;
            webrequestCreationInfo.Url = BackendEndpoint.VaultHost + accessVaultEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");
            webrequestCreationInfo.Headers.Add("X-Vault-Token", vaultToken);

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            Debug.Log(httpResponseMessage.ToString());
            Debug.Log(httpResponseMessage.StatusCode.ToString());
            Debug.Log(httpResponseMessage.StatusCode);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                if (httpResponseMessage.StatusCode.ToString() == Constants.VaultEmptyErrorCode)
                {
                    throw new Exception(httpResponseMessage.StatusCode.ToString());
                }
                else 
                {
                    throw new Exception($"{nameof(GetVaultData)} failed with error code {httpResponseMessage.StatusCode}");
                }
            }

            GetVaultDataResponse getVaultDataResponse = JsonConvert.DeserializeObject<GetVaultDataResponse>(
                httpResponseMessage.ReadToEnd());
            return getVaultDataResponse;
        }

        public async Task<bool> SetVaultData(string vaultToken, string privateKey)
        {
            WebrequestCreationInfo webrequestCreationInfo = new WebrequestCreationInfo();
            webrequestCreationInfo.Method = WebRequestMethod.POST;
            webrequestCreationInfo.Url = BackendEndpoint.VaultHost + accessVaultEndPoint;
            webrequestCreationInfo.ContentType = "application/json;charset=UTF-8";

            VaultPrivateKeyRequest vaultPrivateKeyRequest = new VaultPrivateKeyRequest();
            vaultPrivateKeyRequest.privatekey = privateKey;

            webrequestCreationInfo.Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vaultPrivateKeyRequest));
            webrequestCreationInfo.Headers.Add("accept", "application/json, text/plain, */*");
            webrequestCreationInfo.Headers.Add("X-Vault-Token", vaultToken);

            HttpResponseMessage httpResponseMessage =
                await WebRequestUtils.CreateAndSendWebrequest(webrequestCreationInfo);

            Debug.Log(httpResponseMessage.ToString());

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                throw new Exception($"{nameof(SetVaultData)} failed with error code {httpResponseMessage.StatusCode}");
            }

            return true;
        }

        private struct LoginRequest 
        {
            public string email;
            public string password;
        }

        private struct VaultTokenRequest
        {
            public string authy_token;
            public string access_token;
        }

        private struct VaultTokenNon2FARequest
        {
            public string access_token;
        }

        private struct VaultPrivateKeyRequest
        {
            public string privatekey;
        }

        private struct BetaKeyValidationResponse
        {
            [JsonProperty(PropertyName = "is_valid")]
            public bool IsValid;
        }

        #endregion


        #region VersionCheck

        private const string GetVersionMethod = "GetVersions";

        public async Task<GetVersionsResponse> GetVersions()
        {
            GetVersionsRequest request = new GetVersionsRequest();
            return await _contractCallProxy.StaticCallAsync<GetVersionsResponse>(GetVersionMethod, request);
        }

        #endregion


        #region PVP

        private const string FindMatchMethod = "FindMatch";
        private const string DebugFindMatchMethod = "DebugFindMatch";
        private const string CancelFindMatchMethod = "CancelFindMatch";
        private const string EndMatchMethod = "EndMatch";
        private const string SendPlayerActionMethod = "SendPlayerAction";
        private const string GetGameStateMethod = "GetGameState";
        private const string GetMatchMethod = "GetMatch";
        private const string CheckGameStatusMethod = "CheckGameStatus";
        private const string RegisterPlayerPoolMethod = "RegisterPlayerPool";
        private const string AcceptMatchMethod = "AcceptMatch";
        private const string KeepAliveStatusMethod = "KeepAlive";

        public event PlayerActionDataReceivedHandler PlayerActionDataReceived;

        public async Task<AcceptMatchResponse> AcceptMatch(string userId, long matchId)
        {
            AcceptMatchRequest request = new AcceptMatchRequest
            {
                UserId = userId,
                MatchId = matchId
            };

            return await _contractCallProxy.CallAsync<AcceptMatchResponse>(AcceptMatchMethod, request);
        }

        public async Task<RegisterPlayerPoolResponse> RegisterPlayerPool(
            string userId,
            long deckId,
            Address? customGameModeAddress,
            IList<string> pvpTags,
            bool useBackendGameLogic,
            DebugCheatsConfiguration debugCheats = null)
        {
            if (pvpTags != null && pvpTags.Count != 0)
            {
                Debug.Log("PvPTags: " + String.Join(", ", pvpTags));
            }

            RegisterPlayerPoolRequest request = new RegisterPlayerPoolRequest
            {
                RegistrationData = new PlayerProfileRegistrationData
                {
                    UserId = userId,
                    DeckId = deckId,
                    Version = BackendEndpoint.DataVersion,
                    Tags =
                    {
                        pvpTags ?? Array.Empty<string>()
                    },
                    CustomGame = customGameModeAddress?.ToProtobufAddress(),
                    UseBackendGameLogic = useBackendGameLogic,
                    DebugCheats = debugCheats.ToProtobuf()
                }
            };

            return await _contractCallProxy.CallAsync<RegisterPlayerPoolResponse>(RegisterPlayerPoolMethod, request);
        }

        public async Task<FindMatchResponse> FindMatch(string userId, IList<string> pvpTags)
        {
            if (pvpTags != null && pvpTags.Count != 0)
            {
                Debug.Log("PvPTags: " + String.Join(", ", pvpTags));
            }

            FindMatchRequest request = new FindMatchRequest
            {
                UserId = userId,
                Tags =
                {
                    pvpTags ?? Array.Empty<string>()
                }
            };

            return await _contractCallProxy.CallAsync<FindMatchResponse>(FindMatchMethod, request);
        }

        public async Task<CancelFindMatchResponse> CancelFindMatch(string userId, long matchId)
        {
            CancelFindMatchRequest request = new CancelFindMatchRequest
            {
                UserId = userId,
                MatchId = matchId
            };

            return await _contractCallProxy.CallAsync<CancelFindMatchResponse>(CancelFindMatchMethod, request);
        }

        public async Task<CancelFindMatchResponse> CancelFindMatchRelatedToUserId(string userId)
        {
            CancelFindMatchRequest request = new CancelFindMatchRequest
            {
                UserId = userId
            };

            return await _contractCallProxy.CallAsync<CancelFindMatchResponse>(CancelFindMatchMethod, request);
        }

        public async Task<GetGameStateResponse> GetGameState(long matchId)
        {
            GetGameStateRequest request = new GetGameStateRequest
            {
                MatchId = matchId
            };

            return await _contractCallProxy.StaticCallAsync<GetGameStateResponse>(GetGameStateMethod, request);
        }

        public async Task<GetMatchResponse> GetMatch(long matchId)
        {
            GetMatchRequest request = new GetMatchRequest
            {
                MatchId = matchId
            };

            return await _contractCallProxy.StaticCallAsync<GetMatchResponse>(GetMatchMethod, request);
        }

        public async Task SubscribeEvent(IList<string> topics)
         {
            #warning Fix the multiple subscription issue once and for all

            for (int i = _subscribeCount; i > 0; i--) {
                await UnsubscribeEvent();
            }

            await _reader.SubscribeAsync(EventHandler, topics);
            _subscribeCount++;
        }

         public async Task UnsubscribeEvent()
         {
            //TODO Remove the logs once we fix the multiple subscription issue once and for all
            if (_subscribeCount > 0)
            {
                Debug.Log("Unsubscribing from Event - Current Subscriptions = " + _subscribeCount);
                try
                {
                    await _reader.UnsubscribeAsync(EventHandler);
                    _subscribeCount--;
                }
                catch (Exception e)
                {
                    Debug.Log("Unsubscribe Error " + e);
                }

            }
            else
            {
                Debug.Log("Tried to Unsubscribe, count <= 0 = " + _subscribeCount);
            }
        }

        public async Task SendPlayerAction(PlayerActionRequest request)
        {
            await _contractCallProxy.CallAsync(SendPlayerActionMethod, request);
        }

        public async Task SendEndMatchRequest(EndMatchRequest request)
        {
            await _contractCallProxy.CallAsync(EndMatchMethod, request);
        }

        public async Task<CheckGameStatusResponse> CheckPlayerStatus(long matchId)
        {
            CheckGameStatusRequest request = new CheckGameStatusRequest
            {
                MatchId = matchId
            };

            return await _contractCallProxy.CallAsync<CheckGameStatusResponse>(CheckGameStatusMethod, request);
        }

        public async Task<KeepAliveResponse> KeepAliveStatus(string userId, long matchId)
        {
            KeepAliveRequest request = new KeepAliveRequest
            {
                MatchId = matchId,
                UserId = userId
            };

            return await _contractCallProxy.CallAsync<KeepAliveResponse>(KeepAliveStatusMethod, request);
        }

        private void EventHandler(object sender, JsonRpcEventData e)
        {
            PlayerActionDataReceived?.Invoke(e.Data);
        }

        #endregion

        #region Custom Game Modes

        private const string ListGameModesMethod = "ListGameModes";
        private const string CallCustomGameModeFunctionMethod = "CallCustomGameModeFunction";
        private const string GetGameModeCustomUiMethod = "GetGameModeCustomUi";

        public async Task<GameModeList> GetCustomGameModeList()
        {
            ListGameModesRequest request = new ListGameModesRequest();
            return await _contractCallProxy.StaticCallAsync<GameModeList>(ListGameModesMethod, request);
        }

        public async Task<GetCustomGameModeCustomUiResponse> GetGameModeCustomUi(Address address)
        {
            GetCustomGameModeCustomUiRequest request = new GetCustomGameModeCustomUiRequest
            {
                Address = address.ToProtobufAddress()
            };

            return await _contractCallProxy.StaticCallAsync<GetCustomGameModeCustomUiResponse>(GetGameModeCustomUiMethod, request);
        }

        public async Task CallCustomGameModeFunction(Address address, byte[] callData)
        {
            CallCustomGameModeFunctionRequest request = new CallCustomGameModeFunctionRequest
            {
                Address = address.ToProtobufAddress(),
                CallData = ByteString.CopyFrom(callData)
            };

            await _contractCallProxy.CallAsync(CallCustomGameModeFunctionMethod, request);
        }

        #endregion
    }
}
