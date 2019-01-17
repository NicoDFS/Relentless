using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Loom.ZombieBattleground
{
    public class UIManager : IService, IUIManager
    {
        private List<IUIPopup> _uiPopups;

        public List<IUIElement> Pages { get; private set; }

        public void Dispose()
        {
            foreach (IUIElement page in Pages)
            {
                page.Dispose();
            }

            foreach (IUIPopup popup in _uiPopups)
            {
                popup.Dispose();
            }
        }

        public void Init()
        {
            Canvas = GameObject.Find("Canvas1");
            Canvas2 = GameObject.Find("Canvas2");
            Canvas3 = GameObject.Find("Canvas3");
            CanvasScaler = Canvas.GetComponent<CanvasScaler>();

            Pages = new List<IUIElement>();
            Pages.Add(new LoadingPage());
            Pages.Add(new MainMenuPage());
            Pages.Add(new OverlordSelectionPage());
            Pages.Add(new HordeSelectionPage());
            Pages.Add(new ArmyPage());
            Pages.Add(new HordeEditingPage());
            Pages.Add(new ShopPage());
            Pages.Add(new GameplayPage());
            Pages.Add(new PackOpenerPage());
            Pages.Add(new CreditsPage());
            Pages.Add(new PlaySelectionPage());
            Pages.Add(new PvPSelectionPage());
            Pages.Add(new CustomGameModeListPage());
            Pages.Add(new CustomGameModeCustomUiPage());

            foreach (IUIElement page in Pages)
            {
                page.Init();
            }

            _uiPopups = new List<IUIPopup>();
            _uiPopups.Add(new CardInfoPopup());
            _uiPopups.Add(new DesintigrateCardPopup());
            _uiPopups.Add(new WarningPopup());
            _uiPopups.Add(new QuestionPopup());
            _uiPopups.Add(new TutorialPopup());
            _uiPopups.Add(new PreparingForBattlePopup());
            _uiPopups.Add(new YouLosePopup());
            _uiPopups.Add(new YouWonPopup());
            _uiPopups.Add(new LevelUpPopup());
            _uiPopups.Add(new YourTurnPopup());
            _uiPopups.Add(new ConfirmationPopup());
            _uiPopups.Add(new LoadingGameplayPopup());
            _uiPopups.Add(new PlayerOrderPopup());
            //Hide for current Beta release
            //_uiPopups.Add(new TermsPopup());
            _uiPopups.Add(new LoginPopup());
            _uiPopups.Add(new MatchMakingPopup());
            _uiPopups.Add(new ConnectionPopup());
            _uiPopups.Add(new OverlordAbilitySelectionPopup());
            _uiPopups.Add(new OverlordAbilityTooltipPopup());
            _uiPopups.Add(new PastActionsPopup());
            _uiPopups.Add(new SettingsPopup());
            _uiPopups.Add(new UpdatePopup());
            _uiPopups.Add(new MulliganPopup());
            _uiPopups.Add(new LoadDataMessagePopup());
            _uiPopups.Add(new LoadingFiatPopup());

            foreach (IUIPopup popup in _uiPopups)
            {
                popup.Init();
            }
        }

        public void Update()
        {
            foreach (IUIElement page in Pages)
            {
                page.Update();
            }

            foreach (IUIPopup popup in _uiPopups)
            {
                popup.Update();
            }
        }

        public IUIElement CurrentPage { get; set; }

        public CanvasScaler CanvasScaler { get; set; }

        public GameObject Canvas { get; set; }

        public GameObject Canvas2 { get; set; }

        public GameObject Canvas3 { get; set; }

        public void HideAllPages()
        {
            foreach (IUIElement page in Pages)
            {
                page.Hide();
            }
        }

        public void HideAllPopups()
        {
            foreach (IUIPopup popup in _uiPopups)
            {
                popup.Hide();
            }
        }

        public void SetPage<T>(bool hideAll = false)
            where T : IUIElement
        {
            if (hideAll)
            {
                HideAllPages();
            }
            else
            {
                CurrentPage?.Hide();
            }

            foreach (IUIElement page in Pages)
            {
                if (page is T)
                {
                    CurrentPage = page;
                    break;
                }
            }

            CurrentPage.Show();
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CHANGE_SCREEN, Constants.SfxSoundVolume,
                false, false, true);
        }

        public void DrawPopup<T>(object message = null, bool setMainPriority = false)
            where T : IUIPopup
        {
            IUIPopup popup = GetPopup<T>();

            if (setMainPriority)
            {
                popup.SetMainPriority();
            }

            if (message == null)
            {
                popup.Show();
            }
            else
            {
                popup.Show(message);
            }
        }

        public void HidePopup<T>()
            where T : IUIPopup
        {
            IUIPopup popup = GetPopup<T>();
            popup.Hide();
        }

        public T GetPopup<T>()
            where T : IUIPopup
        {
            for (int i = 0; i < _uiPopups.Count; i++)
            {
                if (_uiPopups[i] is T popup)
                    return popup;
            }

            return default(T);
        }

        public T GetPage<T>()
            where T : IUIElement
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i] is T page)
                    return page;
            }

            return default(T);
        }
    }
}
