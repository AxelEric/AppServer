/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;

using ASC.Common.Caching;
using ASC.Common.Threading.Progress;
using ASC.Data.Storage.Configuration;
using ASC.Migration;

using Microsoft.Extensions.DependencyInjection;

namespace ASC.Data.Storage.Migration
{
    public class ServiceClient : IService
    {
        public ICacheNotify<MigrationCache> CacheMigrationNotify { get; }
        public ICacheNotify<MigrationProgress> ProgressMigrationNotify { get; }
        public IServiceProvider ServiceProvider { get; }
        public ICache Cache { get; }

        public ServiceClient(
            ICacheNotify<MigrationCache> cacheMigrationNotify,
            ICacheNotify<MigrationProgress> progressMigrationNotify,
            IServiceProvider serviceProvider,
            ICache cache)
        {
            CacheMigrationNotify = cacheMigrationNotify;
            ProgressMigrationNotify = progressMigrationNotify;
            ServiceProvider = serviceProvider;
            Cache = cache;
        }

        public void Migrate(int tenant, StorageSettings storageSettings)
        {
            var storSettings = new BaseStorSettings { ID = storageSettings.ID.ToString(), Module = storageSettings.Module };

            CacheMigrationNotify.Publish(new MigrationCache
            {
                Tenant = tenant,
                StorSettings = storSettings
            },
                CacheNotifyAction.Insert);
        }

        public void UploadCdn(int tenantId, string relativePath, string mappedPath, CdnStorageSettings settings = null)
        {
            var cdnStorSettings = new BaseStorSettings { ID = settings.ID.ToString(), Module = settings.Module };

            CacheMigrationNotify.Publish(new MigrationCache
            {
                TenantId = tenantId,
                RelativePath = relativePath,
                MappedPath = mappedPath,
                CdnStorSettings = cdnStorSettings
            },
                CacheNotifyAction.Insert);
        }

        public double GetProgress(int tenant)
        {
            ProgressMigrationNotify.Subscribe(n =>
            {
                using var scope = ServiceProvider.CreateScope();
                var service = scope.ServiceProvider.GetService<ServiceClient>();
                service.GetProgress(n.TenantId);
            },
            CacheNotifyAction.Insert);

            Cache.Get<MigrationProgress>(GetCacheKey(tenant));

            var progress = (ProgressBase)StorageUploader.GetProgress(tenant) ?? StaticUploader.GetProgress(tenant);

            return progress != null ? progress.Percentage : -1;
        }

        private string GetCacheKey(int tenantId)
        {
            return typeof(MigrationProgress).FullName + tenantId;
        }

        public void StopMigrate()
        {
            CacheMigrationNotify.Publish(new MigrationCache(), CacheNotifyAction.InsertOrUpdate);
        }
    }
}
