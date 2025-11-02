// PhotoDataSO.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PhotoData", menuName = "Photo Gallery/Photo Data")]
public class PhotoDataSO : ScriptableObject
{
    [System.Serializable]
    public class PhotoItem
    {
        public string imageName;      // 对应Photos中的文件名
        public string title;          // 展品名称
        public string description;    // 30字简介

        public PhotoItem(string imageName, string title, string description)
        {
            this.imageName = imageName;
            this.title = title;
            this.description = description;
        }
    }

    public List<PhotoItem> photoItems = new List<PhotoItem>();

    // 通过图片名称快速查找的字典
    private Dictionary<string, PhotoItem> photoDict;

    public void InitializeDictionary()
    {
        photoDict = new Dictionary<string, PhotoItem>();
        foreach (var item in photoItems)
        {
            if (!string.IsNullOrEmpty(item.imageName))
            {
                photoDict[item.imageName] = item;
            }
        }
    }

    public PhotoItem GetPhotoItem(string imageName)
    {
        if (photoDict == null)
            InitializeDictionary();

        if (photoDict.ContainsKey(imageName))
            return photoDict[imageName];
        return null;
    }
}