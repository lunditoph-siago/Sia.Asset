namespace Sia.Asset;

public static class EntityAssetExtensions
{
    public static void Refer(this EntityRef entity, in EntityRef target)
        => entity.Modify(new AssetMetadata.Refer(target));
    
    public static void Unrefer(this EntityRef entity, in EntityRef target)
        => entity.Modify(new AssetMetadata.Unrefer(target));
    
    public static EntityRef? FindReferrer<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferrer<TAsset>(recurse);
    
    public static EntityRef GetReferrer<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().GetReferrer<TAsset>(recurse);
    
    public static IEnumerable<EntityRef> GetReferrers<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().GetReferrers<TAsset>(recurse);

    public static EntityRef? FindDependent<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindDependent<TAsset>(recurse);

    public static EntityRef GetDependent<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().GetDependent<TAsset>(recurse);

    public static IEnumerable<EntityRef> GetDependents<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().GetDependents<TAsset>(recurse);
}