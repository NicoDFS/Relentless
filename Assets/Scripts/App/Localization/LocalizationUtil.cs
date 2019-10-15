using System.Collections.Generic;
using log4net;
using Loom.ZombieBattleground.Common;
using UnityEngine;

namespace Loom.ZombieBattleground.Localization
{
    public static class LocalizationUtil
    {
        private static readonly ILog Log = Logging.GetLog(nameof(LocalizationUtil));
        
        public static Dictionary<LocalizationTerm, LocalizationString> LocalizedStringDictionary = new Dictionary<LocalizationTerm, LocalizationString>();

        public static readonly Dictionary<Enumerators.Language, string> IsoLanguageCodeToFullLanguageNameMap = new Dictionary<Enumerators.Language, string>
        {
            { Enumerators.Language.EN, "English" },
            { Enumerators.Language.ZH_CN, "Chinese" }
        };

        public static string GetLocalizedString(LocalizationTerm term, string fallbackText = "")
        {
            try
            {
                return LocalizedStringDictionary[term].GetString().Replace("\\n", "\n");
            }
            catch
            {
                return fallbackText;
            }
        }

        public static string GetLocalizedStringFromEnglish(string translation)
        {
            try
            {
                foreach(KeyValuePair<LocalizationTerm, LocalizationString> entry in LocalizedStringDictionary)
                {
                    if (entry.Value.Contains(translation))
                    {
                        return entry.Value.GetString().Replace("\\n", "\n");
                    }
                }
                return translation;
            }
            catch
            {
                return translation;
            }
        }

        public static void SetLanguage(Enumerators.Language language)
        {
            string fullLanguageName = GetFullLanguageName(language);
        }
        
        private static string GetFullLanguageName(Enumerators.Language language)
        {
            return IsoLanguageCodeToFullLanguageNameMap.ContainsKey(language) ?
                IsoLanguageCodeToFullLanguageNameMap[language] :
                "";
        }
        
        public static string CapitalizedText(string text)
        {
            if(string.IsNullOrEmpty(text))
            {
                return text;
            }
            
            string capitalizedText = text;
            capitalizedText = capitalizedText.Length > 1 ?
                capitalizedText.Substring(0, 1).ToUpper() + capitalizedText.Substring(1).ToLower() :
                capitalizedText.ToUpper();

            return capitalizedText;
        }
    }

    public class LocalizationString 
    {
        public Dictionary<Enumerators.Language, string> translation = new Dictionary<Enumerators.Language, string>();
        private IDataManager _dataManager = GameClient.Get<IDataManager>();

        public string GetString()
        {
            string transString = translation[_dataManager.CachedUserLocalData.AppLanguage];
            if (transString != "")
            {
                string translationText = translation[_dataManager.CachedUserLocalData.AppLanguage];
                if (_dataManager.CachedUserLocalData.AppLanguage == Enumerators.Language.ZH_CN)
                {
                    translationText = translationText.Replace("<b>", "").Replace("</b>", "");
                }
                return translation[_dataManager.CachedUserLocalData.AppLanguage];
            }
            else
            {
                return translation[Enumerators.Language.EN];
            }
        }

        public bool Contains(string toFind)
        {
            return translation.ContainsValue(toFind);
        }
    }
}
