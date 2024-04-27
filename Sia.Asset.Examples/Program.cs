using Sia;
using Sia_Asset_Examples;

void Invoke(Action<World> action)
{
    Console.WriteLine("== " + action.Method.DeclaringType + " ==");
    var world = new World();
    Context<World>.Current = world;
    action(world);
    world.Dispose();
    Console.WriteLine();
}

Invoke(Example1_TextAsset.Run);
Invoke(Example2_StatefulAsset.Run);