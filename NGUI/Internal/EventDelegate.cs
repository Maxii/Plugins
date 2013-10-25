//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

#if UNITY_EDITOR || (!UNITY_FLASH && !UNITY_WP8 && !UNITY_METRO)
#define REFLECTION_SUPPORT
#endif

#if REFLECTION_SUPPORT
using System.Reflection;
#endif

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Delegate callback that Unity can serialize and set via Inspector.
/// </summary>

[System.Serializable]
public class EventDelegate
{
	[SerializeField] MonoBehaviour mTarget;
	[SerializeField] string mMethodName;

	/// <summary>
	/// Whether the event delegate will be removed after execution.
	/// </summary>

	public bool oneShot = false;

	public delegate void Callback();
	Callback mCachedCallback;

	/// <summary>
	/// Event delegate's target object.
	/// </summary>

	public MonoBehaviour target { get { return mTarget; } set { mTarget = value; mCachedCallback = null; } }

	/// <summary>
	/// Event delegate's method name.
	/// </summary>

	public string methodName { get { return mMethodName; } set { mMethodName = value; mCachedCallback = null; } }

	/// <summary>
	/// Whether this delegate's values have been set.
	/// </summary>

	public bool isValid { get { return mTarget != null && !string.IsNullOrEmpty(mMethodName); } }

	/// <summary>
	/// Whether the target script is actually enabled.
	/// </summary>

	public bool isEnabled { get { return mTarget != null && mTarget.enabled; } }

	public EventDelegate () { }
	public EventDelegate (Callback call) { Set(call); }
	public EventDelegate (MonoBehaviour target, string methodName) { Set(target, methodName); }

	/// <summary>
	/// Equality operator.
	/// </summary>

	public override bool Equals (object obj)
	{
		if (obj == null)
		{
			return !isValid;
		}

		if (obj is Callback)
		{
			Callback callback = obj as Callback;
			return (mTarget == callback.Target && string.Equals(mMethodName, callback.Method.Name));
		}
		
		if (obj is EventDelegate)
		{
			EventDelegate del = obj as EventDelegate;
			return (mTarget == del.mTarget && string.Equals(mMethodName, del.mMethodName));
		}
		return false;
	}

	static int s_Hash = "EventDelegate".GetHashCode();

	/// <summary>
	/// Used in equality operators.
	/// </summary>

	public override int GetHashCode () { return s_Hash; }

#if REFLECTION_SUPPORT
	/// <summary>
	/// Convert the saved target and method name into an actual delegate.
	/// </summary>

	Callback Get ()
	{
		if (mCachedCallback == null || mCachedCallback.Target != mTarget || mCachedCallback.Method.Name != mMethodName)
		{
			if (mTarget != null && !string.IsNullOrEmpty(mMethodName))
			{
				mCachedCallback = (Callback)System.Delegate.CreateDelegate(typeof(Callback), mTarget, mMethodName);
			}
			else return null;
		}
		return mCachedCallback;
	}
#endif

	/// <summary>
	/// Set the delegate callback directly.
	/// </summary>

	void Set (Callback call)
	{
		if (call == null || call.Method == null)
		{
			mTarget = null;
			mMethodName = null;
			mCachedCallback = null;
		}
		else
		{
			mTarget = call.Target as MonoBehaviour;
			mMethodName = call.Method.Name;
		}
	}

	/// <summary>
	/// Set the delegate callback using the target and method names.
	/// </summary>

	public void Set (MonoBehaviour target, string methodName)
	{
		this.mTarget = target;
		this.mMethodName = methodName;
		mCachedCallback = null;
	}

	/// <summary>
	/// Execute the delegate, if possible.
	/// This will only be used when the application is playing in order to prevent unintentional state changes.
	/// </summary>

	public bool Execute ()
	{
#if UNITY_EDITOR
		if (Application.isPlaying)
#endif
		{
#if REFLECTION_SUPPORT
			Callback call = Get();
			
			if (call != null)
			{
				call();
				return true;
			}
#else
			if (isValid)
			{
				mTarget.SendMessage(mMethodName, SendMessageOptions.DontRequireReceiver);
				return true;
			}

#endif
		}
		return false;
	}

	/// <summary>
	/// Convert the delegate to its string representation.
	/// </summary>

	public override string ToString ()
	{
		if (mTarget != null && !string.IsNullOrEmpty(methodName))
		{
			string typeName = mTarget.GetType().ToString();
			int period = typeName.LastIndexOf('.');
			if (period > 0) typeName = typeName.Substring(period + 1);
			return typeName + "." + methodName;
		}
		return null;
	}

	/// <summary>
	/// Execute an entire list of delegates.
	/// </summary>

	static public void Execute (List<EventDelegate> list)
	{
		if (list != null)
		{
			for (int i = 0; i < list.Count; )
			{
				EventDelegate del = list[i];

				if (del != null)
				{
					del.Execute();

					if (del.oneShot)
					{
						list.RemoveAt(i);
						continue;
					}
				}
				++i;
			}
		}
	}

	/// <summary>
	/// Convenience function to check if the specified list of delegates can be executed.
	/// </summary>

	static public bool IsValid (List<EventDelegate> list)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.isValid)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Assign a new event delegate.
	/// </summary>

	static public void Set (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			list.Clear();
			list.Add(new EventDelegate(callback));
		}
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, Callback callback) { Add(list, callback, false); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, Callback callback, bool oneShot)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.Equals(callback))
					return;
			}

			EventDelegate ed = new EventDelegate(callback);
			ed.oneShot = oneShot;
			list.Add(ed);
		}
		else
		{
			Debug.LogWarning("Attempting to add a callback to a list that's null");
		}
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev) { Add(list, ev, false); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev, bool oneShot)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.Equals(ev))
					return;
			}
			
			EventDelegate ed = new EventDelegate(ev.target, ev.methodName);
			ed.oneShot = oneShot;
			list.Add(ed);
		}
		else
		{
			Debug.LogWarning("Attempting to add a callback to a list that's null");
		}
	}

	/// <summary>
	/// Remove an existing event delegate from the list.
	/// </summary>

	static public bool Remove (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				
				if (del != null && del.Equals(callback))
				{
					list.RemoveAt(i);
					return true;
				}
			}
		}
		return false;
	}
}
