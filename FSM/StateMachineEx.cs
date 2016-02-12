using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

/// <summary>
/// State machine with a typed state
/// </summary>
public abstract class StateMachineEx<T> : StateMachineEx
{
	public T activeState
	{
		get
		{
			return (T)currentState;
		}
		set
		{
			currentState = value;
		}
	}
}

/// <summary>
/// Base class for state machines
/// </summary>
public abstract class StateMachineEx  {
	
	/// <summary>
	/// Sends the message that called the function to the current state
	/// </summary>
	/// <param name='param'>
	/// Any parameter passed to the current handler that should be passed on
	/// </param>
	protected void SendStateMessage(params object[] param)
	{
		var message = currentState.ToString() + "_" + (new StackFrame(1)).GetMethod().Name;
		SendMessageEx(message, param);
	}
	
	static Dictionary<Type, Dictionary<string, MethodInfo>> _messages = new Dictionary<Type, Dictionary<string, MethodInfo>>();
	Dictionary<string, Action> _actions = new Dictionary<string, Action>();
	
	
	void SendMessageEx(string message, object[] param)
	{
		Action a = null;
		var actionSpecified = false;
		if(_actions.TryGetValue(message, out a))
		{
			actionSpecified = true;
			if(a!=null)
			{
				a();
				return;
			}
		}
		
		
		MethodInfo d = null;
		Dictionary<string, MethodInfo> lookup = null;
		if(!_messages.TryGetValue(GetType(), out lookup))
		{
			lookup = new Dictionary<string, MethodInfo>();
			_messages[GetType()] = lookup;
		}
		
		if(!lookup.TryGetValue(message, out d))
		{
			d=GetType().GetMethod(message, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			lookup[message] = d;
		}
		
		if(d != null)
		{
			if(!actionSpecified)
			{
				if(d.GetParameters().Length == 0 && d.ReturnType == typeof(void))
				{
					var action = (Action)Delegate.CreateDelegate(typeof(Action),this,d);
					_actions[message] = action;
					action();
				}
				else
				{
					_actions[message]= null;
				}
			}
			else
				d.Invoke(this, param);

		}
		
	}
	
	/// <summary>
	/// The state machine of the controlled object
	/// </summary>
	[HideInInspector]
	public StateMachineEx stateMachine;
		
	/// <summary>
	/// The time that the current state was entered
	/// </summary>
	private float _timeEnteredState;
	
	/// <summary>
	/// Gets the amount of time spent in the current state
	/// </summary>
	/// <value>
	/// The number of seconds in the current state
	/// </value>
	public float timeInCurrentState
	{
		get
		{
			return Time.time - _timeEnteredState;
		}
	}
	
	
	public StateMachineEx()
	{
		//Configure the initial state of the state machine
		state.executingStateMachine = stateMachine = this;
	}
	
	#region Default Implementations Of Delegates
	
	static void DoNothing()
	{
	}
	
	#endregion
	
	
	/// <summary>
	/// Class that represents the settings for a particular state
	/// </summary>
	public class State
	{
	
		public object currentState;
		//The amount of time that was spend in this state
		//when pushed to the stack
		public float time;
		
		public StateMachineEx executingStateMachine;
	}
		
	/// <summary>
	/// The state of the current statemachine
	/// </summary>
	[HideInInspector]
	public State state = new State();
	
	/// <summary>
	/// Gets or sets the current state
	/// </summary>
	/// <value>
	/// The state to use
	/// </value>
	public object currentState
	{
		get
		{
			return state.currentState;
		}
		set
		{
			if(!stateMachine.Equals(this))
			{
				stateMachine.currentState = value;
			}
			else
			{
			
				if(state.Equals(value))
					return;
				
				ChangingState();
				state.currentState = value;
				state.executingStateMachine.state.currentState = value;
				ConfigureCurrentState();				
			}
		}
	}
	
	[HideInInspector]
	/// <summary>
	/// The last state.
	/// </summary>
	public object lastState;
	[HideInInspector]
	/// <summary>
	/// The last state machine behaviour.
	/// </summary>
	public StateMachineEx lastStateMachineBehaviour;
	
	/// <summary>
	/// Sets the state providing an injected statemachine behaviour
	/// </summary>
	/// <param name='stateToActivate'>
	/// State to activate.
	/// </param>
	/// <param name='useStateMachine'>
	/// The state machine behaviour to use for executing the state
	/// </param>
	public void SetState(object stateToActivate, StateMachineEx useStateMachine)
	{
		if(state.executingStateMachine == useStateMachine && stateToActivate.Equals(state.currentState))
			return;
		
	
		ChangingState();
		state.currentState = stateToActivate;
		state.executingStateMachine = useStateMachine;
		
		if(useStateMachine != this)
		{
			useStateMachine.stateMachine = this;
		} 
		state.executingStateMachine.state.currentState = stateToActivate;
		ConfigureCurrentState();
	}
	
	//Stack of the previous running states
	private Stack<State> _stack = new Stack<State>();
	
	/// <summary>
	/// Call the specified state - activates the new state without deactivating the 
	/// current state.  Called states need to execute Return() when they are finished
	/// </summary>
	/// <param name='stateToActivate'>
	/// State to activate.
	/// </param>
	public void Call(object stateToActivate)
	{
		Call(stateToActivate, null);
	}
	
	/// <summary>
	/// Call the specified state - activates the new state without deactivating the 
	/// current state.  Called states need to execute Return() when they are finished.
	/// This version enables the injection of a called state from another state machine
	/// </summary>
	/// <param name='stateToActivate'>
	/// State to activate.
	/// </param>
	/// <param name='useStateMachine'>
	/// The state machine to use
	/// </param>
	public void Call(object stateToActivate, StateMachineEx useStateMachine)
	{
		useStateMachine = useStateMachine ?? stateMachine;
		state.time = timeInCurrentState;
		ChangingState();
		
		_stack.Push(state);
		state = new State();
		state.currentState = stateToActivate;
		state.executingStateMachine = useStateMachine;
		
		if(useStateMachine != this)
		{
			useStateMachine.stateMachine = this;
		} 
		state.executingStateMachine.state.currentState = stateToActivate;
		ConfigureCurrentStateForCall();
	}
	
	//Configures the state machine when the new state has been called
	void ConfigureCurrentStateForCall()
	{
		GetStateMethods();
	}
	
	/// <summary>
	/// Return this state from a call
	/// </summary>
	public void Return()
	{
		if(stateMachine != this)
		{
			stateMachine.Return();
			return;
		}
		if(_stack.Count > 0)
		{
			state = _stack.Pop();
			_timeEnteredState = Time.time - state.time;
		}
	}
	
	/// <summary>
	/// Return the state from a call with a specified state to 
	/// enter if this state wasn't called
	/// </summary>
	/// <param name='baseState'>
	/// The state to use if there is no waiting calling state
	/// </param>
	public void Return(object baseState)
	{
		if(stateMachine != this)
		{
			stateMachine.Return(baseState);
			return;
		}
		if(_stack.Count > 0)
		{
			state = _stack.Pop();
		}
		else
		{
			currentState = baseState;
		}
		_timeEnteredState = Time.time - state.time;
	}

	
	/// <summary>
	/// Caches previous states
	/// </summary>
	void ChangingState()
	{
		lastState = state.currentState;
		lastStateMachineBehaviour = state.executingStateMachine;
		_timeEnteredState = Time.time;
	}
	
	/// <summary>
	/// Configures the state machine for the current state
	/// </summary>
	void ConfigureCurrentState()
	{

		GetStateMethods();
	}
	
	//Retrieves all of the methods for the current state
	void GetStateMethods()
	{
		UnwireEvents();
		WireEvents();
	}
	
	/// <summary>
	/// A cache of the delegates for a particular state and method
	/// </summary>
	Dictionary<object, Dictionary<string, Delegate>> _cache = new Dictionary<object, Dictionary<string, Delegate>>();
	
	//Creates a delegate for a particular method on the current state machine
	//if a suitable method is not found then the default is used instead
	T ConfigureDelegate<T>(string methodRoot, T Default) where T : class
	{
		
		Dictionary<string, Delegate> lookup;
		if(!_cache.TryGetValue(state.currentState, out lookup))
		{
			_cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
		}
		Delegate returnValue;
		if(!lookup.TryGetValue(methodRoot, out returnValue))
		{
		
			var mtd = GetType().GetMethod(state.currentState.ToString() + "_" + methodRoot, System.Reflection.BindingFlags.Instance 
				| System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);
			
			if(mtd != null)
			{
				if(typeof(T) == typeof(Func<IEnumerator>) && mtd.ReturnType != typeof(IEnumerator))
				{
					Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
					Func<IEnumerator> func = () => { a(); return null; };
					returnValue = func;
				}
				else
					returnValue = Delegate.CreateDelegate(typeof(T), this, mtd);
			}
			else
			{
				returnValue = Default as Delegate;
			}
			lookup[methodRoot] = returnValue;
		}
		return returnValue as T;
		
	}
	
	public class EventDef
	{
		public string eventName;
		public string selector;
		public MethodInfo method;
	}
	
	private class WiredEvent
	{
		public System.Reflection.EventInfo evt;
		public Delegate dlg;
		public object source;
	}
	
	List<WiredEvent> _wiredEvents = new List<WiredEvent>();
	
	private static Dictionary<Type,Dictionary<string, EventDef[]>> _cachedEvents= new Dictionary<Type, Dictionary<string, EventDef[]>>();
	
	void WireEvents()
	{
		var cs = currentState.ToString();
		var type = GetType();
		EventDef[] events;
		Dictionary<string, EventDef[]> lookup;
		
		if(!_cachedEvents.TryGetValue(type, out lookup))
		{
			lookup = new Dictionary<string, EventDef[]>();
			_cachedEvents[type] = lookup;
		}
		if(!lookup.TryGetValue(cs, out events))
		{
			var len = currentState.ToString().Length + 3;
			events = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
				.Where(m=>m.Name.StartsWith(currentState.ToString() + "_On"))
				.Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
						.Where(m=>m.Name.StartsWith("All_On")))
				.Select(m=> new { name = m.Name.Substring(len), method = m})
				.Where(n=>n.name.IndexOf("_") >1)
				.Select(n=>new EventDef {
						eventName = n.name.Substring(0, n.name.IndexOf("_")), 
						selector = n.name.Substring(n.name.IndexOf("_")+1),
						method = n.method
					})
				.ToArray();
			lookup[cs] = events;
		}
		
		foreach(var evt in events)
		{
			
			var list = OnWire(evt);
			list.AddRange(list.ToList().Where(l=>l is Component).SelectMany(l=>((Component)l).GetComponents<MonoBehaviour>()).Cast<object>());
			list.AddRange(list.ToList().Where(l=>l is GameObject).SelectMany(l=>((GameObject)l).GetComponents<MonoBehaviour>()).Cast<object>());
			var sources = 
				list.Select(l=> new { @event = l.GetType().GetEvent(evt.eventName), source = l })
					.Where(e=>e.@event!=null);
			foreach(var source in sources)	
			{
				var dlg = Delegate.CreateDelegate(source.@event.EventHandlerType, this, evt.method);
				if(dlg != null)
				{
					source.@event.AddEventHandler(source.source, dlg);
					_wiredEvents.Add( new WiredEvent { dlg = dlg, evt = source.@event, source = source.source} );
				}
			}
					
			
		}
		
		
		
	}
	
	public void RefreshEvents()
	{
		UnwireEvents();
		WireEvents();
	}
	
	void UnwireEvents()
	{
		foreach(var evt in _wiredEvents)
		{
			evt.evt.RemoveEventHandler(evt.source, evt.dlg);
		}
	}
	
	List<object> OnWire(EventDef eventInfo)
	{
		List<object> objects = new List<object>();
		var extra = OnWireEvent(eventInfo);
		if(extra!=null)
		{
			objects.AddRange(extra);
			if(objects.Count > 0)
				return objects;
		}
		if(eventInfo.selector.StartsWith("Tag_"))
		{
			objects.AddRange(GameObject.FindGameObjectsWithTag(eventInfo.selector.Substring(4)));
			return objects;
		}
			
		objects.Add(GameObject.Find(eventInfo.selector));
		return objects;
	}
	
	protected virtual IEnumerable<object> OnWireEvent(EventDef eventInfo)
	{
		return null;
	}
	
}

