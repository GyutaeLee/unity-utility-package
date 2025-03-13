using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace qbot.Utility
{
    public class CsvTextDataImporter : EditorWindow
    {
        private static readonly string ProjectPrefix = Application.dataPath.GetHashCode().ToString();

        private string _csvFilePath;
        private string CsvFilePath
        {
            get => EditorPrefs.GetString($"{ProjectPrefix}.{nameof(CsvTextDataImporter)}.{nameof(CsvFilePath)}");
            set => EditorPrefs.SetString($"{ProjectPrefix}.{nameof(CsvTextDataImporter)}.{nameof(CsvFilePath)}", value);
        }

        private TextDataContainer _koreanDataContainer;
        private TextDataContainer _englishDataContainer;

        [MenuItem("qbot/Utility/CSV Text Data Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<CsvTextDataImporter>("CSV Text Data Importer");
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
        }

        private void OnEnable()
        {
            _csvFilePath = CsvFilePath;
        }

        private void OnGUI()
        {
            GUILayout.Label("Select CSV file and assign containers for each language", EditorStyles.boldLabel);

            // CSV file path and button arranged in a single line
            EditorGUILayout.BeginHorizontal();
            var newCsvFilePath = EditorGUILayout.TextField("CSV File Path", _csvFilePath);
            if (newCsvFilePath != _csvFilePath)
            {
                _csvFilePath = newCsvFilePath;
                CsvFilePath = newCsvFilePath;
            }

            if (GUILayout.Button("Find CSV File", GUILayout.Width(100)))
            {
                CsvFilePath = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
            }

            EditorGUILayout.EndHorizontal();

            // Language-specific data container assignments
            _koreanDataContainer = (TextDataContainer)EditorGUILayout.ObjectField("Korean Data Container", _koreanDataContainer, typeof(TextDataContainer), false);
            _englishDataContainer = (TextDataContainer)EditorGUILayout.ObjectField("English Data Container", _englishDataContainer, typeof(TextDataContainer), false);

            // Enable Import button only when all fields are properly set
            GUI.enabled = !string.IsNullOrEmpty(CsvFilePath) && _koreanDataContainer != null && _englishDataContainer != null;
            if (GUILayout.Button("Import Data"))
            {
                ImportData();
            }

            GUI.enabled = true; // Restore GUI enabled state for other UI elements
        }

        private void ImportData()
        {
            if (File.Exists(CsvFilePath) == false)
            {
                Debug.LogError("CSV file not found: " + CsvFilePath);
                return;
            }

            var koreanDataList = new List<TextData>();
            var englishDataList = new List<TextData>();

            try
            {
                using (var reader = new StreamReader(CsvFilePath, Encoding.UTF8))
                {
                    // Skip the first line as it is the header
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split('\t');
                        if (values.Length < 3) continue; // Expecting columns: ID, Korean, English

                        if (int.TryParse(values[0].Trim(), out var key) == false)
                            continue;

                        var koreanValue = values[1].Trim(); // Korean text
                        var englishValue = values[2].Trim(); // English text
                        
                        koreanValue = koreanValue.Replace("…", "...");
                        englishValue = englishValue.Replace("…", "...");

                        koreanDataList.Add(new TextData { Key = key, Value = koreanValue });
                        englishDataList.Add(new TextData { Key = key, Value = englishValue });
                    }
                }

                // Set data for each language container
                _koreanDataContainer.SetTextData(koreanDataList);
                _englishDataContainer.SetTextData(englishDataList);

                // Mark containers as dirty to ensure changes are saved
                EditorUtility.SetDirty(_koreanDataContainer);
                EditorUtility.SetDirty(_englishDataContainer);

                Debug.Log("CSV data import completed successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error reading CSV file: " + e.Message);
            }
        }
    }
}