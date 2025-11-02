// CSVToPhotoDataEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CSVToPhotoDataEditor : EditorWindow
{
    private string csvFilePath = "";
    private string statusMessage = "";
    private Vector2 scrollPosition;

    [MenuItem("Tools/CSV to Photo Data Converter")]
    static void Init()
    {
        CSVToPhotoDataEditor window = (CSVToPhotoDataEditor)EditorWindow.GetWindow(typeof(CSVToPhotoDataEditor));
        window.titleContent = new GUIContent("CSV转换工具");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("CSV转Photo Data工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 使用文件路径选择而不是ObjectField
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CSV文件路径:", GUILayout.Width(100));
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        if (GUILayout.Button("浏览", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", Application.streamingAssetsPath, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 显示当前选择的文件
        if (!string.IsNullOrEmpty(csvFilePath))
        {
            EditorGUILayout.HelpBox($"已选择文件: {Path.GetFileName(csvFilePath)}", MessageType.Info);
        }

        // 说明文本
        EditorGUILayout.HelpBox(
            "请确保CSV文件包含以下列：\n" +
            "- ImageName（对应Photos文件夹中的文件名）\n" +
            "- 展品名称\n" +
            "- 30字简介",
            MessageType.Info
        );

        GUILayout.Space(20);

        // 转换按钮
        if (GUILayout.Button("转换为Photo Data", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
            {
                EditorUtility.DisplayDialog("错误", "请选择有效的CSV文件", "确定");
                return;
            }

            ConvertCSVToPhotoData();
        }

        GUILayout.Space(10);

        // 状态显示
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        // 滚动视图显示详细日志
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        EditorGUILayout.EndScrollView();
    }

    void ConvertCSVToPhotoData()
    {
        try
        {
            // 创建或获取PhotoDataSO
            PhotoDataSO dataSO = CreateOrGetPhotoDataSO();
            dataSO.photoItems.Clear();

            // 读取CSV文件内容
            string csvText = File.ReadAllText(csvFilePath);
            string[] lines = csvText.Split('\n');

            // 查找列索引
            int imageNameIndex = -1;
            int titleIndex = -1;
            int descriptionIndex = -1;

            // 读取表头
            if (lines.Length > 0)
            {
                string[] headers = ParseCSVLine(lines[0]);
                for (int i = 0; i < headers.Length; i++)
                {
                    string header = headers[i].Trim();
                    if (header == "ImageName")
                        imageNameIndex = i;
                    else if (header == "展品名称")
                        titleIndex = i;
                    else if (header == "30字简介")
                        descriptionIndex = i;
                }
            }

            // 检查必要的列
            if (imageNameIndex == -1 || titleIndex == -1 || descriptionIndex == -1)
            {
                statusMessage = "错误：CSV文件中未找到必要的列名（ImageName, 展品名称, 30字简介）";
                Debug.LogError(statusMessage);
                return;
            }

            int successCount = 0;
            int errorCount = 0;

            // 处理数据行（从第二行开始）
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i].Trim()))
                    continue;

                string[] values = ParseCSVLine(lines[i]);

                if (values.Length > Mathf.Max(imageNameIndex, titleIndex, descriptionIndex))
                {
                    string imageName = values[imageNameIndex].Trim();
                    string title = values[titleIndex].Trim();
                    string description = values[descriptionIndex].Trim();

                    if (!string.IsNullOrEmpty(imageName) && !string.IsNullOrEmpty(title))
                    {
                        // 使用PhotoDataSO的PhotoItem构造函数
                        PhotoDataSO.PhotoItem item = new PhotoDataSO.PhotoItem(imageName, title, description);
                        dataSO.photoItems.Add(item);
                        successCount++;

                        Debug.Log($"添加数据: {imageName} - {title}");
                    }
                    else
                    {
                        errorCount++;
                        Debug.LogWarning($"跳过第{i + 1}行：缺少必要数据");
                    }
                }
                else
                {
                    errorCount++;
                    Debug.LogWarning($"跳过第{i + 1}行：数据列不足");
                }
            }

            // 保存并刷新
            EditorUtility.SetDirty(dataSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 初始化字典
            dataSO.InitializeDictionary();

            statusMessage = $"转换完成！成功: {successCount} 条，失败: {errorCount} 条";
            Debug.Log(statusMessage);

            // 选中生成的文件
            Selection.activeObject = dataSO;
        }
        catch (System.Exception e)
        {
            statusMessage = $"转换失败: {e.Message}";
            Debug.LogError(statusMessage);
        }
    }

    PhotoDataSO CreateOrGetPhotoDataSO()
    {
        // 查找现有的PhotoDataSO
        string[] guids = AssetDatabase.FindAssets("t:PhotoDataSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PhotoDataSO>(path);
        }

        // 创建新的PhotoDataSO
        PhotoDataSO dataSO = ScriptableObject.CreateInstance<PhotoDataSO>();
        string assetPath = "Assets/Resources/PhotoData.asset";
        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(dataSO, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return dataSO;
    }

    // 解析CSV行，处理逗号在引号内的情况
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }
}
#endif