﻿using System;
using System.Threading.Tasks;
using Vostok.ZooKeeper.Client.Abstractions;
using Vostok.ZooKeeper.Client.Abstractions.Model;
using Vostok.ZooKeeper.Client.Abstractions.Model.Request;
using Vostok.ZooKeeper.Client.Abstractions.Model.Result;

namespace Vostok.ServiceDiscovery.Helpers
{
    public static class ZooKeeperClientExtensions
    {
        public static async Task<bool> TryUpdateNodeDataAsync(
            this IZooKeeperClient zooKeeperClient,
            string path,
            Func<byte[], byte[]> updateBytesFunc,
            int attempts = 5)
        {
            for (var i = 0; i < attempts; i++)
            {
                var readResult = zooKeeperClient.GetData(path);
                if (!readResult.IsSuccessful)
                    return false;

                var newData = updateBytesFunc(readResult.Data);

                var request = new SetDataRequest(path, newData)
                {
                    Version = readResult.Stat.Version
                };

                var updateResult = await zooKeeperClient.SetDataAsync(request).ConfigureAwait(false);

                if (updateResult.Status == ZooKeeperStatus.VersionsMismatch)
                    continue;

                return updateResult.IsSuccessful;
            }

            return false;
        }
    }
}