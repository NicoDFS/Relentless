using UnityEngine;
using System.Collections.Generic;
using log4net;
using TMPro;
using Language = Loom.ZombieBattleground.Common.Enumerators.Language;

namespace Loom.ZombieBattleground.Localization
{   
    public class LocalizationObject : MonoBehaviour
    {
        public LocalizationTerm TranslationKey;
        public bool ChangeFont = true;
        public bool ChangeText = true;
        public bool KeepOriginalFontInEnglish = false;
        private TMP_FontAsset _originalFont;
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

            if (_text != null)
            {
                _originalFont = _text.font;
            }
            else if (_tmpText != null)
            {
                _originalFont = _tmpText.font;
            }

            OnApplyTranslation(_localizationManager.CurrentLanguage);
        }
        
        private void OnApplyTranslation(Language language)
        {
            if (_text != null)
            {
                if (ChangeText)
                {
                    _text.text = LocalizationUtil.GetLocalizedString(TranslationKey, _text.text);
                }

                if (ChangeFont)
                {
                    if (_localizationManager.CurrentLanguage == Language.EN && KeepOriginalFontInEnglish)
                    {
                        _text.font = _originalFont;
                    }
                    else
                    {
                        _text.font = _localizationManager.fontLanguages[_localizationManager.CurrentLanguage];
                    }
                }
            }
            else if(_tmpText != null)
            {
                if (ChangeText)
                {
                    _tmpText.text = LocalizationUtil.GetLocalizedString(TranslationKey, _tmpText.text);
                }

                if (ChangeFont)
                {
                    if (_localizationManager.CurrentLanguage == Language.EN && KeepOriginalFontInEnglish)
                    {
                        _tmpText.font = _originalFont;
                    }
                    else
                    {
                        _tmpText.font = _localizationManager.fontLanguages[_localizationManager.CurrentLanguage];
                    }
                }
            }
            else
            {
                _localizationManager.LanguageWasChangedEvent -= OnApplyTranslation;
            }
        }
    }
}