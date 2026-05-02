using System.Collections;
using UnityEngine;

public class BoxDropHelper : MonoBehaviour
{
    int _rarityIndex;

    public void StartDrop(float groundY, int rarityIndex)
    {
        _rarityIndex = rarityIndex;
        StartCoroutine(DropAnimation(groundY));
    }

    IEnumerator DropAnimation(float groundY)
    {
        var sr = GetComponent<SpriteRenderer>();
        Vector3 startPos = transform.position;
        float fallDistance = Mathf.Max(startPos.y - groundY, 0.1f);

        float sign = Random.value > 0.5f ? 1f : -1f;
        float totalDrift = fallDistance * Random.Range(0.15f, 0.30f) * sign;
        float rotDir = sign;
        float fallDuration = Mathf.Clamp(Mathf.Sqrt(fallDistance * 0.22f), 0.3f, 0.8f);

        // Phase 1: Fall — quadratic drop, linear horizontal drift, spin
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / fallDuration, 1f);
            transform.position = new Vector3(
                startPos.x + totalDrift * t,
                startPos.y - fallDistance * (t * t),
                transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, rotDir * 300f * t);
            yield return null;
        }

        Vector3 landPos = new Vector3(startPos.x + totalDrift, groundY, transform.position.z);
        transform.position = landPos;
        float landRot = rotDir * 300f;

        // Phase 2: Roll — small bounce + decelerated sideways slide
        float rollDuration = 0.4f;
        float rollDrift = totalDrift * 0.3f;
        float bounceH = fallDistance * 0.05f;
        elapsed = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / rollDuration, 1f);
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = new Vector3(
                landPos.x + rollDrift * eased,
                groundY + bounceH * Mathf.Sin(t * Mathf.PI),
                transform.position.z);
            transform.rotation = Quaternion.Euler(0f, 0f, landRot + rotDir * 35f * eased);
            yield return null;
        }

        // Phase 3: Fade out
        float fadeDuration = 0.7f;
        Color c = sr.color;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Min(elapsed / fadeDuration, 1f);
            sr.color = c;
            yield return null;
        }

        int playerLevel = PlayerExpManager.Instance != null ? PlayerExpManager.Instance.Level : 1;
        Debug.Log($"[BoxDrop] 动画结束，准备弹 UI。rarityIndex={_rarityIndex} playerLevel={playerLevel}");

        if (EquipmentDropSystem.Instance == null)
        {
            Debug.LogError("[BoxDrop] EquipmentDropSystem 不在场景中，请执行 OpenBox → 装备系统 → 配置掉落系统");
            TreeClick.Unlock();
            Destroy(gameObject);
            yield break;
        }

        EquipmentResult result = EquipmentDropSystem.Instance.GenerateDrop(_rarityIndex, playerLevel);
        Debug.Log($"[BoxDrop] 生成装备：{result.itemName} 槽位={result.slotName} 品质={result.rarity}");

        if (EquipmentCardUI.Instance == null)
        {
            Debug.LogError("[BoxDrop] EquipmentCardUI 不在场景中，请执行 OpenBox → UI 构建 → 构建装备卡片");
            TreeClick.Unlock();
            Destroy(gameObject);
            yield break;
        }

        // 若同槽位已有装备，弹出对比窗口；否则直接显示装备卡片
        var existingItem = EquipmentSlotSystem.Instance?.GetSlot(result.slotIndex);
        if (existingItem != null)
        {
            var compareUI = EquipmentCompareUI.GetOrFindInstance();
            if (compareUI != null)
            {
                compareUI.Show(result, existingItem);
                Destroy(gameObject);
                yield break;
            }
            Debug.LogWarning("[BoxDrop] EquipmentCompareUI 不在场景中，请执行 OpenBox → UI 构建 → 构建装备对比窗口；已回退至普通卡片");
        }

        EquipmentCardUI.Instance.Show(result);
        Destroy(gameObject);
    }
}
