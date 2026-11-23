using UnityEngine;
using System;


namespace Common
{
	public abstract class Singleton2<T> where T : class, new()
	{
	    private static T _instance = null;
	    public static T Instance
	    {
	        get {
	            if (_instance == null)
	            {
	                _instance = new T();
	            }

	            return _instance;
	        }
	    }

		public static bool IsInstance()
		{
			return (_instance == null) ? false : true;
		}
	}

	public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
		private static WeakReference _container = null;
		private static WeakReference _instance = null;

        protected virtual bool IsDestroy
        {
            get { return false; }
        }

	    public static T Instance
	    {
	        get {
	            GameObject container;
	            
	            if( _container != null ) {
	                container = _container.Target as GameObject;
	                if( container != null ) {
	                    return _instance.Target as T;
	                }
	            }

                T instance = FindObjectOfType(typeof(T)) as T;

                if(instance == null)
                {
                    container = new GameObject();
                    container.name = "_" + typeof(T).Name;
                    instance = container.AddComponent(typeof(T)) as T;
                }

                container = instance.gameObject;

                _container = new WeakReference(container, false );
	            _instance = new WeakReference(instance, false );

                if(instance.IsDestroy == false)
                {
                    DontDestroyOnLoad(container.transform.root.gameObject);
                }

                return instance;
	        }
	    }

        public static bool IsInstance()
        {
            return (_instance == null) ? false : true;
        }
	}
}