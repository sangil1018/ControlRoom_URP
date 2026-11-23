using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Common.Extension;


public class UIPopup : MonoBehaviour
{
    [SerializeField] protected bool mIsDestroy = true;
    [SerializeField] protected bool mIsBackground = true;

    public Color mBackgroundColor = new Color(10.0f / 255.0f, 10.0f / 255.0f, 10.0f / 255.0f, 0.6f);

    public float mDestroyTime = 0.5f;

    protected bool mIsOpened = false;
    private GameObject mBackground = null;

    public virtual void Open()
    {
        if (mIsOpened)
        {
            return;
        }

        mIsOpened = true;
        gameObject.SetActiveSelf(true);

        if (mIsBackground)
            AddBackground();
    }

    public virtual void Close(bool isImmediate = false)
    {
        if (!mIsOpened)
        {
            return;
        }

        mIsOpened = false;

        if (isImmediate)
        {
            if (mBackground)
            {
                Destroy(mBackground);
                mBackground = null;
            }

            if (mIsDestroy)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActiveSelf(false);
            }
        }
        else
        {
            var animator = GetComponent<Animator>();
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Open"))
                animator.Play("Close");

            RemoveBackground();
            StartCoroutine(RunPopupDestroy());
        }
    }

    private IEnumerator RunPopupDestroy()
    {
        yield return new WaitForSeconds(mDestroyTime);

        if (mBackground)
        {
            Destroy(mBackground);
            mBackground = null;
        }

        if (mIsDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActiveSelf(false);
        }
    }

    private void AddBackground()
    {
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, mBackgroundColor);
        bgTex.Apply();

        mBackground = new GameObject("PopupBackground");
        var image = mBackground.AddComponent<Image>();
        var rect = new Rect(0, 0, bgTex.width, bgTex.height);
        var sprite = Sprite.Create(bgTex, rect, new Vector2(0.5f, 0.5f), 1);
        image.material.mainTexture = bgTex;
        image.sprite = sprite;
        var newColor = image.color;
        image.color = newColor;
        image.canvasRenderer.SetAlpha(0.0f);
        image.CrossFadeAlpha(1.0f, 0.4f, false);

        var canvas = gameObject;// GameObject.Find("Canvas");
        mBackground.transform.localScale = new Vector3(10, 10, 10);
        mBackground.GetComponent<RectTransform>().sizeDelta = canvas.GetComponent<RectTransform>().sizeDelta;
        mBackground.transform.SetParent(canvas.transform, false);
        mBackground.transform.SetAsFirstSibling();
        //mBackground.transform.SetSiblingIndex(transform.GetSiblingIndex());
    }

    private void RemoveBackground()
    {
        if (mBackground != null)
        {
            var image = mBackground.GetComponent<Image>();
            if (image != null)
                image.CrossFadeAlpha(0.0f, 0.2f, false);
        }        
    }
}
