using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] public Material circleMaskMaterial;
    [SerializeField] public float closeTime = 1.0f;
    [SerializeField] public float openTime = 0.7f;

    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // sahneler arasý yaþar
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // LevelManager her sahnede playerTransform'u buraya register eder
    public void RegisterPlayer(Transform player)
    {
        playerTransform = player;
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(TransitionRoutine(buildIndex));
    }

    private IEnumerator TransitionRoutine(int buildIndex)
    {
        // 1. Kapat — circle player pozisyonuna küçülür
        yield return StartCoroutine(AnimateMask(1f, 0f, closeTime, EaseInQuad));

        // 2. Sahneyi arka planda yükle
        AsyncOperation load = SceneManager.LoadSceneAsync(buildIndex);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
            yield return null;

        load.allowSceneActivation = true;
        yield return null; // yeni sahnenin Start()'ý çalýþsýn

        // 3. Aç — circle tekrar büyür (yeni sahnede LevelManager RegisterPlayer çaðýrdý)
        yield return StartCoroutine(AnimateMask(0f, 1f, openTime, EaseOutQuad));
    }

    // Sahne ilk yüklendiðinde açýlýþ animasyonu (LevelManager çaðýrýr)
    public void PlayOpenTransition()
    {
        StartCoroutine(AnimateMask(0f, 1f, openTime, EaseOutQuad));
    }

    private IEnumerator AnimateMask(float from, float to, float duration,
                                     System.Func<float, float> ease)
    {
        if (circleMaskMaterial == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            circleMaskMaterial.SetFloat("_Radius", Mathf.Lerp(from, to, ease(t)));
            UpdateCenter();
            yield return null;
        }
        circleMaskMaterial.SetFloat("_Radius", to);
    }

    private void UpdateCenter()
    {
        if (playerTransform == null || Camera.main == null) return;
        Vector3 vp = Camera.main.WorldToViewportPoint(playerTransform.position);
        circleMaskMaterial.SetVector("_Center", new Vector4(vp.x, vp.y, 0f, 0f));
    }

    private static float EaseInQuad(float t) => t * t;
    private static float EaseOutQuad(float t) => t * (2f - t);
}