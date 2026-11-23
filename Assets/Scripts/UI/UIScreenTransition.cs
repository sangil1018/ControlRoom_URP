using Common.Extension;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class UIFadeScreen : MonoBehaviour
{
    public enum FadeType
    {
        In = 0,
        Out,
        InOut,
    };

    private static GameObject m_canvas = null;

    private GameObject m_overlay = null;
    private CanvasGroup m_group = null;
    private Vector3 m_offset = new Vector3(0f, 0f, 0.1f);


    private void Awake()
    {        
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        m_group = gameObject.AddComponent<CanvasGroup>();
        m_group.alpha = 1f;

        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, Color.black);
        bgTex.Apply();

        m_overlay = new GameObject();
        var image = m_overlay.AddComponent<Image>();
        var rect = new Rect(0, 0, bgTex.width, bgTex.height);
        image.material.mainTexture = bgTex;
        image.sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
        image.color = Color.black;
        image.canvasRenderer.SetAlpha(1f);

        m_overlay.transform.localScale = new Vector3(1, 1, 1);
        m_overlay.GetComponent<RectTransform>().sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;
        m_overlay.transform.SetParent(transform, false);

        m_overlay.SetActiveSelf(false);
    }

    public static async UniTask FadeAsync(FadeType fadeType, float duration, CancellationToken cancellationToken, UnityAction callback)
    {
        if (m_canvas == null)
        {
            m_canvas = new GameObject("UICanvasFadeScreen");            
            m_canvas.transform.SetParent(null, false);
            m_canvas.transform.SetAsLastSibling();
            m_canvas.AddComponent<UIFadeScreen>();

            DontDestroyOnLoad(m_canvas);
        }

        m_canvas.GetComponent<UIFadeScreen>().StartFade(fadeType, duration, callback);
      
        //var cancellationToken = m_canvas.GetCancellationTokenOnDestroy();
        await UniTask.Delay(Mathf.FloorToInt((duration + 0.1f) * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);
    }

    private void StartFade(FadeType fadeType, float duration, UnityAction callback)
    {
        if (fadeType == FadeType.In)
        {
            StartCoroutine(RunFadeIn(duration, callback));
        }
        else if (fadeType == FadeType.Out)
        {
            StartCoroutine(RunFadeOut(duration, callback));
        }
        else
        {
            StartCoroutine(RunFadeInOut(duration, callback));
        }           
    }

    private IEnumerator RunFadeIn(float duration, UnityAction callback)
    {
        m_overlay.SetActive(true);
        m_group.alpha = 0f;

        var time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            m_group.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return new WaitForEndOfFrame();
        }

        m_group.alpha = 1f;
        yield return new WaitForEndOfFrame();

        //m_overlay.SetActive(false);

        if (callback != null)
            callback.Invoke();
    }

    private IEnumerator RunFadeOut(float duration, UnityAction callback)
    {
        m_overlay.SetActive(true);
        m_group.alpha = 1f;

        var time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            m_group.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return new WaitForEndOfFrame();
        }

        m_group.alpha = 0f;
        yield return new WaitForEndOfFrame();
        
        m_overlay.SetActive(false);

        if (callback != null)
            callback.Invoke();
    }

    private IEnumerator RunFadeInOut(float duration, UnityAction callback)
    {
        m_overlay.SetActive(true);
        m_group.alpha = 0f;

        var time = 0.0f;
        var halfDuration = duration / 2.0f;
        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;
            m_group.alpha = Mathf.Lerp(0f, 1f, time / halfDuration);
            yield return new WaitForEndOfFrame();
        }

        m_group.alpha = 1f;
        yield return new WaitForEndOfFrame();

        if (callback != null)
            callback.Invoke();

        time = 0.0f;
        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;
            m_group.alpha = Mathf.InverseLerp(0f, 1f, time / halfDuration);            
            yield return new WaitForEndOfFrame();
        }

        m_group.alpha = 0f; 
        yield return new WaitForEndOfFrame();

        m_overlay.SetActive(false);
    }

    private void Update()//FixedUpdate()
    {
        if (m_overlay != null && m_overlay.activeSelf && Camera.main != null)
        {
            var target = Camera.main.transform;
            m_overlay.transform.SetPositionAndRotation(
                target.position + target.TransformDirection(m_offset), target.rotation);
        }        
    }
}
