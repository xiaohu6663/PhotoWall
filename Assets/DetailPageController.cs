using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DetailPageController : MonoBehaviour
{
    [Header("UI组件")]
    public Image background;          // 背景图片
    public Image photoImage;          // 展品图片
    public TextMeshProUGUI titleText; // 展品名称
    public TextMeshProUGUI descriptionText; // 简介文本

    [Header("动画设置")]
    public float showDuration = 0.5f;
    public float hideDuration = 0.3f;

    private RectTransform rectTransform;
    private Vector2 originalSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;

        // 检查UI组件是否已赋值
        CheckUIComponents();
    }

    void CheckUIComponents()
    {
        if (background == null)
            Debug.LogError("DetailPageController: Background 未赋值！");
        if (photoImage == null)
            Debug.LogError("DetailPageController: PhotoImage 未赋值！");
        if (titleText == null)
            Debug.LogError("DetailPageController: TitleText 未赋值！");
        if (descriptionText == null)
            Debug.LogError("DetailPageController: DescriptionText 未赋值！");
    }

    public void ShowDetail(PhotoDataSO.PhotoItem data, Sprite photoSprite, Sprite bgSprite, Vector2 position, float enlargeSize)
    {
        try
        {
            // 参数检查
            if (data == null)
            {
                Debug.LogError("ShowDetail: data 参数为 null");
                return;
            }

            if (photoSprite == null)
            {
                Debug.LogError($"ShowDetail: photoSprite 参数为 null, 图片名: {data.imageName}");
                return;
            }

            // 设置数据
            if (bgSprite != null)
            {
                background.sprite = bgSprite;
            }
            else
            {
                Debug.LogWarning("ShowDetail: bgSprite 为 null，使用默认背景");
            }

            photoImage.sprite = photoSprite;

            // 确保正确设置标题和简介
            titleText.text = !string.IsNullOrEmpty(data.title) ? data.title : "未命名展品";
            descriptionText.text = !string.IsNullOrEmpty(data.description) ? data.description : "暂无简介";

            // 强制刷新文本显示
            titleText.ForceMeshUpdate();
            descriptionText.ForceMeshUpdate();

            // 设置位置 - 使用传入的位置
            rectTransform.anchoredPosition = position;

            // 设置初始状态 - 重要：初始缩放为1，然后使用传入的enlargeSize进行缩放
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = originalSize;

            // 显示对象
            gameObject.SetActive(true);

            // 使用传入的放大倍数进行缩放 - 只做一次缩放
            rectTransform.DOScale(enlargeSize, showDuration).SetEase(Ease.OutBack);

            // 确保在最前面
            transform.SetAsLastSibling();

            Debug.Log($"显示详情: 标题='{titleText.text}', 简介='{descriptionText.text}', 位置: {position}, 放大倍数: {enlargeSize}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ShowDetail 方法出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
        }
    }

    public void HideDetail()
    {
        // 缩放到0然后隐藏
        rectTransform.DOScale(0f, hideDuration).SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }

    // 添加点击事件来关闭详情页
    public void AddCloseListener(System.Action callback)
    {
        // 使用背景作为关闭按钮
        Button bgButton = background.GetComponent<Button>();
        if (bgButton == null)
            bgButton = background.gameObject.AddComponent<Button>();

        bgButton.onClick.RemoveAllListeners();
        bgButton.onClick.AddListener(() => callback?.Invoke());

        // 确保背景可以接收点击
        background.raycastTarget = true;

        // 同时为图片添加点击事件
        Button photoButton = photoImage.GetComponent<Button>();
        if (photoButton == null)
            photoButton = photoImage.gameObject.AddComponent<Button>();

        photoButton.onClick.RemoveAllListeners();
        photoButton.onClick.AddListener(() => callback?.Invoke());

        photoImage.raycastTarget = true;
    }
}