using System.Collections.Generic;
using UnityEngine;

namespace qbot.Utility
{
    [System.Serializable]
    public class TextData
    {
        public int Key;
        public string Value;
    }

    [CreateAssetMenu(fileName = "TextDataContainer", menuName = "qbot/Text Data Container", order = 1)]
    public class TextDataContainer : ScriptableObject
    {
        [SerializeField] private List<TextData> _texts;

        public string GetText(int key)
        {
            var localizedText = _texts.Find(t => t.Key == key);
            return localizedText != null ? localizedText.Value : key.ToString();
        }
    }
}