using System;
using Loom.ZombieBattleground.Localization;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using UnityEngine;
using TMPro;

namespace Loom.ZombieBattleground
{
    public class LocalizationManager : IService, ILocalizationManager
    {
        private readonly Enumerators.Language _defaultLanguage = Enumerators.Language.EN;

        ILoadObjectsManager _loadObjectsManager;

        private IDataManager _dataManager;

        public event Action<Enumerators.Language> LanguageWasChangedEvent;

        public Dictionary<SystemLanguage, Enumerators.Language> SupportedLanguages { get; private set; }

        public Enumerators.Language CurrentLanguage { get; private set; } = Enumerators.Language.NONE;

        public Dictionary<Enumerators.Language, TMP_FontAsset> fontLanguages {get; set;}

        public void ApplyLocalization()
        {
            if (!SupportedLanguages.ContainsKey(Application.systemLanguage))
            {
                if (_dataManager.CachedUserLocalData.AppLanguage == Enumerators.Language.NONE)
                {
                    SetLanguage(_defaultLanguage);
                }
                else
                {
                    SetLanguage(_dataManager.CachedUserLocalData.AppLanguage);
                }
            }
            else
            {
                if (_dataManager.CachedUserLocalData.AppLanguage == Enumerators.Language.NONE)
                {
                    SetLanguage(SupportedLanguages[Application.systemLanguage]);
                }
                else
                {
                    SetLanguage(_dataManager.CachedUserLocalData.AppLanguage);
                }
            }
        }

        public void SetLanguage(Enumerators.Language language, bool forceUpdate = false)
        {
            if (language == CurrentLanguage && !forceUpdate)
                return;

            CurrentLanguage = language;
            _dataManager.CachedUserLocalData.AppLanguage = language;

            LanguageWasChangedEvent?.Invoke(CurrentLanguage);
        }

        public string GetUITranslation(string key)
        {
            return "";
        }

        public void Dispose()
        {
        }

        public void Init()
        {
            fontLanguages = new Dictionary<Enumerators.Language, TMP_FontAsset>();
            
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            
            fontLanguages.Add(Enumerators.Language.ZH_CN, MainApp.StaticChineseFont);
            fontLanguages.Add(Enumerators.Language.EN, MainApp.StaticEnglishFont);

            _dataManager = GameClient.Get<IDataManager>();

            FillLanguages();

            ApplyLocalization();
        }

        public void Update()
        {
        }

        private void FillLanguages()
        {
            SupportedLanguages = new Dictionary<SystemLanguage, Enumerators.Language>();

            SupportedLanguages.Add(SystemLanguage.English, Enumerators.Language.EN);
            SupportedLanguages.Add(SystemLanguage.German, Enumerators.Language.ZH_CN);

            string content = _loadObjectsManager.GetObjectByPath<TextAsset>("localization_data.txt").text;
            string[] lines = content.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                Debug.LogWarning(lines[i]);
                string[] fields = lines[i].Split(',');

                LocalizationTerm term;
                if (Enum.TryParse<LocalizationTerm>(fields[0], out term)) 
                {
                    Debug.LogWarning(fields.Length);
                    LocalizationString localizedString = new LocalizationString();
                    localizedString.translation.Add(Enumerators.Language.EN, fields[1]);
                    localizedString.translation.Add(Enumerators.Language.ZH_CN, fields[2]);
                    
                    LocalizationUtil.LocalizedStringDictionary.Add(term, localizedString);
                }
            }
        }
    }
}
