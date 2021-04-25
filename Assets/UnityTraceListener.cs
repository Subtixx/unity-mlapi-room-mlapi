using System.Diagnostics;
using DefaultNamespace;
using Debug = UnityEngine.Debug;

public class UnityTraceListener : CustomTraceListener
{
    public UnityTraceListener() : base("UnityTraceListener")
    {
    }

    public override void WriteLine(string message)
    {
        Debug.Log(message);
    }

    protected override void TraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
    {
        switch (eventType)
        {
            case TraceEventType.Error:
                Debug.LogError(message);
                break;
            case TraceEventType.Warning:
                Debug.LogWarning(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
    }
}