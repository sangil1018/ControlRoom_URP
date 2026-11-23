using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Common.Extension
{
    public static class UIPopupExtension
    {
        public static void SetActive(this UIPopup popup, bool active)
        {
            if (popup != null && popup.gameObject.activeSelf != active)
            {
                popup.gameObject.SetActive(active);
            }
        }

        public static void ShowPopup(this UIPopup popup, bool show)
        {
            if (popup != null)
            {
                if (show)
                {
                    popup.Open();
                }
                else
                {
                    popup.Close(true);// false);
                }
            }
        }
    }

    public static class UIExtension
    {
        public static void SetString(this TextMeshProUGUI mesh, string str)
        {
            if (mesh != null)
            {
                mesh.text = str;
            }
        }

        public static void SetString(this TextMeshProUGUI mesh, int strIndex)
        {
            if (mesh != null)
            {
                mesh.text = TableManager.GetString(strIndex);
            }
        }

        public static void SetFillAmount(this Image image, float value)
        {
            if (image != null)
            {
                image.fillAmount = value;
            }
        }
     

        public static IEnumerator FadeIn(this CanvasGroup group, float duration, float delay = 0f, UnityAction callback = null)
        {
            if (group != null)
            {                
                group.alpha = 0f;

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);

                var time = 0f;
                while (time < duration)
                {
                    time += Time.deltaTime;
                    group.alpha = Mathf.Lerp(0f, 1f, time / duration);
                    yield return new WaitForEndOfFrame();
                }

                group.alpha = 1f;
            }

            callback?.Invoke();
        }

        public static IEnumerator FadeOut(this CanvasGroup group, float duration, float delay = 0f, UnityAction callback = null)
        {
            if (group != null)
            {
                group.alpha = 1f;

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);

                var time = 0f;
                while (time < duration)
                {
                    time += Time.deltaTime;
                    group.alpha = Mathf.Lerp(1f, 0f, time / duration);
                    yield return new WaitForEndOfFrame();
                }

                group.alpha = 0f;
            }

            callback?.Invoke();
        }
    }
}
