namespace Sia.Asset;

public static class WorldAssetLibraryExtensions
{
    public static EntityRef CreateAsset(
        this World world, IAssetRecord record, AssetLife life = AssetLife.Automatic)
        => world.GetAddon<AssetLibrary>().CreateEntity(record, life);

    public static EntityRef CreateAsset(
        this World world, IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
        => world.GetAddon<AssetLibrary>().CreateEntity(record, referrer, life);

    public static EntityRef AcquireAsset(
        this World world, IAssetRecord record, AssetLife life = AssetLife.Persistent)
        => world.GetAddon<AssetLibrary>().AcquireEntity(record, life);

    public static EntityRef AcquireAsset(
        this World world, IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
        => world.GetAddon<AssetLibrary>().AcquireEntity(record, referrer, life);

    public static EntityRef GetAsset(this World world, IAssetRecord record)
        => world.GetAddon<AssetLibrary>()[record];

    public static bool TryGetAsset(this World world, IAssetRecord record, out EntityRef entity)
        => world.GetAddon<AssetLibrary>().TryGet(record, out entity);
}