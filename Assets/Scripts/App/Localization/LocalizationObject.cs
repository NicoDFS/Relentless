using UnityEngine;
using log4net;
using TMPro;
using Language = Loom.ZombieBattleground.Common.Enumerators.Language;

namespace Loom.ZombieBattleground.Localization
{   
    public class LocalizationObject : MonoBehaviour
    {
        public LocalizationTerm translationKey;
        private static readonly ILog Log = Logging.GetLog(nameof(LocalizationFontSettings));

        private ILocalizationManager _localizationManager;

        private TextMeshProUGUI _text;

        private TextMeshPro _tmpText;
        
        void Awake()
        {
            _localizationManager = GameClient.Get<ILocalizationManager>();

            _text = this.gameObject.GetComponent<TextMeshProUGUI>();
            _tmpText = this.gameObject.GetComponent<TextMeshPro>();

            _localizationManager.LanguageWasChangedEvent += OnApplyTranslation;

            OnApplyTranslation(_localizationManager.CurrentLanguage);
        }
        
        private void OnApplyTranslation(Language language)
        {
            if (_text != null)
            {
                _text.text = LocalizationUtil.GetLocalizedString(translationKey, _text.text);
            }
            else if(_tmpText != null)
            {
                _tmpText.text = LocalizationUtil.GetLocalizedString(translationKey, _tmpText.text);
            }
        }
    }
}