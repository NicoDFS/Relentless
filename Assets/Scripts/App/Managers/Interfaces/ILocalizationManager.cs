using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using UnityEngine;
using TMPro;

namespace Loom.ZombieBattleground
{
    public interface ILocalizationManager
    {
        event Action<Enumerators.Language> LanguageWasChangedEvent;

        Dictionary<SystemLanguage, Enumerators.Language> SupportedLanguages { get; }

        Enumerators.Language CurrentLanguage { get; }

        void ApplyLocalization();

        void SetLanguage(Enumerators.Language language, bool forceUpdate = false);

        string GetUITranslation(string key);
        
        Dictionary<Enumerators.Language, TMP_FontAsset> fontLanguages {get; set;}
    }
}
