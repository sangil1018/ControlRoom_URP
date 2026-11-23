#define DEBUG_LOG

using System;

using UnityEngine;
using UnityEngine.Assertions;

public static partial class DLogger 
{	
	[System.Diagnostics.Conditional("DEBUG_LOG")]
	public static void Log(string format, params object[] args)
	{
        format = $"[DLogger] {format}";
        if (args != null && args.Length != 0)
            Debug.LogFormat(format, args);
		else
			Debug.Log(format);
	}

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    public static void TLog(string format, params object[] args)
    { 
        string append = $"[DLogger] <color=green>[{DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss")}]</color> {format}";

        if (args != null && args.Length != 0)
            Debug.LogFormat(append, args);
        else
            Debug.Log(append);
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
	public static void Warning(string format, params object[] args)
	{
        format = $"[DLogger] {format}";
        if (args != null && args.Length != 0)
            Debug.LogWarningFormat(format, args);
		else 
			Debug.LogWarning(format);
	}

	[System.Diagnostics.Conditional("DEBUG_LOG")]
	public static void Error(string format, params object[] args)
	{
        string append = $"[DLogger] <color=red>{format}</color>";

        if (args != null && args.Length != 0)
            Debug.LogErrorFormat(append, args);
        else
            Debug.LogError(append);
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    public static void TError(string format, params object[] args)
    {
        string append = $"[DLogger] <color=red>[{DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss")}]</color> {format}";

        if (args != null && args.Length != 0)
            Debug.LogErrorFormat(append, args);
        else
            Debug.LogError(append);
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    public static void LogAssertion(string format, params object[] args)
    {
        string append = $"[DLogger] <color=red>{format}</color>";

        if (args != null && args.Length != 0)
        {
            Debug.LogAssertionFormat(append, args);
        }
        else
        {
            Debug.LogAssertionFormat(append);
        }
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    public static void Exception(System.Exception exception)
    {
        Debug.LogException(exception);
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    public static void Exception(System.Exception exception, UnityEngine.Object context)
    {
        Debug.LogException(exception, context);
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
	public static void  Assert(bool condition, string format, params object[] args)
	{
        if (args != null && args.Length != 0)
            Debug.AssertFormat(condition, format, args);

        else
            Debug.Assert(condition, format);
	}
}

