﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.ServiceDiscovery.Models
{
    internal class ApplicationInfo : IApplicationInfo
    {
        public ApplicationInfo([NotNull] string environment, [NotNull] string application, [CanBeNull] IReadOnlyDictionary<string, string> properties)
        {
            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentOutOfRangeException(nameof(environment), environment);
            if (string.IsNullOrWhiteSpace(application))
                throw new ArgumentOutOfRangeException(nameof(application), application);
            Environment = environment;
            Application = application;
            Properties = properties ?? new Dictionary<string, string>();
        }

        [NotNull]
        public string Environment { get; }

        [NotNull]
        public string Application { get; }

        [NotNull]
        public IReadOnlyDictionary<string, string> Properties { get; }
    }
}