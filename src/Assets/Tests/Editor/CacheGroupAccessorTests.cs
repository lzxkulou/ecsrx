﻿using System;
using EcsRx.Entities;
using EcsRx.Events;
using EcsRx.Groups;
using EcsRx.Pools;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using EcsRx.Tests.Components;
using UniRx;

namespace EcsRx.Tests
{
    [TestFixture]
    public class CacheableGroupAccessorTests
    {
        [Test]
        public void should_include_entity_snapshot_on_creation()
        {
            var mockEventSystem = Substitute.For<IEventSystem>();
            var accessorToken = new GroupAccessorToken(new Type[] { }, "default");

            var dummyEntitySnapshot = new List<IEntity>
            {
                new Entity(Guid.NewGuid(), mockEventSystem),
                new Entity(Guid.NewGuid(), mockEventSystem),
                new Entity(Guid.NewGuid(), mockEventSystem)
            };

            var cacheableGroupAccessor = new CacheableGroupAccessor(accessorToken, dummyEntitySnapshot, mockEventSystem);

            Assert.That(cacheableGroupAccessor.CachedEntities, Has.Count.EqualTo(3));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(dummyEntitySnapshot[0]));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(dummyEntitySnapshot[1]));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(dummyEntitySnapshot[2]));
        }

        [Test]
        public void should_only_cache_applicable_entity_when_applicable_entity_added()
        {
            var mockEventSystem = Substitute.For<IEventSystem>();
            var accessorToken = new GroupAccessorToken(new[] { typeof(TestComponentOne), typeof(TestComponentTwo) }, "default");
            var mockPool = Substitute.For<IPool>();
            
            var applicableEntity = new Entity(Guid.NewGuid(), mockEventSystem);
            applicableEntity.AddComponent<TestComponentOne>();
            applicableEntity.AddComponent<TestComponentTwo>();

            var unapplicableEntity = new Entity(Guid.NewGuid(), mockEventSystem);
            unapplicableEntity.AddComponent<TestComponentOne>();

            var underlyingEvent = new ReactiveProperty<EntityAddedEvent>(new EntityAddedEvent(applicableEntity, mockPool));
            mockEventSystem.Receive<EntityAddedEvent>().Returns(underlyingEvent);

            var cacheableGroupAccessor = new CacheableGroupAccessor(accessorToken, new IEntity[] { }, mockEventSystem);
            cacheableGroupAccessor.MonitorEntityChanges();
            
            underlyingEvent.SetValueAndForceNotify(new EntityAddedEvent(unapplicableEntity, mockPool));
            
            Assert.That(cacheableGroupAccessor.CachedEntities, Has.Count.EqualTo(1));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(applicableEntity));
        }

        [Test]
        public void should_only_remove_applicable_entity_when_entity_removed()
        {
            var mockEventSystem = Substitute.For<IEventSystem>();
            var accessorToken = new GroupAccessorToken(new[] { typeof(TestComponentOne), typeof(TestComponentTwo) }, "default");
            var mockPool = Substitute.For<IPool>();

            var existingEntityOne = new Entity(Guid.NewGuid(), mockEventSystem);
            existingEntityOne.AddComponent<TestComponentOne>();
            existingEntityOne.AddComponent<TestComponentTwo>();

            var existingEntityTwo = new Entity(Guid.NewGuid(), mockEventSystem);
            existingEntityTwo.AddComponent<TestComponentOne>();
            existingEntityTwo.AddComponent<TestComponentTwo>();

            var unapplicableEntity = new Entity(Guid.NewGuid(), mockEventSystem);
            unapplicableEntity.AddComponent<TestComponentOne>();

            var underlyingEvent = new ReactiveProperty<EntityRemovedEvent>(new EntityRemovedEvent(unapplicableEntity, mockPool));
            mockEventSystem.Receive<EntityRemovedEvent>().Returns(underlyingEvent);

            var cacheableGroupAccessor = new CacheableGroupAccessor(accessorToken, new IEntity[] { existingEntityOne, existingEntityTwo }, mockEventSystem);
            cacheableGroupAccessor.MonitorEntityChanges();

            underlyingEvent.SetValueAndForceNotify(new EntityRemovedEvent(existingEntityOne, mockPool));

            Assert.That(cacheableGroupAccessor.CachedEntities, Has.Count.EqualTo(1));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(existingEntityTwo));
        }

        [Test]
        public void should_only_remove_entity_when_components_no_longer_match_group()
        {
            var mockEventSystem = Substitute.For<IEventSystem>();
            var accessorToken = new GroupAccessorToken(new[] { typeof(TestComponentOne), typeof(TestComponentTwo) }, "default");

            var existingEntityOne = new Entity(Guid.NewGuid(), mockEventSystem);
            var componentToRemove = new TestComponentOne();
            existingEntityOne.AddComponent(componentToRemove);
            existingEntityOne.AddComponent<TestComponentTwo>();

            var existingEntityTwo = new Entity(Guid.NewGuid(), mockEventSystem);
            var unapplicableComponent = new TestComponentThree();
            existingEntityTwo.AddComponent<TestComponentOne>();
            existingEntityTwo.AddComponent<TestComponentTwo>();
            existingEntityTwo.AddComponent(unapplicableComponent);

            var dummyEventToSeedMock = new ComponentRemovedEvent(new Entity(Guid.NewGuid(), mockEventSystem), new TestComponentOne());
            var underlyingEvent = new ReactiveProperty<ComponentRemovedEvent>(dummyEventToSeedMock);
            mockEventSystem.Receive<ComponentRemovedEvent>().Returns(underlyingEvent);

            var cacheableGroupAccessor = new CacheableGroupAccessor(accessorToken, new IEntity[] { existingEntityOne, existingEntityTwo }, mockEventSystem);
            cacheableGroupAccessor.MonitorEntityChanges();

            existingEntityOne.RemoveComponent(componentToRemove);
            underlyingEvent.SetValueAndForceNotify(new ComponentRemovedEvent(existingEntityOne, componentToRemove));

            existingEntityTwo.RemoveComponent(unapplicableComponent);
            underlyingEvent.SetValueAndForceNotify(new ComponentRemovedEvent(existingEntityTwo, unapplicableComponent));

            Assert.That(cacheableGroupAccessor.CachedEntities, Has.Count.EqualTo(1));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(existingEntityTwo));
        }

        [Test]
        public void should_only_add_entity_when_components_match_group()
        {
            var mockEventSystem = Substitute.For<IEventSystem>();
            var accessorToken = new GroupAccessorToken(new[] { typeof(TestComponentOne), typeof(TestComponentTwo) }, "default");

            var existingEntityOne = new Entity(Guid.NewGuid(), mockEventSystem);
            var componentToAdd = new TestComponentOne();
            existingEntityOne.AddComponent<TestComponentTwo>();

            var existingEntityTwo = new Entity(Guid.NewGuid(), mockEventSystem);
            var unapplicableComponent = new TestComponentThree();
            existingEntityTwo.AddComponent<TestComponentOne>();

            var dummyEventToSeedMock = new ComponentAddedEvent(new Entity(Guid.NewGuid(), mockEventSystem), new TestComponentOne());
            var underlyingEvent = new ReactiveProperty<ComponentAddedEvent>(dummyEventToSeedMock);
            mockEventSystem.Receive<ComponentAddedEvent>().Returns(underlyingEvent);

            var cacheableGroupAccessor = new CacheableGroupAccessor(accessorToken, new IEntity[] {}, mockEventSystem);

            cacheableGroupAccessor.MonitorEntityChanges();

            existingEntityOne.AddComponent(componentToAdd);
            underlyingEvent.SetValueAndForceNotify(new ComponentAddedEvent(existingEntityOne, componentToAdd));

            existingEntityTwo.AddComponent(unapplicableComponent);
            underlyingEvent.SetValueAndForceNotify(new ComponentAddedEvent(existingEntityTwo, unapplicableComponent));

            Assert.That(cacheableGroupAccessor.CachedEntities, Has.Count.EqualTo(1));
            Assert.That(cacheableGroupAccessor.CachedEntities, Contains.Item(existingEntityOne));
        }
    }
}