using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoWall : MonoBehaviour, IPointerClickHandler
{
    public RectTransform prefab; //照片预制体

    public int row = 10;       // 行
    public int column = 20;    // 列

    public int startXPos = 60;  // 起始X坐标（最左侧照片的位置）
    public int startYPos = -100;// 起始Y坐标

    public float distanceX = 65; //X轴间距值
    public float distanceY = 80; //Y轴间距值

    float initMoveDistance = 1800; //初始从右侧进入的移动距离

    float enlargeSize = 3;      //放大倍数
    float radiateSize = 500;    //扩散效果的半径范围

    List<List<RectTransform>> goList;  //二维列表，存储所有照片引用
    Dictionary<RectTransform, Vector2> itemPosDict;//字典，照片-目标位置
    List<RectTransform> changedItemList;  // 临时列表，存储受扩散效果影响的照片
    Sprite[] loadedSprites;               // 图片数组（从Resources加载）

    RectTransform currentSelectedItem; // 当前选中的照片
    bool isExpanded = false; // 是否已展开

    // Use this for initialization初始化
    void Start()
    {
        // 手动设置DOTween容量，避免自动扩容警告
        DOTween.SetTweensCapacity(2000, 100);

        goList = new List<List<RectTransform>>();
        itemPosDict = new Dictionary<RectTransform, Vector2>();
        changedItemList = new List<RectTransform>();
        // 从Resources文件夹加载所有图片
        LoadSpritesFromResources();

        CreateGos();
    }

    void LoadSpritesFromResources()
    {
        // 从Resources/Photos/文件夹加载所有Sprite
        loadedSprites = Resources.LoadAll<Sprite>("Photos/");

        // 检查是否成功加载
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogError("没有在Resources/Photos/文件夹中找到图片！请检查路径和文件格式。");
        }
        else
        {
            Debug.Log($"成功加载 {loadedSprites.Length} 张图片");
        }
    }

    void CreateGos()//创建照片墙
    {
        // 生成所有物体，并添加到字典
        for (int i = 0; i < row; i++)
        {
            List<RectTransform> gos = new List<RectTransform>();
            goList.Add(gos);
            float lastPosX = 0;
            for (int j = 0; j < column; j++)
            {
                //实例化照片预制体
                RectTransform item = (Instantiate(prefab.gameObject) as GameObject).GetComponent<RectTransform>();
                item.name = i + " " + j;
                item.transform.SetParent(transform);

                // === 设置图片 ===
                if (loadedSprites != null && loadedSprites.Length > 0)
                {
                    Image img = item.GetComponent<Image>();
                    if (img != null)
                    {
                        // 随机选择一张图片
                        int randomIndex = Random.Range(0, loadedSprites.Length);
                        img.sprite = loadedSprites[randomIndex];
                    }
                }

                // 计算目标位置（最终位置）
                Vector2 endPos = new Vector3(
                    startXPos + j * distanceX,      // 使用 startXPos
                    startYPos - i * distanceY
                );

                // 再计算起始位置（从右侧进入）
                Vector2 startPos = new Vector3(
                    endPos.x + initMoveDistance,  // 从右侧 initMoveDistance 距离处开始
                    endPos.y
                );

                item.anchoredPosition = startPos;

                //创建动画：起始位置到目标位置
                Tweener tweener = item.DOAnchorPosX(endPos.x, Random.Range(1.8f, 2f));  // 缓动到目标位置
                tweener.SetDelay(j * 0.1f + (row - i) * 0.1f);      // 延时
                tweener.SetEase(Ease.InSine);           // 缓动效果
                item.gameObject.SetActive(true);

                // 添加点击事件
                AddClickEventToItem(item);

                //存储引用
                gos.Add(item);
                itemPosDict.Add(item, endPos);//记录照片的最终位置

                lastPosX = item.anchoredPosition.x;//更新最后位置
            }
        }
    }

    // 为每个照片添加点击事件
    void AddClickEventToItem(RectTransform item)
    {
        // 添加EventTrigger组件
        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = item.gameObject.AddComponent<EventTrigger>();
        }

        // 创建点击事件
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnPhotoClick(item); });

        // 添加到EventTrigger
        trigger.triggers.Add(entry);
    }

    // 照片点击处理
    public void OnPhotoClick(RectTransform item)
    {
        // 如果已经有展开的照片，先恢复它
        if (isExpanded && currentSelectedItem != null)
        {
            // 如果点击的是同一个照片，则恢复所有
            if (currentSelectedItem == item)
            {
                RestoreAllItems();
                return;
            }
            else
            {
                // 如果点击的是不同照片，先恢复之前的，再展开新的
                RestoreAllItems();
            }
        }

        // 展开新点击的照片
        ExpandItem(item);
    }

    // 展开照片
    void ExpandItem(RectTransform item)
    {
        currentSelectedItem = item;
        isExpanded = true;

        // 缓动改变中心物体尺寸
        item.DOScale(enlargeSize, 0.5f);

        Vector2 pos = itemPosDict[item];

        changedItemList = new List<RectTransform>();

        // 添加扩散物体到集合
        foreach (KeyValuePair<RectTransform, Vector2> i in itemPosDict)
        {
            if (i.Key != item && Vector2.Distance(i.Value, pos) < radiateSize)
            {
                changedItemList.Add(i.Key);
            }
        }

        // 缓动来解决扩散物体的动画
        for (int i = 0; i < changedItemList.Count; i++)
        {
            Vector2 targetPos = itemPosDict[item] + (itemPosDict[changedItemList[i]] - itemPosDict[item]).normalized * radiateSize;
            changedItemList[i].DOAnchorPos(targetPos, 0.8f);
        }
    }

    // 恢复所有照片到原始状态
    void RestoreAllItems()
    {
        if (currentSelectedItem != null)
        {
            // 缓动恢复中心物体尺寸
            currentSelectedItem.DOScale(1, 0.5f);
        }

        // 缓动将扩散物体恢复到初始位置
        for (int i = 0; i < changedItemList.Count; i++)
        {
            changedItemList[i].DOAnchorPos(itemPosDict[changedItemList[i]], 0.8f);
        }

        // 重置状态
        currentSelectedItem = null;
        isExpanded = false;
        changedItemList.Clear();
    }

    // 实现IPointerClickHandler接口，用于在空白处点击恢复
    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查是否点击了空白处（不是照片）
        if (eventData.pointerCurrentRaycast.gameObject == null ||
            eventData.pointerCurrentRaycast.gameObject.transform.parent != transform)
        {
            RestoreAllItems();
        }
    }
}