using System;
using System.Linq;
using System.Threading.Tasks;
using RMVC;
using Xunit;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    // Runs inside the scripted journey's one RMVC lifetime, after its original views detach.
    internal static class ViewContractJourney
    {
        internal static async Task Verify()
        {
            var main = new RecordingMainView();
            var feeding = new RecordingFeedingView();

            var previous = IntegrationFacade.Zoo.Snapshot();

            long revisionBase = previous.Revision + 1;
            long generationBase = previous.Generation + 1;
            int startCount = IntegrationFacade.Scheduler.StartCount + 1;

            RFacade.RegisterActor<IntegrationFacade, IMainView>(main);
            RFacade.RegisterActor<IntegrationFacade, IFeedingView>(feeding);
            main.RequestReset();

            await Eventually(() => IntegrationFacade.Zoo.Snapshot().Generation == generationBase);

            main.RequestReady();

            await Eventually(() => IntegrationFacade.Scheduler.StartCount == startCount);

            // Initial delivery is retained even while a workload view is unattached.
            var zoo = new RecordingView();
            var clock = new RecordingClockView();

            RFacade.RegisterActor<IntegrationFacade, IZooView>(zoo);
            RFacade.RegisterActor<IntegrationFacade, IClockView>(clock);

            var opening = await zoo.WaitForRevision(revisionBase);

            var openingTime = await clock.WaitForRevision(revisionBase);

            Assert.Equal(15, opening.Animals.Count);
            Assert.Equal(0, openingTime.HoursElapsed);
            Assert.Equal(1, feeding.SubscriberCount);

            // Direct state setup keeps this stage focused on view delivery and attachment.
            var decay = new HourDecayDto(
                opening.Animals.Select(animal => new AnimalDecayDto(animal.Id, 20)));

            IntegrationFacade.Zoo.AdvanceHour(decay);

            var decayed = await zoo.WaitForRevision(revisionBase + 1);

            var decayedTime = await clock.WaitForRevision(revisionBase + 1);

            Assert.All(decayed.Animals, animal => Assert.Equal(80, animal.Health));
            Assert.Equal(1, decayedTime.HoursElapsed);

            IntegrationFacade.Random.Enqueue(10, 20, 25);
            feeding.RequestFeed();

            var fed = await zoo.WaitForRevision(revisionBase + 2);

            var fedTime = await clock.WaitForRevision(revisionBase + 2);

            Assert.Equal(1, fedTime.HoursElapsed);
            Assert.Equal(fed.Revision, fedTime.Revision);

            foreach (AnimalSpeciesEnum species in Enum.GetValues<AnimalSpeciesEnum>())
            {
                var health = fed.Animals.Where(animal => animal.Species == species)
                    .Select(animal => animal.Health).ToArray();

                Assert.All(health, value => Assert.InRange(value, 88, 100));
                Assert.All(health, value => Assert.Equal(health[0], value));
            }

            IntegrationFacade.Zoo.AdvanceHour(decay);

            var beforeTarget = await zoo.WaitForRevision(revisionBase + 3);

            IntegrationFacade.Random.Enqueue(10);
            feeding.RequestAnimalFeed(1);

            var afterTarget = await zoo.WaitForRevision(revisionBase + 4);

            Assert.True(afterTarget.Animals[0].Health > beforeTarget.Animals[0].Health);
            Assert.Equal(beforeTarget.HoursElapsed, afterTarget.HoursElapsed);

            for (int index = 1; index < 15; index++)
                Assert.Equal(beforeTarget.Animals[index].Health, afterTarget.Animals[index].Health);

            IntegrationFacade.Random.Enqueue(Enumerable.Repeat(20, 15).ToArray());
            IntegrationFacade.Scheduler.Fire();

            var advanced = await zoo.WaitForRevision(revisionBase + 5);

            var advancedTime = await clock.WaitForRevision(revisionBase + 5);

            Assert.Equal(3, advancedTime.HoursElapsed);
            Assert.Equal(advanced.HoursElapsed, advancedTime.HoursElapsed);
            Assert.Equal(0, openingTime.HoursElapsed);

            RFacade.UnregisterActor<IntegrationFacade, IFeedingView>(feeding);

            Assert.Equal(0, feeding.SubscriberCount);

            feeding.RequestFeed();
            RFacade.UnregisterActor<IntegrationFacade, IZooView>(zoo);
            RFacade.UnregisterActor<IntegrationFacade, IClockView>(clock);

            var replacementZoo = new RecordingView();
            var replacementClock = new RecordingClockView();

            RFacade.RegisterActor<IntegrationFacade, IZooView>(replacementZoo);
            RFacade.RegisterActor<IntegrationFacade, IClockView>(replacementClock);
            main.RequestReady();

            var reopened = await replacementZoo.WaitForRevision(revisionBase + 5);

            var reopenedTime = await replacementClock.WaitForRevision(revisionBase + 5);

            Assert.Equal(advanced.HoursElapsed, reopened.HoursElapsed);
            Assert.Equal(3, reopenedTime.HoursElapsed);
            Assert.Equal(startCount, IntegrationFacade.Scheduler.StartCount);

            long oldSchedule = IntegrationFacade.Scheduler.ScheduleVersion;

            main.RequestReset();

            var reset = await replacementZoo.WaitForRevision(revisionBase + 6);

            var resetTime = await replacementClock.WaitForRevision(revisionBase + 6);

            Assert.Equal(0, reset.HoursElapsed);
            Assert.Equal(0, resetTime.HoursElapsed);
            Assert.Equal(generationBase + 1, reset.Generation);
            Assert.Equal(15, reset.Animals.Count);
            Assert.All(reset.Animals, animal => Assert.Equal(100, animal.Health));
            Assert.All(reset.Animals, animal => Assert.Equal(AnimalStatusEnum.Alive, animal.Status));
            Assert.Equal(startCount + 1, IntegrationFacade.Scheduler.StartCount);

            IntegrationFacade.Scheduler.Fire(oldSchedule);

            Assert.Equal(revisionBase + 6, IntegrationFacade.Zoo.Snapshot().Revision);

            IntegrationFacade.Random.Enqueue(Enumerable.Repeat(20, 15).ToArray());
            IntegrationFacade.Scheduler.Fire();

            var afterReset = await replacementZoo.WaitForRevision(revisionBase + 7);

            Assert.Equal(1, afterReset.HoursElapsed);
            Assert.Equal(generationBase + 1, afterReset.Generation);
            Assert.Equal(0, reset.HoursElapsed);

            for (int hour = 0; hour < 6; hour++)
                IntegrationFacade.Zoo.AdvanceHour(decay);

            var ended = IntegrationFacade.Zoo.Snapshot();

            Assert.All(ended.Animals, animal => Assert.Equal(AnimalStatusEnum.Dead, animal.Status));

            main.RequestReset();

            var secondReset = await replacementZoo.WaitForRevision(ended.Revision + 1);

            Assert.Equal(generationBase + 2, secondReset.Generation);
            Assert.Equal(0, secondReset.HoursElapsed);
            Assert.All(secondReset.Animals, animal => Assert.Equal(100, animal.Health));
            Assert.All(secondReset.Animals, animal => Assert.Equal(AnimalStatusEnum.Alive, animal.Status));
            Assert.All(ended.Animals, animal => Assert.Equal(AnimalStatusEnum.Dead, animal.Status));
            Assert.Equal(startCount + 2, IntegrationFacade.Scheduler.StartCount);

            RFacade.UnregisterActor<IntegrationFacade, IMainView>(main);
            RFacade.UnregisterActor<IntegrationFacade, IZooView>(replacementZoo);
            RFacade.UnregisterActor<IntegrationFacade, IClockView>(replacementClock);
        }

        private static async Task Eventually(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);

            while (condition() == false)
            {
                Assert.True(
                    DateTime.UtcNow < deadline,
                    "Timed out waiting for view startup.");

                await Task.Delay(10);
            }
        }
    }
}
