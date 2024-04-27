namespace Sia.Asset;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Sia;

public record struct AssetMetadata()
{
    public record struct OnReferred(EntityRef Entity) : IEvent;
    public record struct OnUnreferred(EntityRef Entity) : IEvent;

    public required Type AssetType { get; init; }
    public AssetLife AssetLife { get; init; }
    public IAssetRecord? AssetSource { get; init; }

    public readonly IReadOnlySet<Identity> Referrers =>
        _referrers ?? (IReadOnlySet<Identity>)ImmutableHashSet<Identity>.Empty;
    public readonly IReadOnlySet<Identity> Dependents =>
        _dependents ?? (IReadOnlySet<Identity>)ImmutableHashSet<Identity>.Empty;

    private HashSet<Identity>? _referrers;
    private HashSet<Identity>? _dependents;

    public readonly record struct Refer(EntityRef Asset) : ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());

        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            ref var dependents = ref metadata._dependents;
            dependents ??= [];
            if (!dependents.Add(Asset.Id)) {
                return;
            }

            ref var referers = ref Asset.Get<AssetMetadata>()._referrers;
            referers ??= [];
            referers.Add(target.Id);

            world.Send(Asset, new OnReferred(Asset));
        }
    }

    public readonly record struct Unrefer(EntityRef Asset) : ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());
            
        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            ref var dependents = ref metadata._dependents;
            if (dependents == null || !dependents.Remove(Asset.Id)) {
                return;
            }
            Asset.Get<AssetMetadata>()._referrers!.Remove(target.Id);
            world.Send(Asset, new OnUnreferred(Asset));
        }
    }

    public readonly EntityRef? FindReferrer<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referrers == null) {
            return null;
        }

        var assetType = typeof(TAsset);
        var world = World.Current;

        if (recurse) {
            foreach (var referrerId in _referrers) {
                var referrer = world[referrerId];
                ref var meta = ref referrer.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    return referrer;
                }
                if (meta._referrers != null) {
                    return meta.FindReferrer<TAsset>(recurse: true);
                }
            }
        }
        else {
            foreach (var referrerId in _referrers) {
                var referrer = world[referrerId];
                if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    return referrer;
                }
            }
        }
        return null;
    }

    public readonly EntityRef GetReferrer<TAsset>(bool recurse = false)
        where TAsset : struct
        => FindReferrer<TAsset>(recurse) ?? ThrowAssetNotFound<TAsset>();

    public readonly IEnumerable<EntityRef> GetReferrers<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referrers == null) {
            yield break;
        }

        var assetType = typeof(TAsset);
        var world = World.Current;

        if (recurse) {
            foreach (var referrerId in _referrers) {
                var referrer = world[referrerId];
                var meta = referrer.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    yield return referrer;
                }
                if (meta._referrers != null) {
                    foreach (var found in meta.GetReferrers<TAsset>(recurse: true)) {
                        yield return found;
                    }
                }
            }
        }
        else {
            foreach (var referrerId in _referrers) {
                var referrer = world[referrerId];
                if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    yield return referrer;
                }
            }
        }
    }

    public readonly EntityRef? FindDependent<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_dependents == null) {
            return null;
        }

        var assetType = typeof(TAsset);
        var world = World.Current;

        if (recurse) {
            foreach (var dependentId in _dependents) {
                var dependent = world[dependentId];
                ref var meta = ref dependent.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    return dependent;
                }
                if (meta._dependents != null) {
                    return meta.FindDependent<TAsset>(recurse: true);
                }
            }
        }
        else {
            foreach (var dependentId in _dependents) {
                var dependent = world[dependentId];
                if (dependent.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    return dependent;
                }
            }
        }
        return null;
    }

    public readonly EntityRef GetDependent<TAsset>(bool recurse = false)
        where TAsset : struct
        => FindDependent<TAsset>(recurse) ?? ThrowAssetNotFound<TAsset>(); 

    public readonly IEnumerable<EntityRef> GetDependents<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_dependents == null) {
            yield break;
        }

        var assetType = typeof(TAsset);
        var world = World.Current;

        if (recurse) {
            foreach (var dependentId in _dependents) {
                var dependent = world[dependentId];
                var meta = dependent.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    yield return dependent;
                }
                if (meta._dependents != null) {
                    foreach (var found in meta.GetDependents<TAsset>(recurse: true)) {
                        yield return found;
                    }
                }
            }
        }
        else {
            foreach (var dependentId in _dependents) {
                var dependent = world[dependentId];
                if (dependent.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    yield return dependent;
                }
            }
        }
    }

    [DoesNotReturn]
    private static EntityRef ThrowAssetNotFound<TAsset>()
        => throw new AssetNotFoundException("Asset not found: " + typeof(TAsset));
}