using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoWall : MonoBehaviour
{

    public RectTransform prefab; //照片预制体
    

    public int row = 10;       // 行
    public int column = 20;        // 列

    int startXPos = 60;  // 起始X坐标（最左侧照片的位置）
    public int startYPos = -100;// 起始Y坐标

    public float distanceX = 65; //X轴间距值；每张照片的X坐标 = 上一张照片的X坐标 + 随机间距值
    
    public float distanceY = 80;//Y轴间距值;每行的Y坐标 = 起始Y坐标 - 行号 × 随机垂直间距

    float initMoveDistance = 1800; //初始从右侧进入的移动距离

    float enlargeSize = 2;      //放大倍数

    float radiateSize = 220;    //扩散效果的半径范围

    List<List<RectTransform>> goList;  //二维列表，存储所有照片引用
    Dictionary<RectTransform, Vector2> itemPosDict;//字典，照片-目标位置
    List<RectTransform> changedItemList;  // 临时列表，存储受扩散效果影响的照片
    Sprite[] loadedSprites;               // 图片数组（从Resources加载）
    
    
    
    // Use this for initialization初始化
    void Start()
    {

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


                Vector2 startPos = new Vector3(distanceX + lastPosX, startYPos - i *distanceY);
                item.anchoredPosition = startPos;
                
                //计算目标位置
                Vector2 endPos = new Vector3(startPos.x - initMoveDistance, startYPos - i * distanceY);
                
                //创建动画：起始位置到目标位置
                Tweener tweener = item.DOAnchorPosX(endPos.x, Random.Range(1.8f, 2f));  // 缓动到目标位置
                tweener.SetDelay(j * 0.1f + (row - i) * 0.1f);      // 延时
                tweener.SetEase(Ease.InSine);           // 缓动效果
                item.gameObject.SetActive(true);
                
                //存储引用
                gos.Add(item);
                itemPosDict.Add(item, endPos);//记录照片的最终位置

                lastPosX = item.anchoredPosition.x;//更新最后位置
            }
        }
    }
    

    public void OnMousePointEnter(RectTransform item)//鼠标进入处理（悬停）
    {
        // 缓动改变中心物体尺寸
        item.DOScale(enlargeSize, 0.5f);

        Vector2 pos = itemPosDict[item];

        changedItemList = new List<RectTransform>();

        // 添加扩散物体到集合
        foreach (KeyValuePair<RectTransform, Vector2> i in itemPosDict)
        {
            if (Vector2.Distance(i.Value, pos) < radiateSize)
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

    public void OnMousePointExit(RectTransform go)//鼠标离开处理
    {
        // 缓动恢复中心物体尺寸
        go.DOScale(1, 1);
        // 缓动将扩散物体恢复到初始位置
        for (int i = 0; i < changedItemList.Count; i++)
        {
            changedItemList[i].DOAnchorPos(itemPosDict[changedItemList[i]], 0.8f);
        }
    }
}