﻿using System.Collections;
using System.Runtime.InteropServices.Marshalling;

namespace VST3.Hosting;

[GeneratedComClass]
internal partial class EventList : VstObject<IEventList>, IEventList, IEnumerable<Event>
{
    private readonly List<Event> events = new List<Event>();

    public void Clear()
    {
        events.Clear();
    }

    public void AddEvent(in Event e)
    {
        events.Add(e);
    }

    void IEventList.addEvent(in Event e) => AddEvent(in e);

    Event IEventList.getEvent(int index)
    {
        return events[index];
    }

    int IEventList.getEventCount()
    {
        return events.Count;
    }

    public List<Event>.Enumerator GetEnumerator() => events.GetEnumerator();

    IEnumerator<Event> IEnumerable<Event>.GetEnumerator() => ((IEnumerable<Event>)events).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)events).GetEnumerator();
}
