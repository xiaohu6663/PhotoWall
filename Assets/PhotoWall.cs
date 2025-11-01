using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoWall : MonoBehaviour, IPointerClickHandler
{
    public RectTransform prefab; //��ƬԤ����

    public int row = 6;       // ��
    public int column = 8;    // ��

    public int startXPos = 60;  // ��ʼX���꣨�������Ƭ��λ�ã�
    public int startYPos = -100;// ��ʼY����

    public float distanceX = 65; //X����ֵ
    public float distanceY = 80; //Y����ֵ

    float initMoveDistance = 1800; //��ʼ���Ҳ������ƶ�����

    [Header("�Ŵ�����")]
    public float enlargeSize = 5f;      //�Ŵ���

    float radiateSize = 550;    //��ɢЧ���İ뾶��Χ

    [Header("��Χ��Ƭ�ƶ�����")]
    public float inwardMoveDistance = 100f; // ��Χ��Ƭ�����ƶ��ľ���

    [Header("�Զ��ָ�����")]
    public float autoRestoreTime = 30f; // �Զ��ָ�ʱ�䣨��")

    List<List<RectTransform>> goList;  //��ά�б��洢������Ƭ����
    Dictionary<RectTransform, Vector2> itemPosDict;//�ֵ䣬��Ƭ-Ŀ��λ��
    Dictionary<RectTransform, Vector2Int> itemIndexDict; // �ֵ䣬��Ƭ-��������
    Dictionary<RectTransform, string> itemDetailMap; // �ֵ䣬��Ƭ-����ҳͼƬ����ӳ��
    List<RectTransform> changedItemList;  // ��ʱ�б��洢����ɢЧ��Ӱ�����Ƭ
    Sprite[] loadedSprites;               // ͼƬ���飨��Resources���أ�
    Dictionary<string, Sprite> detailSprites; // ����ҳͼƬ�ֵ�

    RectTransform currentSelectedItem; // ��ǰѡ�е���Ƭ
    GameObject currentDetailPage; // ��ǰ��ʾ������ҳ����ΪGameObject��
    bool isExpanded = false; // �Ƿ���չ��
    Coroutine autoRestoreCoroutine; // �Զ��ָ�Э��

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
            Debug.LogError("û����Resources/Photos/�ļ������ҵ�ͼƬ��");
        }
        else
        {
            Debug.Log($"�ɹ����� {loadedSprites.Length} ��ͼƬ");

            // ���ͼƬ�����Ƿ�ƥ��
            if (loadedSprites.Length != row * column)
            {
                Debug.LogWarning($"ͼƬ������ƥ��: ��Ҫ {row * column} �ţ����ҵ� {loadedSprites.Length} ��");
            }
        }
    }

    void LoadDetailSprites()
    {
        Sprite[] details = Resources.LoadAll<Sprite>("Details/");
        if (details == null || details.Length == 0)
        {
            Debug.LogError("û����Resources/Details/�ļ������ҵ�����ҳͼƬ��");
        }
        else
        {
            foreach (Sprite detail in details)
            {
                detailSprites[detail.name] = detail;
            }
            Debug.Log($"�ɹ����� {details.Length} ������ҳͼƬ");
        }
    }

    void CreateGos()//�볡����
    {
        // ����ͼƬ�����б�ϴ��
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

                // ����ͼƬ - ʹ��ϴ�ƺ��˳��
                if (loadedSprites != null && loadedSprites.Length > 0)
                {
                    Image img = item.GetComponent<Image>();
                    if (img != null)
                    {
                        // ʹ��ϴ�ƺ������
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

        Debug.Log($"�ɹ����� {row * column} ����Ƭ��ʹ���� {photoIndex} ��ͼƬ");
    }

    // ��ȡϴ�ƺ��ͼƬ����
    List<int> GetShuffledPhotoIndices()
    {
        List<int> indices = new List<int>();

        // �������ͼƬ������
        for (int i = 0; i < loadedSprites.Length; i++)
        {
            indices.Add(i);
        }

        // Fisher-Yates ϴ���㷨 - ȷ����������ظ�
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

        // ����ԭʼͼƬ
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

        // ��ʾ����ҳ
        ShowDetailPage(item);

        StartAutoRestoreTimer();
    }

    // �޸����ShowDetailPage����
    void ShowDetailPage(RectTransform item)
    {
        // �������е�����ҳ
        if (currentDetailPage != null)
        {
            Destroy(currentDetailPage);
        }

        // ��������ҳ����
        currentDetailPage = new GameObject("DetailPage", typeof(RectTransform), typeof(Image));
        RectTransform detailRT = currentDetailPage.GetComponent<RectTransform>();
        detailRT.SetParent(transform);

        // �ؼ��޸���ʹ��Ŀ��λ�ö����ǵ�ǰλ��
        Vector2 targetItemPos = itemPosDict[item]; // ��Ƭ��Ŀ��λ��
        Vector2 inwardOffset = CalculateInwardOffset(item);

        // ����λ�úͳߴ�
        detailRT.anchoredPosition = targetItemPos + inwardOffset; // Ŀ��λ�� + ƫ��
        detailRT.sizeDelta = item.sizeDelta;

        // ����ê�����ã�ȷ��λ�ü���һ��
        detailRT.anchorMin = item.anchorMin;
        detailRT.anchorMax = item.anchorMax;
        detailRT.pivot = item.pivot;

        // ����ͼƬ
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
                // ����Ҳ�������ҳ��ʹ��ԭʼͼƬ��Ϊ��ѡ
                detailImage.sprite = item.GetComponent<Image>().sprite;
                Debug.LogWarning($"ʹ��ԭʼͼƬ��Ϊ����ҳ: {detailName} δ�ҵ�");
            }
        }

        // Ϊ����ҳ��ӵ���¼�
        AddClickEventToDetailPage(detailRT);

        // ���ó�ʼ״̬�Ͷ���
        detailRT.localScale = Vector3.one;
        detailRT.DOScale(enlargeSize, 0.5f).SetEase(Ease.OutBack);

        // ȷ������ҳ����ȷ�㼶
        detailRT.SetAsLastSibling();

        Debug.Log($"����ҳλ��: {detailRT.anchoredPosition}, Ŀ��λ��: {targetItemPos}, ƫ��: {inwardOffset}");
    }

    // Ϊ����ҳ��ӵ���¼�
    void AddClickEventToDetailPage(RectTransform detailRT)
    {
        EventTrigger trigger = detailRT.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => {
            Debug.Log("����ҳ�����");
            RestoreAllItems();
        });
        trigger.triggers.Add(entry);

        // ȷ������ҳ���Խ��յ��
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

        Debug.Log("�ѻָ�������Ŀ");
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