using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoWall : MonoBehaviour, IPointerClickHandler
{
    public RectTransform prefab; //照片预制体

    public int row = 6;       // 行
    public int column = 8;    // 列

    public int startXPos = 60;  // 起始X坐标（最左侧照片的位置）
    public int startYPos = -100;// 起始Y坐标

    public float distanceX = 65; //X轴间距值
    public float distanceY = 80; //Y轴间距值

    float initMoveDistance = 1800; //初始从右侧进入的移动距离

    [Header("放大设置")]
    public float enlargeSize = 5f;      //放大倍数

    float radiateSize = 550;    //扩散效果的半径范围

    [Header("外围照片移动设置")]
    public float inwardMoveDistance = 100f; // 外围照片向内移动的距离

    [Header("自动恢复设置")]
    public float autoRestoreTime = 30f; // 自动恢复时间（秒")

    List<List<RectTransform>> goList;  //二维列表，存储所有照片引用
    Dictionary<RectTransform, Vector2> itemPosDict;//字典，照片-目标位置
    Dictionary<RectTransform, Vector2Int> itemIndexDict; // 字典，照片-行列索引
    Dictionary<RectTransform, string> itemDetailMap; // 字典，照片-详情页图片名称映射
    List<RectTransform> changedItemList;  // 临时列表，存储受扩散效果影响的照片
    Sprite[] loadedSprites;               // 图片数组（从Resources加载）
    Dictionary<string, Sprite> detailSprites; // 详情页图片字典

    RectTransform currentSelectedItem; // 当前选中的照片
    GameObject currentDetailPage; // 当前显示的详情页（改为GameObject）
    bool isExpanded = false; // 是否已展开
    Coroutine autoRestoreCoroutine; // 自动恢复协程

    void Start()
    {
        DOTween.SetTweensCapacity(2000, 100);

        goList = new List<List<RectTransform>>();
        itemPosDict = new Dictionary<RectTransform, Vector2>();
        itemIndexDict = new Dictionary<RectTransform, Vector2Int>();
        itemDetailMap = new Dictionary<RectTransform, string>();
        changedItemList = new List<RectTransform>();
        detailSprites = new Dictionary<string, Sprite>();

        LoadSpritesFromResources();
        LoadDetailSprites();
        CreateGos();
    }

    void LoadSpritesFromResources()
    {
        loadedSprites = Resources.LoadAll<Sprite>("Photos/");
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogError("没有在Resources/Photos/文件夹中找到图片！");
        }
        else
        {
            Debug.Log($"成功加载 {loadedSprites.Length} 张图片");

            // 检查图片数量是否匹配
            if (loadedSprites.Length != row * column)
            {
                Debug.LogWarning($"图片数量不匹配: 需要 {row * column} 张，但找到 {loadedSprites.Length} 张");
            }
        }
    }

    void LoadDetailSprites()
    {
        Sprite[] details = Resources.LoadAll<Sprite>("Details/");
        if (details == null || details.Length == 0)
        {
            Debug.LogError("没有在Resources/Details/文件夹中找到详情页图片！");
        }
        else
        {
            foreach (Sprite detail in details)
            {
                detailSprites[detail.name] = detail;
            }
            Debug.Log($"成功加载 {details.Length} 张详情页图片");
        }
    }

    void CreateGos()//入场动画
    {
        // 创建图片索引列表并洗牌
        List<int> photoIndices = GetShuffledPhotoIndices();
        int photoIndex = 0;

        for (int i = 0; i < row; i++)
        {
            List<RectTransform> gos = new List<RectTransform>();
            goList.Add(gos);

            for (int j = 0; j < column; j++)
            {
                RectTransform item = Instantiate(prefab.gameObject).GetComponent<RectTransform>();
                item.name = $"Photo_{i}_{j}";
                item.transform.SetParent(transform);

                // 设置图片 - 使用洗牌后的顺序
                if (loadedSprites != null && loadedSprites.Length > 0)
                {
                    Image img = item.GetComponent<Image>();
                    if (img != null)
                    {
                        // 使用洗牌后的索引
                        int spriteIndex = photoIndices[photoIndex];
                        Sprite selectedSprite = loadedSprites[spriteIndex];
                        img.sprite = selectedSprite;

                        string detailName = GetDetailName(selectedSprite.name);
                        itemDetailMap[item] = detailName;
                        photoIndex++;
                    }
                }

                Vector2 endPos = new Vector3(startXPos + j * distanceX, startYPos - i * distanceY);
                Vector2 startPos = new Vector3(endPos.x + initMoveDistance, endPos.y);

                item.anchoredPosition = startPos;

                Tweener tweener = item.DOAnchorPos(endPos, Random.Range(1.8f, 2f));
                tweener.SetDelay(j * 0.1f + (row - i) * 0.1f);
                tweener.SetEase(Ease.InSine);
                item.gameObject.SetActive(true);

                AddClickEventToItem(item);

                gos.Add(item);
                itemPosDict.Add(item, endPos);
                itemIndexDict.Add(item, new Vector2Int(i, j));
            }
        }

        Debug.Log($"成功创建 {row * column} 个照片，使用了 {photoIndex} 张图片");
    }

    // 获取洗牌后的图片索引
    List<int> GetShuffledPhotoIndices()
    {
        List<int> indices = new List<int>();

        // 添加所有图片的索引
        for (int i = 0; i < loadedSprites.Length; i++)
        {
            indices.Add(i);
        }

        // Fisher-Yates 洗牌算法 - 确保随机但不重复
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        return indices;
    }

    string GetDetailName(string spriteName)
    {
        return spriteName + "_detail";
    }

    void AddClickEventToItem(RectTransform item)
    {
        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = item.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnPhotoClick(item); });
        trigger.triggers.Add(entry);
    }

    public void OnPhotoClick(RectTransform item)
    {
        ResetAutoRestoreTimer();

        if (isExpanded && currentSelectedItem != null)
        {
            if (currentSelectedItem == item)
            {
                RestoreAllItems();
                return;
            }
            else
            {
                RestoreAllItems();
            }
        }

        ExpandItem(item);
    }

    void ExpandItem(RectTransform item)
    {
        currentSelectedItem = item;
        isExpanded = true;

        // 隐藏原始图片
        item.gameObject.SetActive(false);

        Vector2 pos = itemPosDict[item];
        changedItemList = new List<RectTransform>();

        foreach (KeyValuePair<RectTransform, Vector2> i in itemPosDict)
        {
            if (i.Key != item && Vector2.Distance(i.Value, pos) < radiateSize)
            {
                changedItemList.Add(i.Key);
            }
        }

        for (int i = 0; i < changedItemList.Count; i++)
        {
            Vector2 targetPos = itemPosDict[item] + (itemPosDict[changedItemList[i]] - itemPosDict[item]).normalized * radiateSize;
            changedItemList[i].DOAnchorPos(targetPos, 0.8f);
        }

        // 显示详情页
        ShowDetailPage(item);

        StartAutoRestoreTimer();
    }

    // 修复后的ShowDetailPage方法
    void ShowDetailPage(RectTransform item)
    {
        // 销毁现有的详情页
        if (currentDetailPage != null)
        {
            Destroy(currentDetailPage);
        }

        // 创建详情页对象
        currentDetailPage = new GameObject("DetailPage", typeof(RectTransform), typeof(Image));
        RectTransform detailRT = currentDetailPage.GetComponent<RectTransform>();
        detailRT.SetParent(transform);

        // 关键修复：使用目标位置而不是当前位置
        Vector2 targetItemPos = itemPosDict[item]; // 照片的目标位置
        Vector2 inwardOffset = CalculateInwardOffset(item);

        // 设置位置和尺寸
        detailRT.anchoredPosition = targetItemPos + inwardOffset; // 目标位置 + 偏移
        detailRT.sizeDelta = item.sizeDelta;

        // 复制锚点设置，确保位置计算一致
        detailRT.anchorMin = item.anchorMin;
        detailRT.anchorMax = item.anchorMax;
        detailRT.pivot = item.pivot;

        // 设置图片
        Image detailImage = currentDetailPage.GetComponent<Image>();
        if (itemDetailMap.ContainsKey(item))
        {
            string detailName = itemDetailMap[item];
            if (detailSprites.ContainsKey(detailName))
            {
                detailImage.sprite = detailSprites[detailName];
            }
            else
            {
                // 如果找不到详情页，使用原始图片作为备选
                detailImage.sprite = item.GetComponent<Image>().sprite;
                Debug.LogWarning($"使用原始图片作为详情页: {detailName} 未找到");
            }
        }

        // 为详情页添加点击事件
        AddClickEventToDetailPage(detailRT);

        // 设置初始状态和动画
        detailRT.localScale = Vector3.one;
        detailRT.DOScale(enlargeSize, 0.5f).SetEase(Ease.OutBack);

        // 确保详情页在正确层级
        detailRT.SetAsLastSibling();

        Debug.Log($"详情页位置: {detailRT.anchoredPosition}, 目标位置: {targetItemPos}, 偏移: {inwardOffset}");
    }

    // 为详情页添加点击事件
    void AddClickEventToDetailPage(RectTransform detailRT)
    {
        EventTrigger trigger = detailRT.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => {
            Debug.Log("详情页被点击");
            RestoreAllItems();
        });
        trigger.triggers.Add(entry);

        // 确保详情页可以接收点击
        Image image = detailRT.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }
    }

    void HideDetailPage()
    {
        if (currentDetailPage != null)
        {
            Destroy(currentDetailPage);
            currentDetailPage = null;
        }
    }

    Vector2 CalculateInwardOffset(RectTransform item)
    {
        if (!itemIndexDict.ContainsKey(item))
            return Vector2.zero;

        Vector2Int index = itemIndexDict[item];
        int i = index.x;
        int j = index.y;

        Vector2 offset = Vector2.zero;

        if (j == 0)
            offset.x = inwardMoveDistance;
        else if (j == column - 1)
            offset.x = -inwardMoveDistance;
        else if (j < column / 2)
            offset.x = inwardMoveDistance * (1 - (float)j / (column / 2));
        else
            offset.x = -inwardMoveDistance * ((float)(j - column / 2) / (column / 2));

        if (i == 0)
            offset.y = -inwardMoveDistance;
        else if (i == row - 1)
            offset.y = inwardMoveDistance;
        else if (i < row / 2)
            offset.y = -inwardMoveDistance * (1 - (float)i / (row / 2));
        else
            offset.y = inwardMoveDistance * ((float)(i - row / 2) / (row / 2));

        return offset;
    }

    void RestoreAllItems()
    {
        StopAutoRestoreTimer();

        if (currentSelectedItem != null)
        {
            currentSelectedItem.gameObject.SetActive(true);
            currentSelectedItem.DOAnchorPos(itemPosDict[currentSelectedItem], 0.5f);
        }

        for (int i = 0; i < changedItemList.Count; i++)
        {
            changedItemList[i].DOAnchorPos(itemPosDict[changedItemList[i]], 0.8f);
        }

        HideDetailPage();

        currentSelectedItem = null;
        isExpanded = false;
        changedItemList.Clear();

        Debug.Log("已恢复所有项目");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ResetAutoRestoreTimer();

        if (eventData.pointerCurrentRaycast.gameObject == null ||
            eventData.pointerCurrentRaycast.gameObject.transform.parent != transform)
        {
            RestoreAllItems();
        }
    }

    void StartAutoRestoreTimer()
    {
        StopAutoRestoreTimer();
        autoRestoreCoroutine = StartCoroutine(AutoRestoreCoroutine());
    }

    void StopAutoRestoreTimer()
    {
        if (autoRestoreCoroutine != null)
        {
            StopCoroutine(autoRestoreCoroutine);
            autoRestoreCoroutine = null;
        }
    }

    void ResetAutoRestoreTimer()
    {
        if (isExpanded)
        {
            StartAutoRestoreTimer();
        }
    }

    IEnumerator AutoRestoreCoroutine()
    {
        yield return new WaitForSeconds(autoRestoreTime);
        RestoreAllItems();
        autoRestoreCoroutine = null;
    }

    void OnDisable()
    {
        StopAutoRestoreTimer();
    }

    void OnDestroy()
    {
        StopAutoRestoreTimer();
    }
}