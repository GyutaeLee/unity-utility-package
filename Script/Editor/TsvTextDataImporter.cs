using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace qbot.Utility
{
    public class TsvTextDataImporter : EditorWindow
    {
        private string _tsvFilePath;
        private TextDataContainer _koreanDataContainer;
        private TextDataContainer _englishDataContainer;

        [MenuItem("Tools/TSV Text Data Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TsvTextDataImporter>("TSV Text Data Importer");
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
        }

        private void OnGUI()
        {
            GUILayout.Label("Select TSV file and assign containers for each language", EditorStyles.boldLabel);

            // TSV file path and button arranged in a single line
            EditorGUILayout.BeginHorizontal();
            _tsvFilePath = EditorGUILayout.TextField("TSV File Path", _tsvFilePath);
            if (GUILayout.Button("Find TSV File", GUILayout.Width(100)))
            {
                _tsvFilePath = EditorUtility.OpenFilePanel("Select TSV File", "", "tsv");
            }

            EditorGUILayout.EndHorizontal();

            // Language-specific data container assignments
            _koreanDataContainer = (TextDataContainer)EditorGUILayout.ObjectField("Korean Data Container", _koreanDataContainer, typeof(TextDataContainer), false);
            _englishDataContainer = (TextDataContainer)EditorGUILayout.ObjectField("English Data Container", _englishDataContainer, typeof(TextDataContainer), false);

            // Enable Import button only when all fields are properly set
            GUI.enabled = !string.IsNullOrEmpty(_tsvFilePath) && _koreanDataContainer != null && _englishDataContainer != null;
            if (GUILayout.Button("Import Data"))
            {
                ImportData();
            }

            GUI.enabled = true; // Restore GUI enabled state for other UI elements
        }

        private void ImportData()
        {
            if (!File.Exists(_tsvFilePath))
            {
                Debug.LogError("TSV file not found: " + _tsvFilePath);
                return;
            }

            var koreanDataList = new List<TextData>();
            var englishDataList = new List<TextData>();

            try
            {
                using (var reader = new StreamReader(_tsvFilePath, Encoding.UTF8))
                {
                    // Skip the first line as it is the header
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split('\t');
                        if (values.Length < 3) continue; // Expecting columns: ID, Korean, English

                        if (int.TryParse(values[0].Trim(), out var key))
                        {
                            var koreanValue = values[1].Trim('"').Trim(); // Korean text
                            var englishValue = values[2].Trim('"').Trim(); // English text

                            koreanDataList.Add(new TextData { Key = key, Value = koreanValue });
                            englishDataList.Add(new TextData { Key = key, Value = englishValue });
                        }
                    }
                }

                // Set data for each language container
                _koreanDataContainer.SetTextData(koreanDataList);
                _englishDataContainer.SetTextData(englishDataList);

                // Mark containers as dirty to ensure changes are saved
                EditorUtility.SetDirty(_koreanDataContainer);
                EditorUtility.SetDirty(_englishDataContainer);

                Debug.Log("TSV data import completed successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading TSV file: " + e.Message);
            }
        }
    }
}