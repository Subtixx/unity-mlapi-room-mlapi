using System.Diagnostics;
using System.Text;

namespace DefaultNamespace
{
    public abstract class CustomTraceListener : TraceListener
    {
        protected CustomTraceListener(string name) : base(name)
        {
        }

        protected abstract void TraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType,
            int id, string message);

        protected virtual string FormatData(object[] data)
        {
            StringBuilder strData = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                if (i >= 1) strData.Append("|");
                strData.Append(data[i].ToString());
            }

            return strData.ToString();
        }

        protected void TraceDataCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            params object[] data)
        {
            if (Filter != null &&
                !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data)) return;
            TraceEventCore(eventCache, source, eventType, id, FormatData(data));
        }

        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType,
            int id, string message)
        {
            if (Filter != null &&
                !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) return;
            TraceEventCore(eventCache, source, eventType, id, message);
        }

        public sealed override void Write(string message)
        {
            if (Filter != null && !Filter.ShouldTrace(null, "Trace", TraceEventType.Information, 0, message, null,
                    null, null)) return;
            TraceEventCore(null, "Trace", TraceEventType.Information, 0, message);
        }
        
        

        public sealed override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public sealed override string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }
    }
}