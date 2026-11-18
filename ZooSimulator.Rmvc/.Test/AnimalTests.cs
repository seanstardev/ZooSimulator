using System;
using Xunit;
using ZooSimulator.Rmvc.Model.Zoo;
using ZooSimulator.Rmvc.Shared;

namespace ZooSimulator.Rmvc.Tests
{
    public sealed class AnimalTests
    {
        [Fact]
        public void HealthChangesAreProportionalAndFeedingIsCapped()
        {
            var animal = new Monkey(1);

            animal.AdvanceHour(20, 1);

            Assert.Equal(80, animal.Health, 10);

            animal.Feed(10, 1);

            Assert.Equal(88, animal.Health, 10);

            animal.Feed(25, 1);

            Assert.Equal(100, animal.Health);
        }

        [Fact]
        public void MonkeySurvivesAtThirtyButDiesImmediatelyBelowIt()
        {
            var animal = new Monkey(1);

            animal.AdvanceHour(70, 1);

            Assert.Equal(30, animal.Health, 10);
            Assert.Equal(AnimalStatusEnum.Alive, animal.Status);

            animal.AdvanceHour(0.1, 2);

            Assert.Equal(AnimalStatusEnum.Dead, animal.Status);
        }

        [Fact]
        public void GiraffeSurvivesAtFiftyButDiesImmediatelyBelowIt()
        {
            var animal = new Giraffe(1);

            animal.AdvanceHour(50, 1);

            Assert.Equal(50, animal.Health);
            Assert.Equal(AnimalStatusEnum.Alive, animal.Status);

            animal.AdvanceHour(0.1, 2);

            Assert.Equal(AnimalStatusEnum.Dead, animal.Status);
        }

        [Fact]
        public void ElephantCanWalkAtSeventy()
        {
            var animal = new Elephant(1);

            animal.AdvanceHour(30, 1);

            Assert.Equal(70, animal.Health);
            Assert.Equal(AnimalStatusEnum.Alive, animal.Status);
        }

        [Fact]
        public void ElephantHasOneSubsequentHourToRecover()
        {
            var animal = new Elephant(1);

            animal.AdvanceHour(40, 1);

            Assert.Equal(AnimalStatusEnum.UnableToWalk, animal.Status);

            animal.Feed(10, 1);

            Assert.Equal(66, animal.Health, 10);
            Assert.Equal(AnimalStatusEnum.UnableToWalk, animal.Status);

            animal.AdvanceHour(20, 2);

            Assert.Equal(AnimalStatusEnum.Dead, animal.Status);
            Assert.Equal(66, animal.Health, 10);
        }

        [Fact]
        public void RecoveryAtExactlySeventyClearsTheDeadline()
        {
            var animal = new Elephant(1);

            animal.AdvanceHour(44, 1);
            animal.Feed(25, 1);

            Assert.Equal(70, animal.Health, 10);
            Assert.Equal(AnimalStatusEnum.Alive, animal.Status);

            animal.AdvanceHour(0, 2);

            Assert.Equal(AnimalStatusEnum.Alive, animal.Status);
        }

        [Fact]
        public void RecoveryFollowedByFreshDeclineStartsANewWindow()
        {
            var animal = new Elephant(1);

            animal.AdvanceHour(40, 1);
            animal.Feed(25, 1);

            Assert.Equal(75, animal.Health);

            animal.AdvanceHour(20, 2);

            Assert.Equal(60, animal.Health);
            Assert.Equal(AnimalStatusEnum.UnableToWalk, animal.Status);

            animal.AdvanceHour(0, 3);

            Assert.Equal(AnimalStatusEnum.Dead, animal.Status);
        }

        [Fact]
        public void DeathIsPermanentAndPreservesHealthAtDeath()
        {
            Animal[] animals = [new Monkey(1), new Giraffe(2), new Elephant(3)];

            foreach (var animal in animals)
            {
                animal.AdvanceHour(100, 1);
                animal.AdvanceHour(0, 2);

                Assert.Equal(AnimalStatusEnum.Dead, animal.Status);

                double healthAtDeath = animal.Health;

                Assert.False(animal.Feed(25, 2));

                animal.AdvanceHour(20, 3);

                Assert.Equal(healthAtDeath, animal.Health);
                Assert.Equal(AnimalStatusEnum.Dead, animal.Status);
            }
        }

        [Fact]
        public void AnimalSnapshotDoesNotFollowLaterMutation()
        {
            var animal = new Monkey(1);

            var before = animal.Snapshot();

            animal.AdvanceHour(20, 1);

            Assert.Equal(100, before.Health);
            Assert.Equal(80, animal.Snapshot().Health);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void InvalidArithmeticInputsDoNotMutateHealth(double percentage)
        {
            var animal = new Monkey(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => animal.AdvanceHour(percentage, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => animal.Feed(percentage, 1));
            Assert.Equal(100, animal.Health);
        }
    }
}
