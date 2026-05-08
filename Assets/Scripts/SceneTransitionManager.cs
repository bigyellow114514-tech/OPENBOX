using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    static SceneTransitionManager _instance;
    Image _overlay;
    bool _busy;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SceneTransitionManager");
                _instance = go.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    void BuildOverlay()
    {
        var canvasGO = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var overlayGO = new GameObject("BlackOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        var rt = overlayGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0f);
        _overlay.raycastTarget = false;
    }

    public void LoadScene(string sceneName)
    {
        if (_busy) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        _busy = true;
        _overlay.raycastTarget = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            _overlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
            yield return null;
        }
        _overlay.color = Color.black;

        SceneManager.LoadScene(sceneName);
        yield return null;

        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 3f;
            _overlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
            yield return null;
        }
        _overlay.color = new Color(0f, 0f, 0f, 0f);
        _overlay.raycastTarget = false;
        _busy = false;
    }
}
