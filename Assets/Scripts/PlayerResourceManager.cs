using UnityEngine;

public class PlayerResourceManager : MonoBehaviour
{
    public static PlayerResourceManager Instance { get; private set; }

    public const int ItemIdGold = 101;
    public const int ItemIdPetTicket = 102;
    public const int ItemIdMagicBottle = 103;
    public const int ItemIdPetFood = 104;

    const string KeyGold = "Gold";
    const string KeyPetTickets = "PetTickets";
    const string KeyMagicBottles = "MagicBottles";
    const string KeyPetFood = "PetFood";

    public int Gold { get; private set; }
    public int PetTickets { get; private set; }
    public int MagicBottles { get; private set; }
    public int PetFood { get; private set; }

    public event System.Action OnResourceChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Gold = Mathf.Max(0, PlayerPrefs.GetInt(KeyGold, 0));
        PetTickets = Mathf.Max(0, PlayerPrefs.GetInt(KeyPetTickets, 0));
        MagicBottles = Mathf.Max(0, PlayerPrefs.GetInt(KeyMagicBottles, 0));
        PetFood = Mathf.Max(0, PlayerPrefs.GetInt(KeyPetFood, 0));
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        Gold += amount;
        PlayerPrefs.SetInt(KeyGold, Gold);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount) return false;

        Gold -= amount;
        PlayerPrefs.SetInt(KeyGold, Gold);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
        return true;
    }

    public void AddPetTickets(int amount)
    {
        if (amount <= 0) return;

        PetTickets += amount;
        PlayerPrefs.SetInt(KeyPetTickets, PetTickets);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }

    public bool SpendPetTickets(int amount)
    {
        if (amount <= 0 || PetTickets < amount) return false;

        PetTickets -= amount;
        PlayerPrefs.SetInt(KeyPetTickets, PetTickets);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
        return true;
    }

    public void AddMagicBottles(int amount)
    {
        if (amount <= 0) return;

        MagicBottles += amount;
        PlayerPrefs.SetInt(KeyMagicBottles, MagicBottles);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }

    public bool SpendMagicBottles(int amount)
    {
        if (amount <= 0 || MagicBottles < amount) return false;

        MagicBottles -= amount;
        PlayerPrefs.SetInt(KeyMagicBottles, MagicBottles);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
        return true;
    }

    public void AddPetFood(int amount)
    {
        if (amount <= 0) return;

        PetFood += amount;
        PlayerPrefs.SetInt(KeyPetFood, PetFood);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }

    public bool SpendPetFood(int amount)
    {
        if (amount <= 0 || PetFood < amount) return false;

        PetFood -= amount;
        PlayerPrefs.SetInt(KeyPetFood, PetFood);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
        return true;
    }

    public bool AddItem(int itemId, int amount)
    {
        switch (itemId)
        {
            case ItemIdGold:
                AddGold(amount);
                return amount > 0;
            case ItemIdPetTicket:
                AddPetTickets(amount);
                return amount > 0;
            case ItemIdMagicBottle:
                AddMagicBottles(amount);
                return amount > 0;
            case ItemIdPetFood:
                AddPetFood(amount);
                return amount > 0;
            default:
                Debug.LogWarning("[PlayerResourceManager] Unknown item id: " + itemId);
                return false;
        }
    }

    public bool SpendItem(int itemId, int amount)
    {
        switch (itemId)
        {
            case ItemIdGold: return SpendGold(amount);
            case ItemIdPetTicket: return SpendPetTickets(amount);
            case ItemIdMagicBottle: return SpendMagicBottles(amount);
            case ItemIdPetFood: return SpendPetFood(amount);
            default:
                Debug.LogWarning("[PlayerResourceManager] Unknown item id: " + itemId);
                return false;
        }
    }

    public int GetItemAmount(int itemId)
    {
        switch (itemId)
        {
            case ItemIdGold: return Gold;
            case ItemIdPetTicket: return PetTickets;
            case ItemIdMagicBottle: return MagicBottles;
            case ItemIdPetFood: return PetFood;
            default: return 0;
        }
    }

    public void ResetToDefault()
    {
        Gold = 0;
        PetTickets = 0;
        MagicBottles = 0;
        PetFood = 0;
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.DeleteKey(KeyPetTickets);
        PlayerPrefs.DeleteKey(KeyMagicBottles);
        PlayerPrefs.DeleteKey(KeyPetFood);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }
}
