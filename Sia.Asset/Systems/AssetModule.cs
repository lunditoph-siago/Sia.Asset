namespace Sia.Asset;

using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

public abstract class AssetModule<TAsset, TAssetRecord, TSnapshot>(
    SystemChain? children = null)
    : SystemBase(Matchers.Any, children: children)
    where TAsset : IAsset<TAsset, TAssetRecord>
    where TAssetRecord : class, IAssetRecord
{
    private interface IEventCache
    {
        public void Handle(
            AssetModule<TAsset, TAssetRecord, TSnapshot> module,
            int index, in Identity id);
        public void Clear();
    }

    private class EventCache<TEvent> : IEventCache
        where TEvent : IEvent
    {
        public readonly List<(TSnapshot, TEvent)> List = [];

        public void Handle(
            AssetModule<TAsset, TAssetRecord, TSnapshot> module, int index, in Identity id)
        {
            ref var entry = ref List.AsSpan()[index];
            module.HandleEvent(id, entry.Item1, entry.Item2);
        }

        public void Clear() => List.Clear();
    }

    private readonly struct CommandRecorder(AssetModule<TAsset, TAssetRecord, TSnapshot> module)
        : IGenericTypeHandler<ICommand<TAsset>>
    {
        public void Handle<T>()
            where T : ICommand<TAsset>
            => module.RecordEvent<T>();
    }

    static AssetModule()
    {
        AssetLibrary.RegisterAsset<TAsset, TAssetRecord>();
    }

    protected event Action? OnUninitialize;

    public World World { get; private set; } = null!;
    public Scheduler Scheduler { get; private set; } = null!;

    private Dictionary<Type, IEventCache> _eventCaches = [];
    private Dictionary<Type, IEventCache> _eventCachesBack = [];
    private List<(Identity, IEventCache, int)> _events = [];
    private List<(Identity, IEventCache, int)> _eventsBack = [];

    private readonly Dictionary<Identity, TSnapshot> _snapshots = [];

    public override void Initialize(World world, Scheduler scheduler)
    {
        World = world;
        World.IndexHosts(Matchers.Of<TAsset>());
        Scheduler = scheduler;

        _eventCaches[typeof(WorldEvents.Add<TAsset>)] = new EventCache<WorldEvents.Add<TAsset>>();
        _eventCaches[typeof(WorldEvents.Remove<TAsset>)] = new EventCache<WorldEvents.Remove<TAsset>>();
        _eventCachesBack[typeof(WorldEvents.Add<TAsset>)] = new EventCache<WorldEvents.Add<TAsset>>();
        _eventCachesBack[typeof(WorldEvents.Remove<TAsset>)] = new EventCache<WorldEvents.Remove<TAsset>>();

        RecordAddEvent();
        RecordRemoveEvent();
        TAsset.HandleCommandTypes(new CommandRecorder(this));
    }

    public override void Uninitialize(World world, Scheduler scheduler)
        => OnUninitialize?.Invoke();
    
    protected abstract TSnapshot Snapshot<TEvent>(
        in EntityRef entity, in TAsset asset, in TEvent e)
        where TEvent : IEvent;

    protected abstract void HandleEvent<TEvent>(
        in Identity id, in TSnapshot snapshot, in TEvent e)
        where TEvent : IEvent;
    
    private void RecordAddEvent()
    {
        bool EventListener(in EntityRef entity, in WorldEvents.Add<TAsset> e)
        {
            var id = entity.Id;
            ref var asset = ref entity.Get<TAsset>();

            var eventCache = Unsafe.As<EventCache<WorldEvents.Add<TAsset>>>(
                _eventCachesBack[typeof(WorldEvents.Add<TAsset>)]);

            var snapshot = Snapshot(entity, asset, e);
            _snapshots.Add(id, snapshot);

            var eventIndex = eventCache.List.Count;
            eventCache.List.Add((snapshot, e));
            _eventsBack.Add((id, eventCache, eventIndex));
            return false;
        }

        World.Dispatcher.Listen<WorldEvents.Add<TAsset>>(EventListener);
        OnUninitialize += () => World.Dispatcher.Unlisten<WorldEvents.Add<TAsset>>(EventListener);
    }

    private void RecordRemoveEvent()
    {
        bool EventListener(in EntityRef entity, in WorldEvents.Remove<TAsset> e)
        {
            var id = entity.Id;
            ref var asset = ref entity.Get<TAsset>();

            var eventCache = Unsafe.As<EventCache<WorldEvents.Remove<TAsset>>>(
                _eventCachesBack[typeof(WorldEvents.Remove<TAsset>)]);

            if (!_snapshots.Remove(id, out var snapshot)) {
                return false;
            }

            var eventIndex = eventCache.List.Count;
            eventCache.List.Add((snapshot, e));
            _eventsBack.Add((id, eventCache, eventIndex));
            return false;
        }

        World.Dispatcher.Listen<WorldEvents.Remove<TAsset>>(EventListener);
        OnUninitialize += () => World.Dispatcher.Unlisten<WorldEvents.Remove<TAsset>>(EventListener);
    }

    protected void RecordEvent<TEvent>()
        where TEvent : IEvent
    {
        bool EventListener(in EntityRef entity, in TEvent e)
        {
            var id = entity.Id;
            ref var asset = ref entity.Get<TAsset>();
            EventCache<TEvent> eventCache;

            var eventType = typeof(TEvent);
            if (!_eventCachesBack.TryGetValue(eventType, out var rawCache)) {
                eventCache = new();
                _eventCachesBack.Add(eventType, eventCache);
            }
            else {
                eventCache = Unsafe.As<EventCache<TEvent>>(rawCache);
            }

            if (_snapshots.Remove(id, out var snapshot)) {
                _snapshots.Add(id, Snapshot(entity, asset, e));
            }
            else {
                snapshot = Snapshot(entity, asset, e);
                _snapshots.Add(id, snapshot);
            }

            var eventIndex = eventCache.List.Count;
            eventCache.List.Add((snapshot, e));
            _eventsBack.Add((id, eventCache, eventIndex));
            return false;
        }

        World.Dispatcher.Listen<TEvent>(EventListener);
        OnUninitialize += () => World.Dispatcher.Unlisten<TEvent>(EventListener);
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        (_eventCaches, _eventCachesBack) = (_eventCachesBack, _eventCaches);
        (_events, _eventsBack) = (_eventsBack, _events);

        foreach (var (id, cache, index) in _events) {
            cache.Handle(this, index, id);
        }

        _events.Clear();

        foreach (var cache in _eventCaches.Values) {
            cache.Clear();
        }
    }
}

public abstract class AssetModule<TAsset, TAssetRecord>(
    SystemChain? children = null)
    : AssetModule<TAsset, TAssetRecord, TAsset>(children)
    where TAsset : IAsset<TAsset, TAssetRecord>
    where TAssetRecord : class, IAssetRecord
{
    protected override TAsset Snapshot<TEvent>(
        in EntityRef entity, in TAsset asset, in TEvent e)
        => asset;
}