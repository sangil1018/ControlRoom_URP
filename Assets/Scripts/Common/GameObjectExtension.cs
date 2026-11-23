using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Common.Extension
{
    public static class GameObjectExtension
    {
        public static void SetActiveSelf(this GameObject gameObject, bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }

        public static void SetActiveSelf(this Transform transform, bool active)
        {
            if (transform != null && transform.gameObject.activeSelf != active)
            {
                transform.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }
    }

    public static class GameComponentExtension
    {
        public static void SetEnableSelf(this MonoBehaviour gameBehaviour, bool enable)
        {
            if (gameBehaviour != null && gameBehaviour.enabled != enable)
            {
                gameBehaviour.enabled = enable;
            }
        }
    }
}