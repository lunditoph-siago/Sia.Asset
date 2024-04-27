namespace Sia.Asset;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Sia.Reactors;

public class AssetLibrary : ReactorBase<TypeUnion<AssetMetadata>>
{
    private record struct AssetEntry(
        Func<World, IAssetRecord, AssetLife, EntityRef> EntityCreator);

    public IReadOnlyDictionary<ObjectKey<IAssetRecord>, EntityRef> Entities => _entities;

    public EntityRef this[IAssetRecord record]
        => _entities.TryGetValue(new(record), out var entity)
            ? entity : throw new KeyNotFoundException("Asset entity not found");

    private readonly Dictionary<ObjectKey<IAssetRecord>, EntityRef> _entities = [];

    private static readonly ConcurrentDictionary<Type, AssetEntry> s_assetEntries = [];

    public static void RegisterAsset<TAsset, TAssetRecord>()
        where TAsset : IAsset<TAsset, TAssetRecord>
        where TAssetRecord : class, IAssetRecord
    {
        static EntityRef EntityCreator(World world, IAssetRecord record, AssetLife life)
            => world.CreateInArrayHost(HList.Create(
                TAsset.Create(Unsafe.As<TAssetRecord>(record), life)));

        s_assetEntries.AddOrUpdate(typeof(TAssetRecord),
            t => new(EntityCreator), (t, e) => new(EntityCreator));
    }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        world.IndexHosts(Matcher);

        Listen((in EntityRef e, in WorldEvents.Remove cmd) => {
            ref var meta = ref e.Get<AssetMetadata>();
            DestroyAssetRecursively(e, ref meta);
        });
    }

    private void DestroyAssetRecursively(in EntityRef entity, ref AssetMetadata meta)
    {
        foreach (var referredId in meta.Dependents) {
            var referred = World[referredId];
            entity.Unrefer(referred);

            ref var refereeMeta = ref referred.Get<AssetMetadata>();
            if (refereeMeta.AssetLife == AssetLife.Automatic
                    && refereeMeta.Referrers.Count == 0) {
                DestroyAssetRecursively(referred, ref refereeMeta);
                referred.Dispose();
            }
        }
    }

    protected override void OnEntityAdded(in EntityRef entity)
    {
        ref var metadata = ref entity.Get<AssetMetadata>();
        var assetRecord = metadata.AssetSource;
        if (assetRecord != null) {
            _entities.Add(new(assetRecord), entity);
        }
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var metadata = ref entity.Get<AssetMetadata>();
        var assetRecord = metadata.AssetSource;
        if (assetRecord != null) {
            _entities.Remove(new(assetRecord));
        }
    }

    public EntityRef CreateEntity(
        IAssetRecord record, AssetLife life = AssetLife.Automatic)
    {
        var type = record.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset record type");
        }
        return entry.EntityCreator(World, record, life);
    }

    public EntityRef CreateEntity(
        IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = CreateEntity(record, life);
        referrer.Refer(entity);
        return entity;
    }

    public EntityRef AcquireEntity(
        IAssetRecord record, AssetLife life = AssetLife.Automatic)
    {
        var key = new ObjectKey<IAssetRecord>(record);
        if (!_entities.TryGetValue(key, out var entity)) {
            entity = CreateEntity(record, life);
            _entities.Add(key, entity);
        }
        return entity;
    }

    public EntityRef AcquireEntity(
        IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = AcquireEntity(record, life);
        referrer.Refer(entity);
        return entity;
    }

    public bool TryGet(IAssetRecord record, out EntityRef entity)
        => _entities.TryGetValue(new(record), out entity);
}