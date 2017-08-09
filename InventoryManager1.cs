using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryManager : MonoSingleton<InventoryManager>
{
    /// <summary>
    /// 物品信息列表
    /// </summary>
    private List<Item> itemList = new List<Item>();
    private TextAsset itemText;
    public ItemUI SlotItem { get; set; }
    public Slot PreviousSlot { get; set; }
    #region ToolTip
    private ToolTip toolTip;
    private bool isToolTipShow = false;
    private Vector2 toolTipOffset = new Vector2(20, -20);
    #endregion
    private Canvas canvas;
    #region pickedItem
    private bool isPicked = false;
    public bool IsPicked
    {
        get
        {
            return isPicked;
        }
        set
        {
            isPicked = value;
        }
    }
    private ItemUI pickedItem; //鼠标选中的物体
    public ItemUI PickedItem
    {
        get
        {
            return pickedItem;
        }
    }
    #endregion
    public SplitUI SplitUI { get; private set; }
    private ThrowAwayUI throwAwayUI;
    private Player player;
    private void Awake()
    {
        ParseItemJson();
    }

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        toolTip = transform.FindChild("Tooltip").GetComponent<ToolTip>();
        pickedItem = transform.FindChild("PickedItem").GetComponent<ItemUI>();
        pickedItem.Hide();
        SplitUI = transform.FindChild("Panel_BatchTransItems").GetComponent<SplitUI>();
        SplitUI.Hide();
        throwAwayUI = transform.FindChild("Panel_ThrowAwayItem").GetComponent<ThrowAwayUI>();
        throwAwayUI.Hide();
    }

    /// <summary>
    /// 光标是否停留在UI上
    /// </summary>
    private bool IsPointerOverUI()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        //PC 或者 编译器
        return EventSystem.current.IsPointerOverGameObject();
#elif UNITY_IPHONE || UNITY_ANDROID
        //苹果 或者 安卓
       return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
    }

    private void Update()
    {
        if (isToolTipShow)
        {
            //控制提示面板跟随鼠标
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
            toolTip.SetLocalPosition(position + toolTipOffset);
        }
        //如果物品被拾取，则让物品跟随鼠标移动
        if(isPicked)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
            pickedItem.SetLocalPosition(position);
        }
        if (isPicked && Input.GetMouseButtonDown(0) && IsPointerOverUI() == false)
        {
            throwAwayUI.Show();
            throwAwayUI.PlayAnimation();
            throwAwayUI.SetThrowAwayNumDes(pickedItem);
            pickedItem.Hide();
        }
    }
    /// <summary>
    /// 解析物品信息
    /// </summary>
    private void ParseItemJson()
    {
        //文本在Unity里是TextAsset类型
        itemText = Resources.Load<TextAsset>("ItemJson");
        //string itemContent = itemText.ToString();
        string itemContent = itemText.text;

        JsonData json = JsonMapper.ToObject(itemContent);
        //下面的是解析这个对象里面的共有属性
        for (int i = 0; i < json.Count; i++)
        {
            int id = int.Parse(json[i]["id"].ToString());
            string name = json[i]["name"].ToString();
            Item.ItemType type = (Item.ItemType)Enum.Parse(typeof(Item.ItemType), json[i]["type"].ToString());
            Item.ItemQuality quality = (Item.ItemQuality)Enum.Parse(typeof(Item.ItemQuality), json[i]["quality"].ToString());
            string description = json[i]["description"].ToString();
            int capacity = int.Parse(json[i]["capacity"].ToString());
            int buyPrice = int.Parse(json[i]["buyPrice"].ToString());
            int sellPrice = int.Parse(json[i]["sellPrice"].ToString());
            string sprite = json[i]["sprite"].ToString();

            Item item = null;
            switch (type)
            {
                case Item.ItemType.Consumable:
                    int hp = int.Parse(json[i]["hp"].ToString());
                    int mp = int.Parse(json[i]["mp"].ToString());
                    item = new Consumable(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, hp, mp);
                    break;
                case Item.ItemType.Equipment:
                    int strength = int.Parse(json[i]["strength"].ToString());
                    int intellect = int.Parse(json[i]["intellect"].ToString());
                    int agility = int.Parse(json[i]["agility"].ToString());
                    int stamina = int.Parse(json[i]["stamina"].ToString());
                    Equipment.EquipmentType equipType = (Equipment.EquipmentType)Enum.Parse(typeof(Equipment.EquipmentType), json[i]["equipType"].ToString());
                    item = new Equipment(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, strength, intellect, agility, stamina, equipType);
                    break;
                case Item.ItemType.Weapon:
                    int damage = int.Parse(json[i]["damage"].ToString());
                    Weapon.WeaponType wpType = (Weapon.WeaponType)Enum.Parse(typeof(Weapon.WeaponType), json[i]["weaponType"].ToString());
                    item = new Weapon(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite, damage, wpType);
                    break;
                case Item.ItemType.Material:
                    item = new Material(id, name, type, quality, description, capacity, buyPrice, sellPrice, sprite);
                    break;
            }
        itemList.Add(item);
        }
    }

    public Item GetItemByID(int id)
    {
        foreach (var item in itemList)
        {
            if (item.ID == id)
            {
                return item;
            }
        }
        return null;
    }

    public void ShowToolTip(string content)
    {
        //捡起物品，隐藏所有Tooltip
        //if (isPicked) return;
        isToolTipShow = true;
        toolTip.Show(content);
    }

    public void HideToolTip()
    {
        isToolTipShow = false;
        toolTip.Hide();
    }

    /// <summary>
    /// 捡起物品槽中指定数量的物品
    /// </summary>
    public void PickUpItem(Item item, int amount)
    {
        pickedItem.SetItem(item, amount);
        pickedItem.Show();
        isPicked = true;
        //捡起物品后隐藏所捡起物品的Tooltip
        this.toolTip.Hide();
        //控制提示面板跟随鼠标
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, null, out position);
        toolTip.SetLocalPosition(position + toolTipOffset);
    }

    /// <summary>
    /// 从鼠标上拿掉指定个数物品放在物品槽里面
    /// </summary>
    public void RemoveItem(int amount =1)
    {
        PickedItem.ReduceAmount(amount);
        if (PickedItem.Amount <= 0)
        {
            isPicked = false;
            PickedItem.Hide();
        }
    }

    /// <summary>
    /// 交换两个物品槽中的物品
    /// </summary>
    public void ExchangeItem(Item item, int amount)
    {
        PreviousSlot.StoreItem(item);
        PreviousSlot.GetComponentInChildren<ItemUI>().SetItem(item, amount);
        PickedItem.Hide();
        IsPicked = false;
    }

    public void SaveInventory()
    {
        Knapsack.instance.SaveInventory();
        Chest.instance.SaveInventory();
        CharacterPanel.instance.SaveInventory();
        Forge.instance.SaveInventory();
        PlayerPrefs.SetInt("CoinAmount", player.CoinAmount);
    }

    public void LoadInventory()
    {
        Knapsack.instance.LoadInventory();
        Chest.instance.LoadInventory();
        CharacterPanel.instance.LoadInventory();
        Forge.instance.LoadInventory();
        player.CoinAmount = PlayerPrefs.GetInt("CoinAmount", 0);
    }
}
