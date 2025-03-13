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
        [SerializeField] private List<TextData> _texts = new();

        public void SetTextData(List<TextData> texts)
        {
            _texts = texts;
            RemoveDuplicateKeys();
        }

        private void RemoveDuplicateKeys()
        {
            var uniqueData = new Dictionary<int, TextData>();
            foreach (var textData in _texts)
            {
                uniqueData.TryAdd(textData.Key, textData);
            }

            _texts = new List<TextData>(uniqueData.Values);
        }

        public string GetText(int key)
        {
            var localizedText = _texts.Find(t => t.Key == key);
            return localizedText != null ? localizedText.Value : key.ToString();
        }
    }
}