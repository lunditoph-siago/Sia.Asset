# Sia.Asset

Asset module for Sia.NET

## Example

```C#
using Sia;
using Sia.Asset;

public static partial class Example2_StatefulAsset
{
    public record struct TestState(int Curent)
    {
        public sealed class Changed : SingletonEvent<Changed>;
    }

    [SiaTemplate(nameof(Test))]
    [SiaAsset]
    public partial record RTest : AssetRecord
    {
        public required int Increment { get; init; }
    }

    public class TestUpdateSystem()
        : SystemBase(Matchers.Of<Test, TestState>())
    {
        public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        {
            query.ForSlice((ref Test test, ref TestState state) => {
                state.Curent += test.Increment;
            });
            foreach (var entity in query) {
                world.Send(entity, TestState.Changed.Instance);
            }
        }
    }

    public class TestModule()
        : TemplateEventSystemBase<Test, RTest, TestState>(
            SystemChain.Empty
                .Add<TestUpdateSystem>())
    {
        public override void Initialize(World world, Scheduler scheduler)
        {
            base.Initialize(world, scheduler);
            RecordEvent<TestState.Changed>();
        }

        protected override TestState Snapshot<TEvent>(
            in EntityRef entity, in TEvent e)
            => e is WorldEvents.Add<Test> ? default : entity.Get<TestState>();

        protected override void HandleEvent<TEvent>(
            in Identity id, in TestState snapshot, in TEvent e)
        {
            Console.WriteLine("Event: " + typeof(TEvent));
            switch (e) {
                case WorldEvents.Add<Test>:
                    Console.WriteLine("\tTest asset added: " + id);
                    World[id].Add<TestState>();
                    break;
                case WorldEvents.Remove<Test>:
                    Console.WriteLine("\tTest asset removed: " + id);
                    Console.WriteLine("\tLast value: " + snapshot.Curent);
                    break;
                case Test.SetIncrement cmd:
                    Console.WriteLine("\tIncrement set: " + cmd.Value);
                    break;
            }
        }
    }

    public static void Run(World world)
    {
        var scheduler = new Scheduler();
        SystemChain.Empty
            .Add<TestModule>()
            .RegisterTo(world, scheduler);
        
        var testRecord = new RTest {
            Increment = 5
        };

        var id = world.CreateInArrayHost()
            .AddBundle(Test.Create(testRecord))
            .Id;

        scheduler.Tick();
        scheduler.Tick();

        new Test.View(world[id]).Increment += 5;
        scheduler.Tick();

        world[id].Dispose();
        scheduler.Tick();
    }
}
```