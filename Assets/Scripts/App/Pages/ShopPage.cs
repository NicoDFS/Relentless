using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using Loom.ZombieBattleground.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using Object = UnityEngine.Object;
using Newtonsoft.Json;

namespace Loom.ZombieBattleground
{
    public class ShopPage : IUIElement
    {
        private const float ScrollAnimationDuration = 0.5f;
        private const int MaxItemsInShop = 4;
        private const int LoopStartFakeShopCount = 1;
        private const int LoopEndFakeShopCount = 1;

        private readonly float[] _costs =
        {
            1.99f, 2.99f, 4.99f, 9.99f
        };

        private readonly int[] _amount =
        {
            1, 2, 5, 10
        };
        
        private readonly string[] _longDescriptions =
        {    
            "1 pack of cards",
            "2 packs of cards",
            "5 packs of cards",
            "10 packs of cards"
        };  
        
        private readonly bool[] _isBestValue =
        {
            false, false, false, true
        };

        private readonly string[] _productID =
        {    
            "booster_pack_1",
            "booster_pack_2",
            "booster_pack_5",
            "booster_pack_10"
        };

        private IUIManager _uiManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IPlayerManager _playerManager;

        private ISoundManager _soundManager;

        private IInAppPurchaseManager _inAppPurchaseManager;

        private FiatBackendManager _fiatBackendManager;

        private FiatPlasmaManager _fiatPlasmaManager;

        private GameObject _selfPage;

        private Button _buttonItem1, _buttonItem2, _buttonItem3, _buttonItem4;

        private Button _buttonOpen, _buttonCollection, _buttonBack, _buttonBuy;

        private Button _leftArrowButton, _rightArrowButton;

        private TextMeshProUGUI _costItem1, _costItem2, _costItem3, _costItem4, _wallet;
        
        private TextMeshProUGUI _infoPackAmount, _infoLongDescription;

        private int _currentPackId = -1;

        private GameObject[] _packsObjects;

        private Image[] _imageObjects;

        private Color _deselectedColor;

        private Color _selectedColor;

        private int _selectedShopIndex;
        private HorizontalLayoutGroup _shopsContainer;
        private List<ShopObject> _shopObjects;
        private List<ShopObject> _loopFakeShopObjects;

        private Sequence _shopSelectScrollSequence;

        event Action _finishRequestPack;

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _playerManager = GameClient.Get<IPlayerManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _fiatBackendManager = GameClient.Get<FiatBackendManager>();
            _fiatPlasmaManager = GameClient.Get<FiatPlasmaManager>();
            
            _inAppPurchaseManager = GameClient.Get<IInAppPurchaseManager>();
            #if UNITY_IOS || UNITY_ANDROID
            _inAppPurchaseManager.ProcessPurchaseAction += OnProcessPurchase;
            #endif
            _finishRequestPack += OnFinishRequestPack;

            _selectedColor = Color.white;
            _deselectedColor = new Color(0.5f, 0.5f, 0.5f);

            Hide();       
        }

        public void Update()
        {
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/ShopPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);
            
             _infoPackAmount         = _selfPage.transform.Find("Panel_ShopContent/Panel_OverlordInfo/Text_Amount").GetComponent<TextMeshProUGUI>();
            _infoLongDescription    = _selfPage.transform.Find("Panel_ShopContent/Panel_OverlordInfo/Text_LongDescription").GetComponent<TextMeshProUGUI>();

            _wallet = _selfPage.transform.Find("Wallet").GetComponent<TextMeshProUGUI>();

            _buttonItem1 = _selfPage.transform.Find("Item1").GetComponent<Button>();
            _buttonItem2 = _selfPage.transform.Find("Item2").GetComponent<Button>();
            _buttonItem3 = _selfPage.transform.Find("Item3").GetComponent<Button>();
            _buttonItem4 = _selfPage.transform.Find("Item4").GetComponent<Button>();

            _costItem1 = _buttonItem1.transform.Find("Cost").GetComponent<TextMeshProUGUI>();
            _costItem2 = _buttonItem2.transform.Find("Cost").GetComponent<TextMeshProUGUI>();
            _costItem3 = _buttonItem3.transform.Find("Cost").GetComponent<TextMeshProUGUI>();
            _costItem4 = _buttonItem4.transform.Find("Cost").GetComponent<TextMeshProUGUI>();

            _buttonBack = _selfPage.transform.Find("Image_Header/BackButton").GetComponent<Button>();
            _buttonBuy = _selfPage.transform.Find("BuyNowPanel/Button_Buy").GetComponent<Button>();
            _buttonOpen = _selfPage.transform.Find("Image_Header/Button_Open").GetComponent<Button>();
            _buttonCollection = _selfPage.transform.Find("Button_Collection").GetComponent<Button>();
            _leftArrowButton = _selfPage.transform.Find("Button_LeftArrow").GetComponent<Button>();
            _rightArrowButton = _selfPage.transform.Find("Button_RightArrow").GetComponent<Button>();

            _shopsContainer = _selfPage.transform.Find("Panel_ShopContent/Group")
                .GetComponent<HorizontalLayoutGroup>();

            _buttonItem1.onClick.AddListener(() => ChooseItemHandler(0));
            _buttonItem2.onClick.AddListener(() => ChooseItemHandler(1));
            _buttonItem3.onClick.AddListener(() => ChooseItemHandler(2));
            _buttonItem4.onClick.AddListener(() => ChooseItemHandler(3));

            _buttonBack.onClick.AddListener(BackButtonhandler);
            //_buttonBuy.onClick.AddListener(BuyButtonHandler);
            _buttonOpen.onClick.AddListener(OpenButtonHandler);
            _buttonCollection.onClick.AddListener(CollectionButtonHandler);
            _leftArrowButton.onClick.AddListener(LeftArrowButtonOnClickHandler);
            _rightArrowButton.onClick.AddListener(RightArrowButtonOnClickHandler);

            _packsObjects = new[]
            {
                _buttonItem1.gameObject, _buttonItem2.gameObject, _buttonItem3.gameObject, _buttonItem4.gameObject
            };

            _imageObjects = new[]
            {
                _buttonItem1.transform.Find("Image").GetComponent<Image>(),
                _buttonItem2.transform.Find("Image").GetComponent<Image>(),
                _buttonItem3.transform.Find("Image").GetComponent<Image>(),
                _buttonItem4.transform.Find("Image").GetComponent<Image>()
            };

            foreach (Image img in _imageObjects)
            {
                img.color = _deselectedColor;
            }

            _playerManager.LocalUser.Wallet = 1000;
            _wallet.text = _playerManager.LocalUser.Wallet.ToString("0.00") + " $";
            if (_currentPackId > -1)
            {
                _packsObjects[_currentPackId].transform.Find("Highlight").GetComponent<Image>().DOFade(0f, 0f);
                _currentPackId = -1;
            }

            _costItem1.text = "$ " + _costs[0];
            _costItem2.text = "$ " + _costs[1];
            _costItem3.text = "$ " + _costs[2];
            _costItem4.text = "$ " + _costs[3];
            _buttonItem1.transform.Find("Text_Value").GetComponent<Text>().text = "x" + _amount[0];
            _buttonItem2.transform.Find("Text_Value").GetComponent<Text>().text = "x" + _amount[1];
            _buttonItem3.transform.Find("Text_Value").GetComponent<Text>().text = "x" + _amount[2];
            _buttonItem4.transform.Find("Text_Value").GetComponent<Text>().text = "x" + _amount[3];

            FillShopObjects();
            SetSelectedShopIndexAndUpdateScrollPosition(0, false, force: true);

            _selfPage.SetActive(true);
        }

        private void FillShopObjects()
        {
            _shopObjects = new List<ShopObject>();
            _loopFakeShopObjects = new List<ShopObject>();

            // add fake shop obj in front
            for (int i = 0; i < LoopStartFakeShopCount; i++)
            {
                ShopObject fakeShopObj = new ShopObject(
                     (MaxItemsInShop-1)+1,
                     _shopsContainer.transform, 
                     _isBestValue[MaxItemsInShop-1],
                     _costs[MaxItemsInShop-1],
                     ()=> { } 
                );
                fakeShopObj.Deselect();
                _loopFakeShopObjects.Add(fakeShopObj);
            }

            // real shop obj
            for (int i = 0; i < MaxItemsInShop; i++)
            {
                int index = i;
                ShopObject current = new ShopObject(i + 1, 
                    _shopsContainer.transform, 
                    _isBestValue[i],
                    _costs[i],                    
                    () => BuyButtonHandler(index)
                );
                _shopObjects.Add(current);
            }

            // add fake shop obj in end
            for (int i = 0; i < LoopEndFakeShopCount; i++)
            {
                ShopObject fakeObj = new ShopObject(i + 1, 
                    _shopsContainer.transform, 
                    _isBestValue[0],
                     _costs[0],
                    ()=> { }
                );
                fakeObj.Deselect();
                _loopFakeShopObjects.Add(fakeObj);
            }


            ShopObjectSelected(_shopObjects[0]);
            UpdatePackDescriptionInfo(0);
        }

        private void ShopObjectSelected(ShopObject shopObject)
        {
            foreach (ShopObject item in _shopObjects)
            {
                if (item != shopObject)
                {
                    item.Deselect();
                }
                else
                {
                    item.Select();
                }
            }
        }

        private void SwitchShopObject(int direction)
        {
            int newIndex = _selectedShopIndex;
            newIndex += direction;

            if (newIndex < 0)
            {
                SetSelectedShopIndexAndUpdateScrollPosition(_shopObjects.Count, false, false);
                SetSelectedShopIndexAndUpdateScrollPosition(_shopObjects.Count - 1, true);
                UpdatePackDescriptionInfo(_shopObjects.Count - 1);
            }
            else if (newIndex >= _shopObjects.Count)
            {
                SetSelectedShopIndexAndUpdateScrollPosition(-1, false, false);
                SetSelectedShopIndexAndUpdateScrollPosition(0, true);
                UpdatePackDescriptionInfo(0);
            }
            else
            {
                SetSelectedShopIndexAndUpdateScrollPosition(newIndex, true);
                UpdatePackDescriptionInfo(newIndex);
            }
        }
        
        private void UpdatePackDescriptionInfo( int shopIndex )
        {
            _infoPackAmount.text        = _amount[shopIndex].ToString();
            _infoLongDescription.text   = _longDescriptions[shopIndex];
        }

        private bool SetSelectedShopIndexAndUpdateScrollPosition(
            int shopIndex, bool animateTransition, bool selectShopObject = true, bool force = false)
        {
            if (!force && shopIndex == _selectedShopIndex)
            {
                return false;
            }

            _selectedShopIndex = shopIndex;

            RectTransform shopContainerRectTransform = _shopsContainer.GetComponent<RectTransform>();
            _shopSelectScrollSequence?.Kill();
            if (animateTransition)
            {
                _shopSelectScrollSequence = DOTween.Sequence();
                _shopSelectScrollSequence.Append(
                        DOTween.To(
                            () => shopContainerRectTransform.anchoredPosition,
                            v => shopContainerRectTransform.anchoredPosition = v,
                            CalculateShopContainerShiftForShopIndex(_selectedShopIndex),
                            ScrollAnimationDuration))
                    .AppendCallback(() => _shopSelectScrollSequence = null);
            }
            else
            {
                shopContainerRectTransform.anchoredPosition =
                    CalculateShopContainerShiftForShopIndex(_selectedShopIndex);
            }

            if (selectShopObject)
            {
                ShopObjectSelected(_shopObjects[_selectedShopIndex]);
            }

            return true;
        }

        private Vector2 CalculateShopContainerShiftForShopIndex(int shopIndex)
        {
            return Vector2.left * (shopIndex + 1) * _shopsContainer.spacing;
        }

        public void Hide()
        {
            if (_selfPage == null)
                return;

            _selfPage.SetActive(false);
            Object.Destroy(_selfPage);
            _selfPage = null;
        }

        public void Dispose()
        {
            _shopSelectScrollSequence?.Kill();
        }

        #region Buttons Handlers

        public void OpenButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.PACK_OPENER);
        }

        public void CollectionButtonHandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.ARMY);
        }

        private void BackButtonhandler()
        {
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            GameClient.Get<IAppStateManager>().BackAppState();
        }

        private void BuyButtonHandler( int id )
        {
            _currentPackId = id;
            
            GameClient.Get<ISoundManager>()
                .PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            if (_currentPackId >= _costs.Length || _currentPackId < 0)
            {
                Debug.LogError("No pack chosen");
                return;
            }

            string productID = _productID[id];
            _inAppPurchaseManager.BuyProductID( productID );            
        }
        
        private void PackMoveAnimation()
        {
            _playerManager.LocalUser.Wallet -= _costs[_currentPackId];
            _wallet.text = _playerManager.LocalUser.Wallet.ToString("0.00") + " $";
            GameObject prefab;
            for (int i = 0; i < _amount[_currentPackId]; i++)
            {
                _playerManager.LocalUser.PacksCount++;

                prefab = _loadObjectsManager.GetObjectByPath<GameObject>(
                            "Prefabs/UI/Elements/Shop/PackItem");

                GameObject packItem = Object.Instantiate(prefab);
                packItem.transform.position = _shopObjects[_selectedShopIndex].GetPackImagePosition();
                packItem.transform.SetParent(_selfPage.transform, true);
                packItem.transform.localScale = Vector3.one;
                packItem.SetActive(true);

                Sequence animationSequence = DOTween.Sequence();
                animationSequence.AppendInterval(_amount[_currentPackId] * 0.1f - 0.1f * i);
                animationSequence.Append(packItem.transform.DOMove(Vector3.up * -15f, .5f));
                
                if( i == 0 )
                {
                    animationSequence.OnComplete(
                    () =>
                    {
                        GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.PACK_OPENER);
                    });
                }
            }
        }

        private async void RequestFiatValidation(string productId, string purchaseToken, string storeTxId, string storeName)
        {
            _uiManager.DrawPopup<LoadingFiatPopup>("Request Fiat Validation");
            FiatBackendManager.FiatValidationResponse response = await _fiatBackendManager.CallFiatValidation
            (
                productId,
                purchaseToken,
                storeTxId,
                storeName
            );
            _uiManager.HidePopup<LoadingFiatPopup>();
            RequestFiatTransaction();           
        }
        
        private async void RequestFiatTransaction()
        {
            _uiManager.DrawPopup<LoadingFiatPopup>("Request Fiat Transaction");
            Debug.Log("Call FiatTransaction request");
            List<FiatBackendManager.FiatTransactionResponse> recordList = await _fiatBackendManager.CallFiatTransaction();
            recordList.Sort( (FiatBackendManager.FiatTransactionResponse resA, FiatBackendManager.FiatTransactionResponse resB)=>
            {
                return resB.TxID - resA.TxID;
            });
            string log = "TxID: ";
            foreach( var i in recordList)
            {
                log += i.TxID + ", ";
            }
            Debug.Log(log);
            _uiManager.HidePopup<LoadingFiatPopup>();
            RequestPack(recordList);            
        }
        
        private async void RequestPack(List<FiatBackendManager.FiatTransactionResponse> sortedRecordList)
        {            
            Debug.Log("<color=green>START REQUEST for packs</color>"); 
            List<FiatBackendManager.FiatTransactionResponse> requestList = new List<FiatBackendManager.FiatTransactionResponse>();
            for (int i = 0; i < sortedRecordList.Count; ++i)
            {
                requestList.Add(sortedRecordList[i]);
            }

            for (int i = 0; i < requestList.Count; ++i)
            {
                FiatBackendManager.FiatTransactionResponse record = requestList[i];
                
                _uiManager.DrawPopup<LoadingFiatPopup>($"Request Pack UserId: {record.UserId}, TxID: {record.TxID}");

                string eventResponse = "";

                eventResponse = await _fiatPlasmaManager.CallRequestPacksContract(record);
                Debug.Log($"<color=green>Contract [requestPacks] success call.</color>");
                Debug.Log($"<color=green>EVENT RESPONSE: {eventResponse}</color>");
                if (!string.IsNullOrEmpty(eventResponse))
                {
                    Debug.Log("<color=green>FINISH REQUEST for packs</color>");
                    await _fiatBackendManager.CallFiatClaim
                    (
                        record.UserId,
                        new List<int>
                        {
                            record.TxID
                        }
                    );                    
                }
                _uiManager.HidePopup<LoadingFiatPopup>();
            }
            
            _finishRequestPack();
        }

        private void OnFinishRequestPack()
        {
            Debug.Log("SUCCESSFULLY REQUEST for packs");
            PackMoveAnimation();
        }

#if UNITY_IOS || UNITY_ANDROID
        private void OnProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;
            string productId = product.definition.id;
            string purchaseToken = ParsePurchaseTokenFromReceipt(args.purchasedProduct.receipt);
            string storeTxId = product.transactionID;
            string storeName = product.definition.storeSpecificId;            

            RequestFiatValidation
            (
                productId,
                purchaseToken,
                storeTxId,
                storeName                
            );                    
        }

        private string ParsePurchaseTokenFromReceipt( string receiptString  )
        {
            string payload = "";   
            string purchaseToken = "";
            
            try
            {
                IAPReceipt2 receipt = JsonConvert.DeserializeObject<IAPReceipt2>(receiptString);                
                
                Debug.Log("IAPReceipt");
                string log = "";
                log += "receipt.TransactionID: " + receipt.TransactionID;
                log += "\n";
                log += "receipt.Store: " + receipt.Store;
                log += "\n";
                log += "Payload: " + receipt.Payload;
                Debug.Log(log);  
                payload = receipt.Payload;
            }
            catch
            {
                Debug.LogError("Cannot deserialize args.purchasedProduct.receipt 2");                
            }
            
            if( !string.IsNullOrEmpty(payload) )
            {
                string json = "";
                try
                {
                    ReceiptPayloadStr rPayload = JsonConvert.DeserializeObject<ReceiptPayloadStr>(payload);
                    Debug.Log("json: " + rPayload.json);
                    json = rPayload.json;
                }
                catch
                {
                    Debug.LogError("Cannot deserialize payload str");
                }
                
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        ReceiptJSON rJson = JsonConvert.DeserializeObject<ReceiptJSON>(json);
                        purchaseToken = rJson.purchaseToken;
                        Debug.Log("purchaseToken: " + purchaseToken);
                    }
                    catch
                    {
                        Debug.LogError("Cannot deserialize rJson");
                    }
                }
            }

            return purchaseToken;
        }

        public class IAPReceipt
        {
            public string Store;
            public string TransactionID;
            public ReceiptPayload Payload;
        }
        public class IAPReceipt2
        {
            public string Store;
            public string TransactionID;
            public string Payload;
        }
        public class ReceiptPayload
        {
            public ReceiptJSON json;
        }
         public class ReceiptPayloadStr
        {
            public string json;
        }
        public class ReceiptJSON
        {
            public string productId;
            public string purchaseToken;
        }
#endif

        private void ChooseItemHandler(int id)
        {
            if (_currentPackId == id)
                return;

            if (_currentPackId > -1)
            {
                _packsObjects[_currentPackId].transform.Find("Highlight").GetComponent<Image>().DOFade(0f, 0.3f);
            }

            _currentPackId = id;
            for (int i = 0; i < _imageObjects.Length; i++)
            {
                if (i == _currentPackId)
                {
                    _imageObjects[i].color = _selectedColor;
                }
                else
                {
                    _imageObjects[i].color = _deselectedColor;
                }
            }

            _packsObjects[_currentPackId].transform.Find("Highlight").GetComponent<Image>().DOFade(0.8f, 0.3f);
        }

        private void LeftArrowButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            SwitchShopObject(-1);
        }

        private void RightArrowButtonOnClickHandler()
        {
            _soundManager.PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
            SwitchShopObject(1);
        }

        #endregion

        public class ShopObject
        {
            private readonly GameObject _activeShopItemObj;
            private readonly GameObject _deactiveShopItemObj;
            private readonly ILoadObjectsManager _loadObjectsManager;
            private readonly Image _glowImage;
            private Sequence _stateChangeSequence;

            private GameObject SelfObject { get; }
            
            public delegate void CallbackHandler();

            public ShopObject(int index, 
                              Transform parent, 
                              bool isBestValue,
                              float cost, 
                              CallbackHandler chooseItemHandler  )
            {
                _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
                SelfObject = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>(
                            "Prefabs/UI/Elements/Shop/Item_ShopSelected_"+index), parent, false);

                _activeShopItemObj = SelfObject.transform.Find("Normal").gameObject;
                _deactiveShopItemObj = SelfObject.transform.Find("Gray").gameObject;
                _glowImage = SelfObject.transform.Find("Image_Glow").gameObject.GetComponent<Image>();

                //Initialize ShopObject UI display from data
                InitializeShopItemObj(_activeShopItemObj, isBestValue, cost, chooseItemHandler);
                InitializeShopItemObj(_deactiveShopItemObj, isBestValue, cost, () => {}); 

            }
            
            private void InitializeShopItemObj( GameObject item, bool isBestValue, float cost, CallbackHandler chooseItemHandler )
            {
            
                //Should restructure prefab's child depth later to be more organized
                Transform cardTran = item.transform.Find("Cards");
                GameObject bestValueBadge = cardTran.GetChild(cardTran.childCount - 1).GetChild(0).gameObject;                          
                
                Button buyButton           = item.transform.Find("BuyButton").GetComponent<ButtonShiftingContent>();
                TextMeshProUGUI costLabel  = item.transform.Find("Pack_Price").GetComponent<TextMeshProUGUI>();

                buyButton.onClick.AddListener( ()=> {
                    chooseItemHandler();
                 });
                
                bestValueBadge.SetActive(isBestValue);
                costLabel.text = "$ " + cost.ToString("N2");                
                
            }
            
            public Vector3 GetPackImagePosition()
            {
                return _activeShopItemObj.transform.position;
            }

            public void Select()
            {
                _deactiveShopItemObj.SetActive(false);
                _activeShopItemObj.SetActive(true);
                SetUIActiveState(true, true, false);
            }

            public void Deselect()
            {
                _deactiveShopItemObj.SetActive(true);
                _activeShopItemObj.SetActive(false);
                SetUIActiveState(false, true, false);
            }

            private void SetUIActiveState(bool active, bool animateTransition, bool forceResetAlpha)
            {
                float duration = animateTransition ? ScrollAnimationDuration : 0f;
                float targetAlpha = active ? 1f : 0f;

                _stateChangeSequence?.Kill();
                _stateChangeSequence = DOTween.Sequence();

                Action<Image, bool> applyAnimation = (image, invert) =>
                {
                    image.gameObject.SetActive(true);
                    if (forceResetAlpha)
                    {
                        image.color = image.color.SetAlpha(invert ? targetAlpha : 1f - targetAlpha);
                    }

                    _stateChangeSequence.Insert(0f,
                        image.DOColor(image.color.SetAlpha(invert ? 1f - targetAlpha : targetAlpha), duration)
                            .OnComplete(() => image.gameObject.SetActive(invert ? !active : active)));
                };

                applyAnimation(_glowImage, false);
            }
        }
    }
}
