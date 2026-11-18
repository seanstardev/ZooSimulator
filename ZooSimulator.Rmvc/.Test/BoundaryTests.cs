using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZooSimulator.Rmvc.Proxy.Clock;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    public sealed class BoundaryTests
    {
        [Fact]
        public void ConstructorsRejectNullAtTheOwningBoundary()
        {
            var commandError = Assert.Throws<ArgumentNullException>(
                () => new ZooChangedCmd(null!));

            var clockError = Assert.Throws<ArgumentNullException>(
                () => new ClockProxy(null!));

            var decayError = Assert.Throws<ArgumentNullException>(
                () => new HourDecayDto(null!));

            Assert.Equal("snapshot", commandError.ParamName);
            Assert.Equal("scheduler", clockError.ParamName);
            Assert.Equal("animals", decayError.ParamName);
        }

        [Fact]
        public void InitialZooContainsFiveOfEachSpeciesAtFullHealth()
        {
            var model = new ZooModel();

            model.Initialise();

            var snapshot = model.Snapshot();

            Assert.Equal(0, snapshot.HoursElapsed);
            Assert.Equal(0, snapshot.Revision);
            Assert.Equal(15, snapshot.Animals.Count);
            Assert.Equal(15, snapshot.Animals.Select(animal => animal.Id).Distinct().Count());

            foreach (AnimalSpeciesEnum species in Enum.GetValues<AnimalSpeciesEnum>())
                Assert.Equal(5, snapshot.Animals.Count(animal => animal.Species == species));

            Assert.All(snapshot.Animals, animal => Assert.Equal(100, animal.Health));
            Assert.All(snapshot.Animals, animal => Assert.Equal(AnimalStatusEnum.Alive, animal.Status));
            Assert.Throws<InvalidOperationException>(model.Initialise);
        }

        [Fact]
        public void InvalidHourIsRejectedBeforeAnyMutation()
        {
            var model = new ZooModel();

            model.Initialise();

            var initial = model.Snapshot();
            var entries = initial.Animals.Select(animal => new AnimalDecayDto(animal.Id, 20)).ToArray();

            entries[14] = new AnimalDecayDto(999, 20);

            Assert.Throws<ArgumentException>(() => model.AdvanceHour(new HourDecayDto(entries)));
            Assert.Throws<ArgumentException>(() => model.AdvanceHour(new HourDecayDto([])));

            Assert.Equal(0, model.Snapshot().HoursElapsed);

            Assert.All(model.Snapshot().Animals, animal => Assert.Equal(100, animal.Health));
        }

        [Fact]
        public void BoundaryCollectionsOwnTheirStorage()
        {
            AnimalDecayDto[] entries = [new AnimalDecayDto(1, 0)];

            var decay = new HourDecayDto(entries);

            entries[0] = new AnimalDecayDto(1, 20);

            Assert.Equal(0, decay.Animals[0].Percentage);
            Assert.Throws<NotSupportedException>(
                () => ((IList<AnimalDecayDto>)decay.Animals)[0] = entries[0]);

            var model = new ZooModel();

            model.Initialise();

            var snapshot = model.Snapshot();

            Assert.Throws<NotSupportedException>(
                () => ((IList<AnimalSnapshotDto>)snapshot.Animals).Clear());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(21)]
        public void DecayRejectsPercentagesOutsideAssignmentRange(int percentage)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new AnimalDecayDto(1, percentage));

        [Theory]
        [InlineData(9)]
        [InlineData(26)]
        public void FeedingRejectsPercentagesOutsideAssignmentRange(int percentage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FeedingDto(percentage, 10, 25));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FeedingDto(10, percentage, 25));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FeedingDto(10, 25, percentage));
        }

        [Fact]
        public void RandomProxyIncludesBothEndpointsAndSupportsConcurrentCalls()
        {
            var proxy = new RandomProxy();

            Assert.Equal(0, proxy.NextPercentage(0, 0));
            Assert.Equal(20, proxy.NextPercentage(20, 20));
            Assert.Equal(10, proxy.NextPercentage(10, 10));
            Assert.Equal(25, proxy.NextPercentage(25, 25));

            Parallel.For(0, 1000, index =>
            {
                Assert.InRange(proxy.NextPercentage(0, 20), 0, 20);
                Assert.InRange(proxy.NextPercentage(10, 25), 10, 25);
            });
        }

        [Fact]
        public async Task ClockStartsOnceAndReleasesItsOwnedScheduler()
        {
            var scheduler = new ManualScheduler();
            var clock = new ClockProxy(scheduler);

            clock.Start();
            clock.Start();

            Assert.Equal(TimeSpan.FromSeconds(20), scheduler.Interval);
            Assert.Equal(1, scheduler.StartCount);
            Assert.Equal(1, scheduler.SubscriberCount);

            long originalVersion = scheduler.ScheduleVersion;

            await Task.Delay(40);

            var observation = clock.ObserveTime(7, 0);

            Assert.Equal(7, observation.Revision);
            Assert.Equal(0, observation.HoursElapsed);
            Assert.Equal(20000, observation.HourDurationMilliseconds);
            Assert.InRange(observation.MillisecondsIntoHour, 1, 20000);
            Assert.True(observation.ObservedAtUnixMilliseconds > 0);

            clock.Restart();

            Assert.Equal(2, scheduler.StartCount);
            Assert.Equal(1, scheduler.SubscriberCount);
            Assert.False(clock.IsCurrentSchedule(originalVersion));
            Assert.True(clock.IsCurrentSchedule(scheduler.ScheduleVersion));

            await clock.Cleanup();
            await clock.Cleanup();

            Assert.True(scheduler.Disposed);
            Assert.Equal(1, scheduler.DisposeCount);
            Assert.Equal(0, scheduler.SubscriberCount);
            Assert.Throws<InvalidOperationException>(clock.Start);
        }

        [Fact]
        public async Task RealSchedulerDeliversRepeatedCallbacks()
        {
            using var scheduler = new TimerScheduler();

            var delivered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int callbacks = 0;

            scheduler.TickEvt += OnTick;
            scheduler.Start(TimeSpan.FromMilliseconds(20), 0);

            await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10));

            void OnTick(long version)
            {
                if (System.Threading.Interlocked.Increment(ref callbacks) >= 2)
                    delivered.TrySetResult(true);
            }
        }

        [Fact]
        public async Task ClockCleanupAlsoWorksBeforeStartup()
        {
            var scheduler = new ManualScheduler();
            var clock = new ClockProxy(scheduler);

            await clock.Cleanup();

            Assert.True(scheduler.Disposed);
            Assert.Equal(0, scheduler.StartCount);
        }
    }
}
