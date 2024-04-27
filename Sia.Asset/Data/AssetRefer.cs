namespace Sia.Asset;

using System.Runtime.CompilerServices;
using Sia.Reactors;

public abstract record AssetRefer<TAssetRecord>
    where TAssetRecord : IAssetRecord
{
    public sealed record Record(TAssetRecord Value) : AssetRefer<TAssetRecord>;
    public sealed record Id(Identity Value) : AssetRefer<TAssetRecord>;
    public sealed record Entity(EntityRef Value) : AssetRefer<TAssetRecord>;
    public sealed record Name(string Value) : AssetRefer<TAssetRecord>;
    public sealed record Matcher(AssetMatcher Value) : AssetRefer<TAssetRecord>;

    public static implicit operator AssetRefer<TAssetRecord>(TAssetRecord record) => new Record(record);
    public static implicit operator AssetRefer<TAssetRecord>(Identity id) => new Id(id);
    public static implicit operator AssetRefer<TAssetRecord>(EntityRef entity) => new Entity(entity);
    public static implicit operator AssetRefer<TAssetRecord>(string name) => new Name(name);
    public static implicit operator AssetRefer<TAssetRecord>(AssetMatcher matcher) => new Matcher(matcher);

    public EntityRef? Find(World world)
    {
        var entity = DoFind(world);
        if (entity == null) {
            return null;
        }
        ref var meta = ref entity.Value.GetOrNullRef<AssetMetadata>();
        if (Unsafe.IsNullRef(ref meta) || !meta.AssetType.IsAssignableTo(typeof(IAsset<TAssetRecord>))) {
            return null;
        }
        return entity;
    }

    private EntityRef? DoFind(World world)
    {
        switch (this) {
            case Record(var record): {
                var assetLib = world.GetAddon<AssetLibrary>();
                return assetLib.TryGet(record, out var entity) ? entity : null;
            }
            case Id(var id):
                return world[id];
            case Name(var name): {
                var aggr = world.GetAddon<Aggregator<AssetName>>();
                return aggr.TryGet(new(name), out _, out var res) ? null : res.First;
            }
            case Entity e:
                return e.Value.Valid ? e.Value : null;
            case Matcher e: {
                var query = world.Query(e.Value.Matcher);
                if (query.Count == 0) {
                    return null;
                }
                var host = query.Hosts[0];
                return new(host.AllocatedSlots[0], host);
            }
            default:
                return null;
        }
    }
}