using UnityEngine;

public class PlayerResourceManager : MonoBehaviour
{
    public static PlayerResourceManager Instance { get; private set; }

    const string KeyPetTickets = "PetTickets";

    public int PetTickets { get; private set; }

    public event System.Action OnResourceChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        PetTickets = Mathf.Max(0, PlayerPrefs.GetInt(KeyPetTickets, 0));
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
        PetTickets = 0;
        PlayerPrefs.DeleteKey(KeyPetTickets);
        PlayerPrefs.Save();
        OnResourceChanged?.Invoke();
    }
}
