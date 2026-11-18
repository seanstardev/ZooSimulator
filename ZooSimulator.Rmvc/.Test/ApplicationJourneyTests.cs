using System;
using System.Linq;
using System.Threading.Tasks;
using RMVC;
using Xunit;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    public sealed class ApplicationJourneyTests
    {
        // RMVC allows one creation per process. This is deliberately one lifecycle test.
        [Fact]
        public async Task ZooJourneyCoversClockRandomnessFeedingConcurrentMutationsAndShutdown()
        {
            var shell = new IntegrationShell();
            var view = new RecordingView();
            var main = new RecordingMainView();
            var feeding = new RecordingFeedingView();

            RFacade.Create<IntegrationFacade>(shell);

            try
            {
                await shell.Running.Task.WaitAsync(TimeSpan.FromSeconds(10));

                RFacade.RegisterActor<IntegrationFacade, IMainView>(main);
                RFacade.RegisterActor<IntegrationFacade, IZooView>(view);
                RFacade.RegisterActor<IntegrationFacade, IFeedingView>(feeding);
                main.RequestReady();

                var opening = await view.WaitForRevision(0);

                await Eventually(() => IntegrationFacade.Scheduler.StartCount == 1);

                main.RequestReady();

                Assert.All(opening.Animals, animal => Assert.Equal(100, animal.Health));

                // One separately drawn percentage per individual, in stable animal order.
                IntegrationFacade.Random.Enqueue(Enumerable.Range(0, 15).ToArray());
                IntegrationFacade.Scheduler.Fire();

                var decayed = await view.WaitForRevision(1);

                Assert.Equal(1, decayed.HoursElapsed);

                for (int index = 0; index < 15; index++)
                    Assert.Equal(100 - index, decayed.Animals[index].Health, 10);

                Assert.Equal(15, IntegrationFacade.Random.Calls);
                Assert.All(IntegrationFacade.Random.Minimums, value => Assert.Equal(0, value));
                Assert.All(IntegrationFacade.Random.Maximums, value => Assert.Equal(20, value));

                IntegrationFacade.Random.Enqueue(10, 20, 25);
                feeding.RequestFeed();

                var fed = await view.WaitForRevision(2);

                Assert.Equal(18, IntegrationFacade.Random.Calls);

                for (int index = 0; index < 15; index++)
                {
                    int percentage = new FeedingDto(10, 20, 25).PercentageFor(fed.Animals[index].Species);

                    Assert.Equal(
                        Math.Min(100, decayed.Animals[index].Health * (1 + percentage / 100.0)),
                        fed.Animals[index].Health,
                        10);
                }

                Assert.Equal(new[] { 10, 10, 10 }, IntegrationFacade.Random.Minimums.Skip(15));
                Assert.Equal(new[] { 25, 25, 25 }, IntegrationFacade.Random.Maximums.Skip(15));
                Assert.All(opening.Animals, animal => Assert.Equal(100, animal.Health));

                // Replay every committed revision to prove feeding and hourly mutation are atomic.
                var decay = new HourDecayDto(
                    fed.Animals.Select(animal => new AnimalDecayDto(animal.Id, 1)));

                Parallel.For(0, 20, index =>
                {
                    IntegrationFacade.Zoo.AdvanceHour(decay);
                    IntegrationFacade.Zoo.Feed(new FeedingDto(10, 10, 10));
                });

                var concurrent = IntegrationFacade.Zoo.Snapshot();

                Assert.Equal(21, concurrent.HoursElapsed);
                await view.WaitForRevision(concurrent.Revision);

                await Eventually(() => view.Deliveries.Select(snapshot => snapshot.Revision).Distinct().Count() == concurrent.Revision + 1);

                var ordered = view.Deliveries
                    .GroupBy(snapshot => snapshot.Revision)
                    .Select(group => group.First())
                    .OrderBy(snapshot => snapshot.Revision)
                    .ToArray();

                for (int index = 3; index < ordered.Length; index++)
                {
                    var previous = ordered[index - 1];
                    var current = ordered[index];

                    Assert.Equal(previous.Revision + 1, current.Revision);
                    Assert.InRange(current.HoursElapsed - previous.HoursElapsed, 0, 1);

                    for (int animal = 0; animal < 15; animal++)
                    {
                        double expected = current.HoursElapsed > previous.HoursElapsed
                            ? previous.Animals[animal].Health * 0.99
                            : Math.Min(100, previous.Animals[animal].Health * 1.1);

                        Assert.Equal(expected, current.Animals[animal].Health, 9);
                    }
                }

                // Six harsh hours kill all species without any real-time waits.
                for (int hour = 1; hour <= 6; hour++)
                {
                    IntegrationFacade.Random.Enqueue(Enumerable.Repeat(20, 15).ToArray());
                    IntegrationFacade.Scheduler.Fire();

                    var next = await view.WaitForRevision(concurrent.Revision + hour);

                    Assert.Equal(concurrent.HoursElapsed + hour, next.HoursElapsed);
                }

                var dead = IntegrationFacade.Zoo.Snapshot();

                Assert.All(dead.Animals, animal => Assert.Equal(AnimalStatusEnum.Dead, animal.Status));
                IntegrationFacade.Random.Enqueue(10, 20, 25);

                int previousCalls = IntegrationFacade.Random.Calls;

                feeding.RequestFeed();

                await Eventually(() => IntegrationFacade.Random.Calls == previousCalls + 3);

                Assert.Equal(dead.Revision, IntegrationFacade.Zoo.Snapshot().Revision);

                RFacade.UnregisterActor<IntegrationFacade, IZooView>(view);
                RFacade.UnregisterActor<IntegrationFacade, IFeedingView>(feeding);
                previousCalls = IntegrationFacade.Random.Calls;
                feeding.RequestFeed();

                Assert.Equal(previousCalls, IntegrationFacade.Random.Calls);

                var replacement = new RecordingView();

                RFacade.RegisterActor<IntegrationFacade, IZooView>(replacement);
                main.RequestReady();

                var reopened = await replacement.WaitForRevision(dead.Revision);

                Assert.Equal(dead.HoursElapsed, reopened.HoursElapsed);
                Assert.Equal(1, IntegrationFacade.Scheduler.StartCount);

                RFacade.UnregisterActor<IntegrationFacade, IZooView>(replacement);
                RFacade.UnregisterActor<IntegrationFacade, IMainView>(main);

                await ViewContractJourney.Verify();

                Assert.Equal(0, IntegrationFacade.Random.Remaining);
            }
            finally
            {
                await RFacade.Shutdown(10000);
            }

            Assert.True(IntegrationFacade.Scheduler.Disposed);
            Assert.Equal(0, IntegrationFacade.Scheduler.SubscriberCount);

            var stopped = IntegrationFacade.Zoo.Snapshot();

            IntegrationFacade.Scheduler.Fire();

            Assert.Equal(stopped.Revision, IntegrationFacade.Zoo.Snapshot().Revision);
            Assert.Empty(shell.Errors);
        }

        private static async Task Eventually(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);

            while (condition() == false)
            {
                Assert.True(
                    DateTime.UtcNow < deadline,
                    "Timed out waiting for command completion.");

                await Task.Delay(10);
            }
        }
    }
}
