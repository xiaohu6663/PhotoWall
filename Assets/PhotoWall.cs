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

    [Header("详情页设置")]
    public GameObject detailPagePrefab; // 详情页预制体
    public Sprite detailBackground; // 详情页背景图片
    public PhotoDataSO photoData; // 照片数据

    List<List<RectTransform>> goList;  //二维列表，存储所有照片引用
    Dictionary<RectTransform, Vector2> itemPosDict;//字典，照片-目标位置
    Dictionary<RectTransform, Vector2Int> itemIndexDict; // 字典，照片-行列索引
    List<RectTransform> changedItemList;  // 临时列表，存储受扩散效果影响的照片
    Sprite[] loadedSprites;               // 图片数组（从Resources加载）
    Dictionary<string, Sprite> detailSprites; // 详情页图片字典

    RectTransform currentSelectedItem; // 当前选中的照片
    GameObject currentDetailPage; // 当前显示的详情页
    bool isExpanded = false; // 是否已展开
    Coroutine autoRestoreCoroutine; // 自动恢复协程

    void Start()
    {
        DOTween.SetTweensCapacity(2000, 100);

        goList = new List<List<RectTransform>>();
        itemPosDict = new Dictionary<RectTransform, Vector2>();
        itemIndexDict = new Dictionary<RectTransform, Vector2Int>();
        changedItemList = new List<RectTransform>();
        detailSprites = new Dictionary<string, Sprite>();

        LoadSpritesFromResources();
        LoadDetailSprites();

        // 初始化PhotoData
        if (photoData != null)
        {
            photoData.InitializeDictionary();
            Debug.Log($"PhotoData 初始化完成，共有 {photoData.photoItems.Count} 条数据");
        }
        else
        {
            Debug.LogError("PhotoData 未分配！");
        }

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

                // 设置图片 - 直接按顺序使用，不洗牌
                if (loadedSprites != null && loadedSprites.Length > 0 && photoIndex < loadedSprites.Length)
                {
                    Image img = item.GetComponent<Image>();
                    if (img != null)
                    {
                        // 直接使用顺序索引
                        Sprite selectedSprite = loadedSprites[photoIndex];
                        img.sprite = selectedSprite;
                        photoIndex++;

                        Debug.Log($"设置图片: {selectedSprite.name} 到位置 ({i},{j})");
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

    void ShowDetailPage(RectTransform item)
    {
        // 销毁现有的详情页
        if (currentDetailPage != null)
        {
            Destroy(currentDetailPage);
        }

        // 检查详情页预制体
        if (detailPagePrefab == null)
        {
            Debug.LogError("详情页预制体未设置！");
            return;
        }

        // 实例化详情页
        currentDetailPage = Instantiate(detailPagePrefab, transform);
        DetailPageController detailController = currentDetailPage.GetComponent<DetailPageController>();

        if (detailController == null)
        {
            Debug.LogError("详情页预制体缺少DetailPageController组件！");
            return;
        }

        // 获取图片数据
        string imageName = item.GetComponent<Image>().sprite.name;
        if (imageName.Contains("."))
        {
            imageName = System.IO.Path.GetFileNameWithoutExtension(imageName);
        }

        Debug.Log($"正在查找图片数据: {imageName}");

        PhotoDataSO.PhotoItem photoItem = null;
        if (photoData != null)
        {
            photoItem = photoData.GetPhotoItem(imageName);
            if (photoItem == null)
            {
                Debug.LogWarning($"未找到图片 '{imageName}' 的数据");
            }
        }
        else
        {
            Debug.LogError("PhotoData 未分配！");
        }

        if (photoItem == null)
        {
            Debug.LogWarning($"未找到图片 '{imageName}' 的数据，使用默认数据");
            photoItem = new PhotoDataSO.PhotoItem(imageName, "未命名展品", "暂无简介");
        }
        else
        {
            Debug.Log($"找到数据: {photoItem.title} - {photoItem.description}");
        }

        // 计算位置 - 与原脚本完全一致
        Vector2 targetItemPos = itemPosDict[item]; // 照片的目标位置
        Vector2 inwardOffset = CalculateInwardOffset(item);

        // 设置详情页的位置和大小 - 与原脚本行为一致
        RectTransform detailRT = currentDetailPage.GetComponent<RectTransform>();

        // 复制原始照片的锚点和轴心点设置
        detailRT.anchorMin = item.anchorMin;
        detailRT.anchorMax = item.anchorMax;
        detailRT.pivot = item.pivot;

        // 设置位置和尺寸 - 与原脚本完全一致
        detailRT.anchoredPosition = targetItemPos + inwardOffset; // 目标位置 + 偏移
        detailRT.sizeDelta = item.sizeDelta;

        // 计算合适的放大倍数，确保详情页不会超出屏幕
        float calculatedEnlargeSize = CalculateOptimalEnlargeSize(item.sizeDelta);

        Debug.Log($"详情页设置 - 位置: {detailRT.anchoredPosition}, 原始尺寸: {item.sizeDelta}, 使用放大倍数: {calculatedEnlargeSize}");

        // 显示详情页 - 使用计算后的放大倍数
        detailController.ShowDetail(
            photoItem,
            item.GetComponent<Image>().sprite, // 原图
            detailBackground,                  // 背景图
            detailRT.anchoredPosition,         // 使用计算好的位置
            calculatedEnlargeSize              // 使用计算后的放大倍数
        );

        // 为详情页添加关闭监听
        detailController.AddCloseListener(() => {
            RestoreAllItems();
        });
    }

    // 计算最佳放大倍数，确保详情页不会超出屏幕
    float CalculateOptimalEnlargeSize(Vector2 originalSize)
    {
        // 获取画布尺寸
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("无法找到Canvas，使用默认放大倍数");
            return enlargeSize;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;

        // 计算最大可用尺寸（留出边距）
        float margin = 50f; // 边距
        Vector2 maxAvailableSize = canvasSize - new Vector2(margin * 2, margin * 2);

        // 计算水平和垂直方向的最大放大倍数
        float maxHorizontalScale = maxAvailableSize.x / originalSize.x;
        float maxVerticalScale = maxAvailableSize.y / originalSize.y;

        // 取较小的那个，确保图片完全在屏幕内
        float maxScale = Mathf.Min(maxHorizontalScale, maxVerticalScale);

        // 使用用户设置的放大倍数，但不能超过最大可用尺寸
        return Mathf.Min(enlargeSize, maxScale);
    }

    void HideDetailPage()
    {
        if (currentDetailPage != null)
        {
            DetailPageController detailController = currentDetailPage.GetComponent<DetailPageController>();
            if (detailController != null)
            {
                detailController.HideDetail();
                // 延迟销毁，让动画完成
                Destroy(currentDetailPage, 0.5f);
            }
            else
            {
                Destroy(currentDetailPage);
            }
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