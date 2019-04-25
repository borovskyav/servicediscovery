﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Environment;
using Vostok.ServiceDiscovery.Models;

namespace Vostok.ServiceDiscovery
{
    internal class ReplicaInfoBuilder : IReplicaInfoBuilder
    {
        private const string DependenciesDelimiter = ";";
        private List<KeyValuePair<string, string>> properties = new List<KeyValuePair<string, string>>();

        private ReplicaInfoBuilder()
        {
            Environment = "default";
            Application = Commons.Environment.EnvironmentInfo.Application;
            Host = Commons.Environment.EnvironmentInfo.Host;
            ProcessName = Commons.Environment.EnvironmentInfo.ProcessName;
            ProcessId = Commons.Environment.EnvironmentInfo.ProcessId;
            BaseDirectory = Commons.Environment.EnvironmentInfo.BaseDirectory;
            CommitHash = AssemblyCommitHashExtractor.ExtractFromEntryAssembly();
            ReleaseDate = AssemblyBuildTimeExtractor.ExtractFromEntryAssembly()?.ToString("O");
            Dependencies = AssemblyDependenciesExtractor.ExtractFromEntryAssembly();
        }

        public static ReplicaInfo Build(ReplicaInfoSetup setup)
        {
            var builder = new ReplicaInfoBuilder();
            setup?.Invoke(builder);
            return builder.Build();
        }

        public string Environment { get; set; }
        public string Application { get; set; }
        public string Replica { get; set; }

        public Uri Url { get; set; }
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }
        public string VirtualPath { get; set; }

        public string ProcessName { get; set; }
        public int? ProcessId { get; set; }
        public string BaseDirectory { get; set; }

        public string CommitHash { get; set; }
        public string ReleaseDate { get; set; }

        public List<string> Dependencies { get; set; }

        public IReplicaInfoBuilder AddProperty(string key, string value)
        {
            properties.Add(new KeyValuePair<string, string>(key ?? throw new ArgumentNullException(nameof(key)), value));
            return this;
        }

        public ReplicaInfo Build()
        {
            Url = Url ?? BuildUrl(Scheme, Port, VirtualPath);
            Replica = Url?.ToString() ?? $"{Commons.Environment.EnvironmentInfo.Host}({Commons.Environment.EnvironmentInfo.ProcessId})";

            if (Url != null)
            {
                Scheme = Url.Scheme;
                Port = Url.Port;
                VirtualPath = Url.AbsolutePath;
            }

            var result = new ReplicaInfo(Environment, Application, Replica);

            FillProperties(result);

            return result;
        }

        private static Uri BuildUrl(string scheme, int? port, string virtualPath)
        {
            if (port == null)
                return null;

            return new UriBuilder
            {
                Scheme = scheme ?? "http",
                Host = Commons.Environment.EnvironmentInfo.Host,
                Port = port.Value,
                Path = virtualPath ?? ""
            }.Uri;
        }

        private void FillProperties(ReplicaInfo replicaInfo)
        {
            replicaInfo.AddProperty(ReplicaInfoKeys.Environment, Environment);
            replicaInfo.AddProperty(ReplicaInfoKeys.Application, Application);
            replicaInfo.AddProperty(ReplicaInfoKeys.Replica, Replica);
            replicaInfo.AddProperty(ReplicaInfoKeys.Url, Url?.ToString());
            replicaInfo.AddProperty(ReplicaInfoKeys.Host, Host);
            replicaInfo.AddProperty(ReplicaInfoKeys.ProcessName, ProcessName);
            replicaInfo.AddProperty(ReplicaInfoKeys.ProcessId, ProcessId?.ToString());
            replicaInfo.AddProperty(ReplicaInfoKeys.BaseDirectory, BaseDirectory);
            replicaInfo.AddProperty(ReplicaInfoKeys.CommitHash, CommitHash);
            replicaInfo.AddProperty(ReplicaInfoKeys.ReleaseDate, ReleaseDate);
            replicaInfo.AddProperty(ReplicaInfoKeys.Dependencies, FormatDependencies());
            replicaInfo.AddProperty(ReplicaInfoKeys.Port, Port?.ToString());

            foreach (var property in properties)
            {
                replicaInfo.AddProperty(property.Key, property.Value);
            }
        }

        private string FormatDependencies()
        {
            return Dependencies == null
                ? null
                : string.Join(
                    DependenciesDelimiter,
                    Dependencies.Select(d => d?.Replace(DependenciesDelimiter, "_")));
        }
    }
}