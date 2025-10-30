using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoWall : MonoBehaviour, IPointerClickHandler
{
    public RectTransform prefab; //��ƬԤ����

    public int row = 10;       // ��
    public int column = 20;    // ��

    public int startXPos = 60;  // ��ʼX���꣨�������Ƭ��λ�ã�
    public int startYPos = -100;// ��ʼY����

    public float distanceX = 65; //X����ֵ
    public float distanceY = 80; //Y����ֵ

    float initMoveDistance = 1800; //��ʼ���Ҳ������ƶ�����

    float enlargeSize = 3;      //�Ŵ���
    float radiateSize = 500;    //��ɢЧ���İ뾶��Χ

    List<List<RectTransform>> goList;  //��ά�б��洢������Ƭ����
    Dictionary<RectTransform, Vector2> itemPosDict;//�ֵ䣬��Ƭ-Ŀ��λ��
    List<RectTransform> changedItemList;  // ��ʱ�б��洢����ɢЧ��Ӱ�����Ƭ
    Sprite[] loadedSprites;               // ͼƬ���飨��Resources���أ�

    RectTransform currentSelectedItem; // ��ǰѡ�е���Ƭ
    bool isExpanded = false; // �Ƿ���չ��

    // Use this for initialization��ʼ��
    void Start()
    {
        // �ֶ�����DOTween�����������Զ����ݾ���
        DOTween.SetTweensCapacity(2000, 100);

        goList = new List<List<RectTransform>>();
        itemPosDict = new Dictionary<RectTransform, Vector2>();
        changedItemList = new List<RectTransform>();
        // ��Resources�ļ��м�������ͼƬ
        LoadSpritesFromResources();

        CreateGos();
    }

    void LoadSpritesFromResources()
    {
        // ��Resources/Photos/�ļ��м�������Sprite
        loadedSprites = Resources.LoadAll<Sprite>("Photos/");

        // ����Ƿ�ɹ�����
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogError("û����Resources/Photos/�ļ������ҵ�ͼƬ������·�����ļ���ʽ��");
        }
        else
        {
            Debug.Log($"�ɹ����� {loadedSprites.Length} ��ͼƬ");
        }
    }

    void CreateGos()//������Ƭǽ
    {
        // �����������壬����ӵ��ֵ�
        for (int i = 0; i < row; i++)
        {
            List<RectTransform> gos = new List<RectTransform>();
            goList.Add(gos);
            float lastPosX = 0;
            for (int j = 0; j < column; j++)
            {
                //ʵ������ƬԤ����
                RectTransform item = (Instantiate(prefab.gameObject) as GameObject).GetComponent<RectTransform>();
                item.name = i + " " + j;
                item.transform.SetParent(transform);

                // === ����ͼƬ ===
                if (loadedSprites != null && loadedSprites.Length > 0)
                {
                    Image img = item.GetComponent<Image>();
                    if (img != null)
                    {
                        // ���ѡ��һ��ͼƬ
                        int randomIndex = Random.Range(0, loadedSprites.Length);
                        img.sprite = loadedSprites[randomIndex];
                    }
                }

                // ����Ŀ��λ�ã�����λ�ã�
                Vector2 endPos = new Vector3(
                    startXPos + j * distanceX,      // ʹ�� startXPos
                    startYPos - i * distanceY
                );

                // �ټ�����ʼλ�ã����Ҳ���룩
                Vector2 startPos = new Vector3(
                    endPos.x + initMoveDistance,  // ���Ҳ� initMoveDistance ���봦��ʼ
                    endPos.y
                );

                item.anchoredPosition = startPos;

                //������������ʼλ�õ�Ŀ��λ��
                Tweener tweener = item.DOAnchorPosX(endPos.x, Random.Range(1.8f, 2f));  // ������Ŀ��λ��
                tweener.SetDelay(j * 0.1f + (row - i) * 0.1f);      // ��ʱ
                tweener.SetEase(Ease.InSine);           // ����Ч��
                item.gameObject.SetActive(true);

                // ��ӵ���¼�
                AddClickEventToItem(item);

                //�洢����
                gos.Add(item);
                itemPosDict.Add(item, endPos);//��¼��Ƭ������λ��

                lastPosX = item.anchoredPosition.x;//�������λ��
            }
        }
    }

    // Ϊÿ����Ƭ��ӵ���¼�
    void AddClickEventToItem(RectTransform item)
    {
        // ���EventTrigger���
        EventTrigger trigger = item.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = item.gameObject.AddComponent<EventTrigger>();
        }

        // ��������¼�
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnPhotoClick(item); });

        // ��ӵ�EventTrigger
        trigger.triggers.Add(entry);
    }

    // ��Ƭ�������
    public void OnPhotoClick(RectTransform item)
    {
        // ����Ѿ���չ������Ƭ���Ȼָ���
        if (isExpanded && currentSelectedItem != null)
        {
            // ����������ͬһ����Ƭ����ָ�����
            if (currentSelectedItem == item)
            {
                RestoreAllItems();
                return;
            }
            else
            {
                // ���������ǲ�ͬ��Ƭ���Ȼָ�֮ǰ�ģ���չ���µ�
                RestoreAllItems();
            }
        }

        // չ���µ������Ƭ
        ExpandItem(item);
    }

    // չ����Ƭ
    void ExpandItem(RectTransform item)
    {
        currentSelectedItem = item;
        isExpanded = true;

        // �����ı���������ߴ�
        item.DOScale(enlargeSize, 0.5f);

        Vector2 pos = itemPosDict[item];

        changedItemList = new List<RectTransform>();

        // �����ɢ���嵽����
        foreach (KeyValuePair<RectTransform, Vector2> i in itemPosDict)
        {
            if (i.Key != item && Vector2.Distance(i.Value, pos) < radiateSize)
            {
                changedItemList.Add(i.Key);
            }
        }

        // �����������ɢ����Ķ���
        for (int i = 0; i < changedItemList.Count; i++)
        {
            Vector2 targetPos = itemPosDict[item] + (itemPosDict[changedItemList[i]] - itemPosDict[item]).normalized * radiateSize;
            changedItemList[i].DOAnchorPos(targetPos, 0.8f);
        }
    }

    // �ָ�������Ƭ��ԭʼ״̬
    void RestoreAllItems()
    {
        if (currentSelectedItem != null)
        {
            // �����ָ���������ߴ�
            currentSelectedItem.DOScale(1, 0.5f);
        }

        // ��������ɢ����ָ�����ʼλ��
        for (int i = 0; i < changedItemList.Count; i++)
        {
            changedItemList[i].DOAnchorPos(itemPosDict[changedItemList[i]], 0.8f);
        }

        // ����״̬
        currentSelectedItem = null;
        isExpanded = false;
        changedItemList.Clear();
    }

    // ʵ��IPointerClickHandler�ӿڣ������ڿհ״�����ָ�
    public void OnPointerClick(PointerEventData eventData)
    {
        // ����Ƿ����˿հ״���������Ƭ��
        if (eventData.pointerCurrentRaycast.gameObject == null ||
            eventData.pointerCurrentRaycast.gameObject.transform.parent != transform)
        {
            RestoreAllItems();
        }
    }
}