﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Helpers.Url;
using Vostok.Commons.Testing;
using Vostok.ServiceDiscovery.Models;

namespace Vostok.ServiceDiscovery.Tests
{
    [TestFixture]
    internal class ServiceLocator_Tests : TestsBase
    {
        [Test]
        public void Should_locate_registered_ServiceBeacon_service()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");
            CreateEnvironmentNode(replica.Environment);

            using (var beacon = GetServiceBeacon(replica))
            {
                beacon.Start();
                WaitReplicaRegistered(replica);

                using (var locator = GetServiceLocator())
                {
                    ShouldLocateImmediately(locator, replica.Environment, replica.Application, replica.Replica);
                }
            }
        }

        [Test]
        public void Should_locate_multiple_registered_ServiceBeacon_services_in_multiple_environments()
        {
            var replica11 = new ReplicaInfo("A", "vostok", "https://github.com/vostok/A1");
            var replica12 = new ReplicaInfo("A", "vostok", "https://github.com/vostok/A2");
            var replica2 = new ReplicaInfo("A", "not-vostok", "https://github.com/not-vostok/A");
            var replica3 = new ReplicaInfo("B", "vostok", "https://github.com/vostok/B");

            CreateEnvironmentNode("A");
            CreateEnvironmentNode("B");

            using (var beacon11 = GetServiceBeacon(replica11))
            using (var beacon12 = GetServiceBeacon(replica12))
            using (var beacon2 = GetServiceBeacon(replica3))
            using (var beacon3 = GetServiceBeacon(replica2))
            {
                beacon11.Start();
                beacon12.Start();
                beacon2.Start();
                beacon3.Start();
                WaitReplicaRegistered(replica11);
                WaitReplicaRegistered(replica12);
                WaitReplicaRegistered(replica2);
                WaitReplicaRegistered(replica3);

                using (var locator = GetServiceLocator())
                {
                    ShouldLocateImmediately(locator, "A", "vostok", replica11.Replica, replica12.Replica);
                    ShouldLocateImmediately(locator, "A", "not-vostok", replica2.Replica);
                    ShouldLocateImmediately(locator, "B", "vostok", replica3.Replica);
                }
            }
        }

        [Test]
        public void Should_locate_ServiceBeacon_service_after_registration()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");
            CreateEnvironmentNode(replica.Environment);

            using (var beacon = GetServiceBeacon(replica))
            {
                using (var locator = GetServiceLocator())
                {
                    ShouldNotLocateImmediately(locator, replica.Environment, replica.Application);

                    beacon.Start();
                    WaitReplicaRegistered(replica);

                    ShouldLocate(locator, replica.Environment, replica.Application, replica.Replica);
                }
            }
        }

        [Test]
        public void Should_not_locate_stopped_ServiceBeacon_service()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");
            CreateEnvironmentNode(replica.Environment);

            using (var beacon = GetServiceBeacon(replica))
            {
                beacon.Start();
                WaitReplicaRegistered(replica);

                using (var locator = GetServiceLocator())
                {
                    ShouldLocateImmediately(locator, replica.Environment, replica.Application, replica.Replica);

                    beacon.Stop();

                    ShouldLocate(locator, replica.Environment, replica.Application);
                }
            }
        }

        [Test]
        public void Should_skip_environment_without_application()
        {
            var replica = new ReplicaInfo("parent", "vostok", "https://github.com/vostok");
            
            CreateEnvironmentNode("parent");
            CreateEnvironmentNode("child", "parent");

            CreateApplicationNode("parent", "vostok");

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "child", "vostok", replica.Replica);
                ShouldLocate(locator, "parent", "vostok", replica.Replica);
            }
        }

        [Test]
        public void Should_not_skip_environment_with_application()
        {
            var replicaParent = new ReplicaInfo("parent", "vostok", "https://github.com/vostok/parent");
            var replicaChild = new ReplicaInfo("child", "vostok", "https://github.com/vostok/child");

            CreateEnvironmentNode("parent");
            CreateEnvironmentNode("child", "parent");

            CreateApplicationNode("child", "vostok");
            CreateApplicationNode("parent", "vostok");

            CreateReplicaNode(replicaParent);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "child", "vostok");
                ShouldLocate(locator, "parent", "vostok", replicaParent.Replica);

                CreateReplicaNode(replicaChild);

                ShouldLocate(locator, "child", "vostok", replicaChild.Replica);
                ShouldLocate(locator, "parent", "vostok", replicaParent.Replica);
            }
        }

        [Test]
        public void Should_skip_environment_with_application_if_specified()
        {
            var replicaParent = new ReplicaInfo("parent", "vostok", "https://github.com/vostok/parent");
            var replicaChild = new ReplicaInfo("child", "vostok", "https://github.com/vostok/child");

            CreateEnvironmentNode("parent");
            CreateEnvironmentNode("child", "parent", new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "True"}});

            CreateApplicationNode("child", "vostok");
            CreateApplicationNode("parent", "vostok");

            CreateReplicaNode(replicaParent);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "child", "vostok", replicaParent.Replica);
                ShouldLocate(locator, "parent", "vostok", replicaParent.Replica);

                CreateReplicaNode(replicaChild);

                ShouldLocate(locator, "child", "vostok", replicaChild.Replica);
                ShouldLocate(locator, "parent", "vostok", replicaParent.Replica);
            }
        }

        [Test]
        public void Should_track_replicas_registration_in_nested_environments()
        {
            var replica1Parent = new ReplicaInfo("parent", "vostok", "https://github.com/vostok1/parent");
            var replica2Parent = new ReplicaInfo("parent", "vostok", "https://github.com/vostok2/parent");
            var replica1Child = new ReplicaInfo("child", "vostok", "https://github.com/vostok1/child");
            var replica2Child = new ReplicaInfo("child", "vostok", "https://github.com/vostok2/child");

            CreateEnvironmentNode("parent");
            CreateEnvironmentNode("child", "parent");

            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "parent", "vostok");
                ShouldNotLocate(locator, "child", "vostok");

                CreateApplicationNode("parent", "vostok");

                ShouldLocate(locator, "parent", "vostok");
                ShouldLocate(locator, "child", "vostok");

                CreateReplicaNode(replica1Parent);
                ShouldLocate(locator, "parent", "vostok", replica1Parent.Replica);
                ShouldLocate(locator, "child", "vostok", replica1Parent.Replica);

                CreateReplicaNode(replica2Parent);
                ShouldLocate(locator, "parent", "vostok", replica1Parent.Replica, replica2Parent.Replica);
                ShouldLocate(locator, "child", "vostok", replica1Parent.Replica, replica2Parent.Replica);

                CreateApplicationNode("child", "vostok");
                ShouldLocate(locator, "parent", "vostok", replica1Parent.Replica, replica2Parent.Replica);
                ShouldLocate(locator, "child", "vostok");

                CreateReplicaNode(replica1Child);
                ShouldLocate(locator, "parent", "vostok", replica1Parent.Replica, replica2Parent.Replica);
                ShouldLocate(locator, "child", "vostok", replica1Child.Replica);

                CreateReplicaNode(replica2Child);
                ShouldLocate(locator, "parent", "vostok", replica1Parent.Replica, replica2Parent.Replica);
                ShouldLocate(locator, "child", "vostok", replica1Child.Replica, replica2Child.Replica);
            }
        }

        [Test]
        public void Should_track_replica_deletion()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default");

            CreateApplicationNode("default", "vostok");

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok", replica.Replica);

                DeleteReplicaNode(replica);

                ShouldLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_track_application_deletion()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default");

            CreateApplicationNode("default", "vostok");

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok", replica.Replica);

                DeleteApplicationNode("default", "vostok");

                ShouldNotLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_track_environment_deletion()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default");

            CreateApplicationNode("default", "vostok");

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok", replica.Replica);

                DeleteEnvironmentNode("default");

                ShouldNotLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_not_go_to_parent_of_deleted_environment()
        {
            var replica = new ReplicaInfo("parent", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("parent");
            CreateEnvironmentNode("child", "parent");

            CreateApplicationNode("parent", "vostok");

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "child", "vostok", replica.Replica);

                DeleteEnvironmentNode("child");

                ShouldNotLocate(locator, "child", "vostok");
            }
        }

        [Test]
        public void Should_track_application_properties()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");
            var properties = new Dictionary<string, string>
            {
                {"key1", "value1"},
                {"key2", "value2"}
            };

            CreateEnvironmentNode("default");

            CreateApplicationNode("default", "vostok", properties);

            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                locator.Locate("default", "vostok").Properties.Should().BeEquivalentTo(properties);

                properties["key3"] = "value3";
                CreateApplicationNode("default", "vostok", properties);

                // ReSharper disable once AccessToDisposedClosure
                Action action = () => { locator.Locate("default", "vostok").Properties.Should().BeEquivalentTo(properties); };

                action.ShouldPassIn(DefaultTimeout);
            }
        }

        [Test]
        public void Should_works_disconnected()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default");
            CreateApplicationNode("default", "vostok");
            CreateReplicaNode(replica);

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok", replica.Replica);

                Ensemble.Stop();

                for (var times = 0; times < 10; times++)
                {
                    ShouldLocate(locator, "default", "vostok", replica.Replica);
                    Thread.Sleep(100.Milliseconds());
                }
            }
        }

        [Test]
        public void Should_locate_after_connect()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default");
            CreateApplicationNode("default", "vostok");
            CreateReplicaNode(replica);

            Ensemble.Stop();

            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "default", "vostok");

                Ensemble.Start();

                ShouldLocate(locator, "default", "vostok", replica.Replica);
            }
        }

        [Test]
        public void Should_not_locate_without_environment()
        {
            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_not_locate_without_application()
        {
            CreateEnvironmentNode("default");
            
            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_locate_empty_without_replicas()
        {
            CreateEnvironmentNode("default");
            CreateApplicationNode("default", "vostok");

            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_ignore_cycled_if_resolved()
        {
            var replica = new ReplicaInfo("default", "vostok", "https://github.com/vostok");

            CreateEnvironmentNode("default", "default");
            CreateReplicaNode(replica);
            
            using (var locator = GetServiceLocator())
            {
                ShouldLocate(locator, "default", "vostok", replica.Replica);
            }
        }

        [Test]
        public void Should_ignore_cycled()
        {
            CreateEnvironmentNode("default", "default");

            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "default", "vostok");
            }
        }

        [Test]
        public void Should_ignore_cycled_long()
        {
            CreateEnvironmentNode("A", "B");
            CreateEnvironmentNode("B", "C");
            CreateEnvironmentNode("C", "D");
            CreateEnvironmentNode("D", "E");
            CreateEnvironmentNode("E", "A");

            using (var locator = GetServiceLocator())
            {
                ShouldNotLocate(locator, "A", "B");
                ShouldNotLocate(locator, "B", "B");
                ShouldNotLocate(locator, "C", "B");
                ShouldNotLocate(locator, "D", "B");
                ShouldNotLocate(locator, "E", "B");
            }
        }

        private void ShouldLocate(ServiceLocator locator, string environment, string application, params string[] replicas)
        {
            Action assertion = () =>
            {
                ShouldLocateImmediately(locator, environment, application, replicas);
            };
            assertion.ShouldPassIn(DefaultTimeout);
        }

        private static void ShouldLocateImmediately(ServiceLocator locator, string environment, string application, params string[] replicas)
        {
            var topology = locator.Locate(environment, application);
            topology.Should().NotBeNull();
            topology.Replicas.Should().BeEquivalentTo(UrlParser.Parse(replicas).Cast<object>());
        }

        private void ShouldNotLocate(ServiceLocator locator, string environment, string application)
        {
            Action assertion = () =>
            {
                ShouldNotLocateImmediately(locator, environment, application);
            };
            assertion.ShouldPassIn(DefaultTimeout);
        }

        private static void ShouldNotLocateImmediately(ServiceLocator locator, string environment, string application)
        {
            locator.Locate(environment, application).Should().BeNull();
        }

        private ServiceLocator GetServiceLocator(string logPrefix = null)
        {
            return new ServiceLocator(ZooKeeperClient, null, string.IsNullOrEmpty(logPrefix) ? Log : Log.ForContext(logPrefix));
        }
    }
}