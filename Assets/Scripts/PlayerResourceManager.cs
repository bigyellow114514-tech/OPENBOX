using UnityEngine;

public class PlayerResourceManager : MonoBehaviour
{
    public static PlayerResourceManager Instance { get; private set; }

    const string KeyGold = "Gold";
    const string KeyPetTickets = "PetTickets";

    public int Gold { get; private set; }
    public int PetTickets { get; private set; }

    public event System.Action OnResourceChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Gold = Mathf.Max(0, PlayerPrefs.GetInt(KeyGold, 0));
        PetTickets = Mathf.Max(0, PlayerPrefs.GetInt(KeyPetTickets, 0));
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

    public void ResetToDefault()
    {
        Gold = 0;
        PetTickets = 0;
        PlayerPrefs.DeleteKey(KeyGold);
        PlayerPrefs.DeleteKey(KeyPetTickets);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }
}
