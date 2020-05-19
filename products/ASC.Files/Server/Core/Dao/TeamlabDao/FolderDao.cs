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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.ElasticSearch;
using ASC.Files.Core.EF;
using ASC.Files.Core.Thirdparty;
using ASC.Files.Resources;
using ASC.Files.Thirdparty.ProviderDao;
using ASC.Web.Files.Classes;
using ASC.Web.Studio.Core;
using ASC.Web.Studio.UserControls.Statistics;
using ASC.Web.Studio.Utility;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ASC.Files.Core.Data
{
    internal class FolderDao : AbstractDao, IFolderDao<int>
    {
        private const string my = "my";
        private const string common = "common";
        private const string share = "share";
        private const string trash = "trash";
        private const string projects = "projects";

        public FactoryIndexer<DbFolder> FactoryIndexer { get; }
        public GlobalSpace GlobalSpace { get; }
        public IDaoFactory DaoFactory { get; }
        public ProviderFolderDao ProviderFolderDao { get; }
        public CrossDao CrossDao { get; }

        public FolderDao(
            FactoryIndexer<DbFolder> factoryIndexer,
            UserManager userManager,
            DbContextManager<FilesDbContext> dbContextManager,
            TenantManager tenantManager,
            TenantUtil tenantUtil,
            SetupInfo setupInfo,
            TenantExtra tenantExtra,
            TenantStatisticsProvider tenantStatisticProvider,
            CoreBaseSettings coreBaseSettings,
            CoreConfiguration coreConfiguration,
            SettingsManager settingsManager,
            AuthContext authContext,
            IServiceProvider serviceProvider,
            GlobalSpace globalSpace,
            IDaoFactory daoFactory,
            ProviderFolderDao providerFolderDao,
            CrossDao crossDao)
            : base(
                  dbContextManager,
                  userManager,
                  tenantManager,
                  tenantUtil,
                  setupInfo,
                  tenantExtra,
                  tenantStatisticProvider,
                  coreBaseSettings,
                  coreConfiguration,
                  settingsManager,
                  authContext,
                  serviceProvider)
        {
            FactoryIndexer = factoryIndexer;
            GlobalSpace = globalSpace;
            DaoFactory = daoFactory;
            ProviderFolderDao = providerFolderDao;
            CrossDao = crossDao;
        }

        public async Task<Folder<int>> GetFolder(int folderId)
        {
            var query = GetFolderQuery(r => r.Id == folderId).AsNoTracking();
            return ToFolder(await FromQueryWithShared(query).SingleOrDefaultAsync());
        }

        public async Task<Folder<int>> GetFolder(string title, int parentId)
        {
            if (string.IsNullOrEmpty(title)) throw new ArgumentNullException(title);

            var query = GetFolderQuery(r => r.Title == title && r.ParentId == parentId).AsNoTracking()
                .OrderBy(r => r.CreateOn);

            return ToFolder(await FromQueryWithShared(query).FirstOrDefaultAsync());
        }

        public async Task<Folder<int>> GetRootFolder(int folderId)
        {
            var id = await FilesDbContext.Tree
                .AsNoTracking()
                .Where(r => r.FolderId == folderId)
                .OrderByDescending(r => r.Level)
                .Select(r => r.ParentId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            var query = GetFolderQuery(r => r.Id == id).AsNoTracking();

            return ToFolder(await FromQueryWithShared(query).SingleOrDefaultAsync());
        }

        public async Task<Folder<int>> GetRootFolderByFile(int fileId)
        {
            var subq = Query(FilesDbContext.Files).AsNoTracking()
                .Where(r => r.Id == fileId && r.CurrentVersion)
                .Select(r => r.FolderId)
                .Distinct();

            var q = await FilesDbContext.Tree.AsNoTracking()
                .Where(r => subq.Any(q => q == r.FolderId))
                .OrderByDescending(r => r.Level)
                .Select(r => r.ParentId)
                .FirstOrDefaultAsync();

            var query = GetFolderQuery(r => r.Id == q).AsNoTracking();
            return ToFolder(await FromQueryWithShared(query).SingleOrDefaultAsync());
        }

        public async Task<List<Folder<int>>> GetFolders(int parentId)
        {
            return await GetFolders(parentId, default, default, false, default, string.Empty);
        }

        public async Task<List<Folder<int>>> GetFolders(int parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, bool withSubfolders = false)
        {
            if (filterType == FilterType.FilesOnly || filterType == FilterType.ByExtension
                || filterType == FilterType.DocumentsOnly || filterType == FilterType.ImagesOnly
                || filterType == FilterType.PresentationsOnly || filterType == FilterType.SpreadsheetsOnly
                || filterType == FilterType.ArchiveOnly || filterType == FilterType.MediaOnly)
                return new List<Folder<int>>();

            if (orderBy == null) orderBy = new OrderBy(SortedByType.DateAndTime, false);

            var q = GetFolderQuery(r => r.ParentId == parentId).AsNoTracking();

            if (withSubfolders)
            {
                q = GetFolderQuery().AsNoTracking()
                    .Join(FilesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                    .Where(r => r.tree.ParentId == parentId && r.tree.Level != 0)
                    .Select(r => r.folder);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                if (FactoryIndexer.TrySelectIds(s => s.MatchAll(searchText), out var searchIds))
                {
                    q = q.Where(r => searchIds.Any(a => a == r.Id));
                }
                else
                {
                    q = BuildSearch(q, searchText, SearhTypeEnum.Any);
                }
            }

            switch (orderBy.SortedBy)
            {
                case SortedByType.Author:
                    q = orderBy.IsAsc ? q.OrderBy(r => r.CreateBy) : q.OrderByDescending(r => r.CreateBy);
                    break;
                case SortedByType.AZ:
                    q = orderBy.IsAsc ? q.OrderBy(r => r.Title) : q.OrderByDescending(r => r.Title);
                    break;
                case SortedByType.DateAndTime:
                    q = orderBy.IsAsc ? q.OrderBy(r => r.ModifiedOn) : q.OrderByDescending(r => r.ModifiedOn);
                    break;
                case SortedByType.DateAndTimeCreation:
                    q = orderBy.IsAsc ? q.OrderBy(r => r.CreateOn) : q.OrderByDescending(r => r.CreateOn);
                    break;
                default:
                    q = q.OrderBy(r => r.Title);
                    break;
            }

            if (subjectID != Guid.Empty)
            {
                if (subjectGroup)
                {
                    var users = UserManager.GetUsersByGroup(subjectID).Select(u => u.ID).ToArray();
                    q = q.Where(r => users.Any(a => a == r.CreateBy));
                }
                else
                {
                    q = q.Where(r => r.CreateBy == subjectID);
                }
            }

            return await FromQueryWithShared(q).AsAsyncEnumerable().Select(ToFolder).ToListAsync();
        }

        public async Task<List<Folder<int>>> GetFolders(int[] folderIds, FilterType filterType = FilterType.None, bool subjectGroup = false, Guid? subjectID = null, string searchText = "", bool searchSubfolders = false, bool checkShare = true)
        {
            if (filterType == FilterType.FilesOnly || filterType == FilterType.ByExtension
                || filterType == FilterType.DocumentsOnly || filterType == FilterType.ImagesOnly
                || filterType == FilterType.PresentationsOnly || filterType == FilterType.SpreadsheetsOnly
                || filterType == FilterType.ArchiveOnly || filterType == FilterType.MediaOnly)
                return new List<Folder<int>>();

            var q = GetFolderQuery(r => folderIds.Any(q => q == r.Id)).AsNoTracking();

            if (searchSubfolders)
            {
                q = GetFolderQuery()
                    .AsNoTracking()
                    .Join(FilesDbContext.Tree, r => r.Id, a => a.FolderId, (folder, tree) => new { folder, tree })
                    .Where(r => folderIds.Any(q => q == r.folder.ParentId))
                    .Select(r => r.folder);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                if (FactoryIndexer.TrySelectIds(s =>
                                                    searchSubfolders
                                                        ? s.MatchAll(searchText)
                                                        : s.MatchAll(searchText).In(r => r.Id, folderIds),
                                                    out var searchIds))
                {
                    q = q.Where(r => searchIds.Any(a => a == r.Id));
                }
                else
                {
                    q = BuildSearch(q, searchText, SearhTypeEnum.Any);
                }
            }


            if (subjectID.HasValue && subjectID != Guid.Empty)
            {
                if (subjectGroup)
                {
                    var users = UserManager.GetUsersByGroup(subjectID.Value).Select(u => u.ID).ToArray();
                    q = q.Where(r => users.Any(a => a == r.CreateBy));
                }
                else
                {
                    q = q.Where(r => r.CreateBy == subjectID);
                }
            }

            return await (checkShare ? FromQueryWithShared(q) : FromQuery(q)).AsAsyncEnumerable().Select(ToFolder).ToListAsync();
        }

        public async Task<List<Folder<int>>> GetParentFolders(int folderId)
        {
            var q = GetFolderQuery()
                .AsNoTracking()
                .Join(FilesDbContext.Tree, r => r.Id, a => a.ParentId, (folder, tree) => new { folder, tree })
                .Where(r => r.tree.FolderId == folderId)
                .OrderByDescending(r => r.tree.Level)
                .Select(r => r.folder);

            return await FromQueryWithShared(q).AsAsyncEnumerable().Select(ToFolder).ToListAsync();
        }

        public async Task<int> SaveFolder(Folder<int> folder)
        {
            return await SaveFolder(folder, null);
        }

        public async Task<int> SaveFolder(Folder<int> folder, IDbContextTransaction transaction)
        {
            if (folder == null) throw new ArgumentNullException("folder");

            folder.Title = Global.ReplaceInvalidCharsAndTruncate(folder.Title);

            folder.ModifiedOn = TenantUtil.DateTimeNow();
            folder.ModifiedBy = AuthContext.CurrentAccount.ID;

            if (folder.CreateOn == default) folder.CreateOn = TenantUtil.DateTimeNow();
            if (folder.CreateBy == default) folder.CreateBy = AuthContext.CurrentAccount.ID;

            var isnew = false;

            var tx = transaction ?? await FilesDbContext.Database.BeginTransactionAsync();

            if (!folder.ID.Equals(default) && await IsExist(folder.ID))
            {
                var toUpdate = await Query(FilesDbContext.Folders)
                    .Where(r => r.Id == folder.ID)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                toUpdate.Title = folder.Title;
                toUpdate.CreateBy = folder.CreateBy;
                toUpdate.ModifiedOn = TenantUtil.DateTimeToUtc(folder.ModifiedOn);
                toUpdate.ModifiedBy = folder.ModifiedBy;

                await FilesDbContext.SaveChangesAsync();

                FactoryIndexer.IndexAsync(toUpdate);
            }
            else
            {
                isnew = true;
                var newFolder = new DbFolder
                {
                    Id = 0,
                    ParentId = folder.ParentFolderID,
                    Title = folder.Title,
                    CreateOn = TenantUtil.DateTimeToUtc(folder.CreateOn),
                    CreateBy = folder.CreateBy,
                    ModifiedOn = TenantUtil.DateTimeToUtc(folder.ModifiedOn),
                    ModifiedBy = folder.ModifiedBy,
                    FolderType = folder.FolderType,
                    TenantId = TenantID
                };

                newFolder = FilesDbContext.Folders.Add(newFolder).Entity;
                await FilesDbContext.SaveChangesAsync();
                FactoryIndexer.IndexAsync(newFolder);
                folder.ID = newFolder.Id;

                //itself link
                var newTree = new DbFolderTree
                {
                    FolderId = folder.ID,
                    ParentId = folder.ID,
                    Level = 0
                };

                FilesDbContext.Tree.Add(newTree);
                await FilesDbContext.SaveChangesAsync();

                //full path to root
                var oldTree = FilesDbContext.Tree
                    .AsQueryable()
                    .Where(r => r.FolderId == (int)folder.ParentFolderID);

                foreach (var o in oldTree)
                {
                    var treeToAdd = new DbFolderTree
                    {
                        FolderId = (int)folder.ID,
                        ParentId = o.ParentId,
                        Level = o.Level + 1
                    };

                    FilesDbContext.Tree.Add(treeToAdd);
                }

                await FilesDbContext.SaveChangesAsync();
            }

            if (transaction == null)
            {
                await tx.CommitAsync();
                await tx.DisposeAsync();
            }

            if (isnew)
            {
                RecalculateFoldersCount(folder.ID);
            }

            //FactoryIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, folder));
            return folder.ID;
        }

        private Task<bool> IsExist(int folderId)
        {
            return Query(FilesDbContext.Folders).AsNoTracking()
                .Where(r => r.Id == folderId)
                .AnyAsync();
        }

        public async Task DeleteFolder(int id)
        {
            if (id == default) throw new ArgumentNullException("folderId");

            using (var tx = FilesDbContext.Database.BeginTransaction())
            {
                var subfolders = await
                    FilesDbContext.Tree
                    .AsQueryable()
                    .Where(r => r.ParentId == id)
                    .Select(r => r.FolderId)
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (!subfolders.Contains(id)) subfolders.Add(id); // chashed folder_tree

                var parent = await Query(FilesDbContext.Folders)
                    .Where(r => r.Id == id)
                    .Select(r => r.ParentId)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                var folderToDelete = Query(FilesDbContext.Folders).Where(r => subfolders.Any(a => r.Id == a));
                FilesDbContext.Folders.RemoveRange(folderToDelete);

                foreach (var f in folderToDelete)
                {
                    FactoryIndexer.DeleteAsync(f);
                }

                var treeToDelete = FilesDbContext.Tree.AsQueryable().Where(r => subfolders.Any(a => r.FolderId == a));
                FilesDbContext.Tree.RemoveRange(treeToDelete);

                var subfoldersStrings = subfolders.Select(r => r.ToString()).ToList();
                var linkToDelete = Query(FilesDbContext.TagLink)
                    .Where(r => subfoldersStrings.Any(a => r.EntryId == a))
                    .Where(r => r.EntryType == FileEntryType.Folder);
                FilesDbContext.TagLink.RemoveRange(linkToDelete);

                var tagsToRemove = Query(FilesDbContext.Tag)
                    .Where(r => !Query(FilesDbContext.TagLink).Where(a => a.TagId == r.Id).Any());

                FilesDbContext.Tag.RemoveRange(tagsToRemove);

                var securityToDelete = Query(FilesDbContext.Security)
                        .Where(r => subfoldersStrings.Any(a => r.EntryId == a))
                        .Where(r => r.EntryType == FileEntryType.Folder);

                FilesDbContext.Security.RemoveRange(securityToDelete);
                await FilesDbContext.SaveChangesAsync();

                var bunchToDelete = Query(FilesDbContext.BunchObjects)
                    .Where(r => r.LeftNode == id.ToString());

                FilesDbContext.RemoveRange(bunchToDelete);
                await FilesDbContext.SaveChangesAsync();

                await tx.CommitAsync();

                RecalculateFoldersCount(parent);
            }

            //FactoryIndexer.DeleteAsync(new FoldersWrapper { Id = id });
        }

        public async Task<TTo> MoveFolder<TTo>(int folderId, TTo toFolderId, CancellationToken? cancellationToken)
        {
            if (toFolderId is int tId)
            {
                return (TTo)Convert.ChangeType(await MoveFolder(folderId, tId, cancellationToken), typeof(TTo));
            }

            if (toFolderId is string tsId)
            {
                return (TTo)Convert.ChangeType(await MoveFolder(folderId, tsId, cancellationToken), typeof(TTo));
            }

            throw new NotImplementedException();
        }

        public async Task<int> MoveFolder(int folderId, int toFolderId, CancellationToken? cancellationToken)
        {
            using (var tx = FilesDbContext.Database.BeginTransaction())
            {
                var folder = await GetFolder(folderId);

                if (folder.FolderType != FolderType.DEFAULT)
                    throw new ArgumentException("It is forbidden to move the System folder.", "folderId");

                var recalcFolders = new List<int> { toFolderId };
                var parent = FilesDbContext.Folders
                    .AsQueryable()
                    .Where(r => r.Id == folderId)
                    .Select(r => r.ParentId)
                    .FirstOrDefault();

                if (parent != 0 && !recalcFolders.Contains(parent)) recalcFolders.Add(parent);

                var toUpdate = Query(FilesDbContext.Folders)
                    .Where(r => r.Id == folderId)
                    .FirstOrDefault();

                toUpdate.ParentId = toFolderId;
                toUpdate.ModifiedOn = DateTime.UtcNow;
                toUpdate.ModifiedBy = AuthContext.CurrentAccount.ID;

                FilesDbContext.SaveChanges();

                var subfolders = FilesDbContext.Tree
                    .AsQueryable()
                    .Where(r => r.ParentId == folderId)
                    .ToDictionary(r => r.FolderId, r => r.Level);

                var toDelete = FilesDbContext.Tree
                    .AsQueryable()
                    .Where(r => subfolders.Keys.Any(a => a == r.FolderId) && !subfolders.Keys.Any(a => a == r.ParentId));

                FilesDbContext.Tree.RemoveRange(toDelete);
                FilesDbContext.SaveChanges();

                var toInsert = FilesDbContext.Tree
                    .AsQueryable()
                    .Where(r => r.FolderId == toFolderId)
                    .ToList();

                foreach (var subfolder in subfolders)
                {
                    foreach (var f in toInsert)
                    {
                        var newTree = new DbFolderTree
                        {
                            FolderId = subfolder.Key,
                            ParentId = f.ParentId,
                            Level = f.Level + 1
                        };
                        FilesDbContext.AddOrUpdate(r => r.Tree, newTree);
                    }
                }

                FilesDbContext.SaveChanges();
                tx.Commit();

                recalcFolders.ForEach(RecalculateFoldersCount);
                recalcFolders.ForEach(fid => GetRecalculateFilesCountUpdate(fid));
            }
            return folderId;
        }

        public async Task<string> MoveFolder(int folderId, string toFolderId, CancellationToken? cancellationToken)
        {
            var toSelector = ProviderFolderDao.GetSelector(toFolderId);

            var moved = await CrossDao.PerformCrossDaoFolderCopy(
                folderId, this, DaoFactory.GetFileDao<int>(), r => r,
                toFolderId, toSelector.GetFolderDao(toFolderId), toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
                true, cancellationToken);

            return moved.ID;
        }


        public async Task<Folder<TTo>> CopyFolder<TTo>(int folderId, TTo toFolderId, CancellationToken? cancellationToken)
        {
            if (toFolderId is int tId)
            {
                return await CopyFolder(folderId, tId, cancellationToken) as Folder<TTo>;
            }

            if (toFolderId is string tsId)
            {
                return await CopyFolder(folderId, tsId, cancellationToken) as Folder<TTo>;
            }

            throw new NotImplementedException();
        }

        public async Task<Folder<int>> CopyFolder(int folderId, int toFolderId, CancellationToken? cancellationToken)
        {
            var folder = await GetFolder(folderId);

            var toFolder = await GetFolder(toFolderId);

            if (folder.FolderType == FolderType.BUNCH)
                folder.FolderType = FolderType.DEFAULT;

            var copy = ServiceProvider.GetService<Folder<int>>();
            copy.ParentFolderID = toFolderId;
            copy.RootFolderId = toFolder.RootFolderId;
            copy.RootFolderCreator = toFolder.RootFolderCreator;
            copy.RootFolderType = toFolder.RootFolderType;
            copy.Title = folder.Title;
            copy.FolderType = folder.FolderType;

            copy = await GetFolder(await SaveFolder(copy));

            //FactoryIndexer.IndexAsync(FoldersWrapper.GetFolderWrapper(ServiceProvider, copy));
            return copy;
        }

        public async Task<Folder<string>> CopyFolder(int folderId, string toFolderId, CancellationToken? cancellationToken)
        {
            var toSelector = ProviderFolderDao.GetSelector(toFolderId);

            var moved = await CrossDao.PerformCrossDaoFolderCopy(
                folderId, this, DaoFactory.GetFileDao<int>(), r => r,
                toFolderId, toSelector.GetFolderDao(toFolderId), toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
                false, cancellationToken);

            return moved;
        }

        public async Task<IDictionary<int, string>> CanMoveOrCopy<TTo>(int[] folderIds, TTo to)
        {
            if (to is int tId)
            {
                return await CanMoveOrCopy(folderIds, tId);
            }

            if (to is string tsId)
            {
                return await CanMoveOrCopy(folderIds, tsId);
            }

            throw new NotImplementedException();
        }

        public async Task<IDictionary<int, string>> CanMoveOrCopy(int[] folderIds, string to)
        {
            return await Task.FromResult(new Dictionary<int, string>());
        }

        public async Task<IDictionary<int, string>> CanMoveOrCopy(int[] folderIds, int to)
        {
            var result = new Dictionary<int, string>();

            foreach (var folderId in folderIds)
            {
                var exists = await FilesDbContext.Tree
                    .AsNoTracking()
                    .Where(r => r.ParentId == folderId)
                    .Where(r => r.FolderId == to)
                    .AnyAsync()
                    .ConfigureAwait(false);

                if (exists)
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMassage_FolderCopyError);
                }

                var title = await Query(FilesDbContext.Folders)
                    .AsNoTracking()
                    .Where(r => r.Id == folderId)
                    .Select(r => r.Title.ToLower())
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                var conflict = await Query(FilesDbContext.Folders)
                    .AsNoTracking()
                    .Where(r => r.Title.ToLower() == title)
                    .Where(r => r.ParentId == to)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (conflict != 0)
                {
                    FilesDbContext.Files
                        .AsNoTracking()
                        .Join(FilesDbContext.Files, f1 => f1.Title.ToLower(), f2 => f2.Title.ToLower(), (f1, f2) => new { f1, f2 })
                        .Where(r => r.f1.TenantId == TenantID && r.f1.CurrentVersion && r.f1.FolderId == folderId)
                        .Where(r => r.f2.TenantId == TenantID && r.f2.CurrentVersion && r.f2.FolderId == conflict)
                        .Select(r => r.f1)
                        .ToList()
                        .ForEach(r => result[r.Id] = r.Title);

                    var childs = Query(FilesDbContext.Folders)
                        .AsNoTracking()
                        .Where(r => r.ParentId == folderId)
                        .Select(r => r.Id);

                    foreach (var pair in await CanMoveOrCopy(childs.ToArray(), conflict))
                    {
                        result.Add(pair.Key, pair.Value);
                    }
                }
            }

            return result;
        }

        public async Task<int> RenameFolder(Folder<int> folder, string newTitle)
        {
            var toUpdate = await Query(FilesDbContext.Folders)
                .Where(r => r.Id == folder.ID)
                .FirstOrDefaultAsync();

            toUpdate.Title = Global.ReplaceInvalidCharsAndTruncate(newTitle);
            toUpdate.ModifiedOn = DateTime.UtcNow;
            toUpdate.ModifiedBy = AuthContext.CurrentAccount.ID;

            await FilesDbContext.SaveChangesAsync();

            FactoryIndexer.IndexAsync(toUpdate);

            return folder.ID;
        }


        public async Task<int> GetItemsCount(int folderId)
        {
            return await GetFoldersCount(folderId) +
                   await GetFilesCount(folderId);
        }

        private async Task<int> GetFoldersCount(int parentId)
        {
            var count = await FilesDbContext.Tree
                .AsNoTracking()
                .Where(r => r.ParentId == parentId)
                .Where(r => r.Level > 0)
                .CountAsync()
                .ConfigureAwait(false);

            return count;
        }

        private async Task<int> GetFilesCount(int folderId)
        {
            var count = await Query(FilesDbContext.Files)
                .AsNoTracking()
                .Distinct()
                .Where(r => FilesDbContext.Tree.AsQueryable().Where(r => r.ParentId == folderId).Select(r => r.FolderId).Any(b => b == r.FolderId))
                .CountAsync()
                .ConfigureAwait(false);

            return count;
        }

        public async Task<bool> IsEmpty(int folderId)
        {
            return await GetItemsCount(folderId) == 0;
        }

        public bool UseTrashForRemove(Folder<int> folder)
        {
            return folder.RootFolderType != FolderType.TRASH && folder.FolderType != FolderType.BUNCH;
        }

        public bool UseRecursiveOperation(int folderId, string toRootFolderId)
        {
            return true;
        }

        public bool UseRecursiveOperation(int folderId, int toRootFolderId)
        {
            return true;
        }

        public bool UseRecursiveOperation<TTo>(int folderId, TTo toRootFolderId)
        {
            return true;
        }

        public bool CanCalculateSubitems(int entryId)
        {
            return true;
        }

        public long GetMaxUploadSize(int folderId, bool chunkedUpload)
        {
            var tmp = long.MaxValue;

            if (CoreBaseSettings.Personal && SetupInfo.IsVisibleSettings("PersonalMaxSpace"))
                tmp = CoreConfiguration.PersonalMaxSpace(SettingsManager) - GlobalSpace.GetUserUsedSpace();

            return Math.Min(tmp, chunkedUpload ? SetupInfo.MaxChunkedUploadSize(TenantExtra, TenantStatisticProvider) : SetupInfo.MaxUploadSize(TenantExtra, TenantStatisticProvider));
        }

        private void RecalculateFoldersCount(int id)
        {
            var toUpdate = Query(FilesDbContext.Folders)
                .Where(r => FilesDbContext.Tree.AsQueryable().Where(a => a.FolderId == id).Select(a => a.ParentId).Any(a => a == r.Id))
                .ToList();

            foreach (var f in toUpdate)
            {
                var count = FilesDbContext.Tree.AsQueryable().Where(r => r.ParentId == id).Count() - 1;
                f.FoldersCount = count;
            }

            FilesDbContext.SaveChanges();
        }

        #region Only for TMFolderDao

        public async Task ReassignFolders(int[] folderIds, Guid newOwnerId)
        {
            var toUpdate = await Query(FilesDbContext.Folders)
                .Where(r => folderIds.Any(a => r.Id == a))
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var f in toUpdate)
            {
                f.CreateBy = newOwnerId;
            }

            await FilesDbContext.SaveChangesAsync();
        }


        public async Task<IEnumerable<Folder<int>>> Search(string text, bool bunch)
        {
            return (await Search(text)).Where(f => bunch
                                               ? f.RootFolderType == FolderType.BUNCH
                                               : (f.RootFolderType == FolderType.USER || f.RootFolderType == FolderType.COMMON)).ToList();
        }

        private async Task<IEnumerable<Folder<int>>> Search(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<Folder<int>>();

            if (FactoryIndexer.TrySelectIds(s => s.MatchAll(text), out var ids))
            {
                var q1 = GetFolderQuery(r => ids.Any(a => r.Id == a));
                return await FromQueryWithShared(q1).AsAsyncEnumerable().Select(ToFolder).ToListAsync();
            }

            var q = BuildSearch(GetFolderQuery(), text, SearhTypeEnum.Any);
            return await FromQueryWithShared(q).AsAsyncEnumerable().Select(ToFolder).ToListAsync();
        }

        public async Task<IEnumerable<int>> GetFolderIDs(string module, string bunch, IEnumerable<string> data, bool createIfNotExists)
        {
            if (string.IsNullOrEmpty(module)) throw new ArgumentNullException("module");
            if (string.IsNullOrEmpty(bunch)) throw new ArgumentNullException("bunch");

            var keys = data.Select(id => string.Format("{0}/{1}/{2}", module, bunch, id)).ToArray();

            var folderIdsDictionary = Query(FilesDbContext.BunchObjects)
                .AsNoTracking()
                .Where(r => keys.Length > 1 ? keys.Any(a => a == r.RightNode) : r.RightNode == keys[0])
                .ToDictionary(r => r.RightNode, r => r.LeftNode);

            var folderIds = new List<int>();

            foreach (var key in keys)
            {
                int newFolderId = 0;
                if (createIfNotExists && !folderIdsDictionary.TryGetValue(key, out var folderId))
                {
                    var folder = ServiceProvider.GetService<Folder<int>>();
                    switch (bunch)
                    {
                        case my:
                            folder.FolderType = FolderType.USER;
                            folder.Title = my;
                            break;
                        case common:
                            folder.FolderType = FolderType.COMMON;
                            folder.Title = common;
                            break;
                        case trash:
                            folder.FolderType = FolderType.TRASH;
                            folder.Title = trash;
                            break;
                        case share:
                            folder.FolderType = FolderType.SHARE;
                            folder.Title = share;
                            break;
                        case projects:
                            folder.FolderType = FolderType.Projects;
                            folder.Title = projects;
                            break;
                        default:
                            folder.FolderType = FolderType.BUNCH;
                            folder.Title = key;
                            break;
                    }
                    using var tx = FilesDbContext.Database.BeginTransaction();//NOTE: Maybe we shouldn't start transaction here at all

                    newFolderId = await SaveFolder(folder, tx); //Save using our db manager

                    var newBunch = new DbFilesBunchObjects
                    {
                        LeftNode = newFolderId.ToString(),
                        RightNode = key,
                        TenantId = TenantID
                    };

                    FilesDbContext.AddOrUpdate(r => r.BunchObjects, newBunch);
                    await FilesDbContext.SaveChangesAsync();

                    await tx.CommitAsync(); //Commit changes
                }
                folderIds.Add(newFolderId);
            }
            return folderIds;
        }

        public async Task<int> GetFolderID(string module, string bunch, string data, bool createIfNotExists)
        {
            if (string.IsNullOrEmpty(module)) throw new ArgumentNullException("module");
            if (string.IsNullOrEmpty(bunch)) throw new ArgumentNullException("bunch");

            var key = string.Format("{0}/{1}/{2}", module, bunch, data);
            var folderId = await Query(FilesDbContext.BunchObjects)
                .Where(r => r.RightNode == key)
                .Select(r => r.LeftNode)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (folderId != null)
            {
                return Convert.ToInt32(folderId);
            }

            var newFolderId = 0;
            if (createIfNotExists)
            {
                var folder = ServiceProvider.GetService<Folder<int>>();
                folder.ParentFolderID = 0;
                switch (bunch)
                {
                    case my:
                        folder.FolderType = FolderType.USER;
                        folder.Title = my;
                        folder.CreateBy = new Guid(data.ToString());
                        break;
                    case common:
                        folder.FolderType = FolderType.COMMON;
                        folder.Title = common;
                        break;
                    case trash:
                        folder.FolderType = FolderType.TRASH;
                        folder.Title = trash;
                        folder.CreateBy = new Guid(data.ToString());
                        break;
                    case share:
                        folder.FolderType = FolderType.SHARE;
                        folder.Title = share;
                        break;
                    case projects:
                        folder.FolderType = FolderType.Projects;
                        folder.Title = projects;
                        break;
                    default:
                        folder.FolderType = FolderType.BUNCH;
                        folder.Title = key;
                        break;
                }
                using var tx = await FilesDbContext.Database.BeginTransactionAsync(); //NOTE: Maybe we shouldn't start transaction here at all
                newFolderId = await SaveFolder(folder, tx); //Save using our db manager
                var toInsert = new DbFilesBunchObjects
                {
                    LeftNode = newFolderId.ToString(),
                    RightNode = key,
                    TenantId = TenantID
                };

                FilesDbContext.AddOrUpdate(r => r.BunchObjects, toInsert);
                await tx.CommitAsync(); //Commit changes
            }

            await FilesDbContext.SaveChangesAsync();

            return newFolderId;
        }

        async Task<int> IFolderDao<int>.GetFolderIDProjects(bool createIfNotExists)
        {
            return await (this as IFolderDao<int>).GetFolderID(FileConstant.ModuleId, projects, null, createIfNotExists);
        }

        public async Task<int> GetFolderIDTrash(bool createIfNotExists, Guid? userId = null)
        {
            return await (this as IFolderDao<int>).GetFolderID(FileConstant.ModuleId, trash, (userId ?? AuthContext.CurrentAccount.ID).ToString(), createIfNotExists);
        }

        public async Task<int> GetFolderIDCommon(bool createIfNotExists)
        {
            return await (this as IFolderDao<int>).GetFolderID(FileConstant.ModuleId, common, null, createIfNotExists);
        }

        public async Task<int> GetFolderIDUser(bool createIfNotExists, Guid? userId = null)
        {
            return await (this as IFolderDao<int>).GetFolderID(FileConstant.ModuleId, my, (userId ?? AuthContext.CurrentAccount.ID).ToString(), createIfNotExists);
        }

        public async Task<int> GetFolderIDShare(bool createIfNotExists)
        {
            return await (this as IFolderDao<int>).GetFolderID(FileConstant.ModuleId, share, null, createIfNotExists);
        }

        #endregion

        protected internal IQueryable<DbFolder> GetFolderQuery(Expression<Func<DbFolder, bool>> where = null)
        {
            var q = Query(FilesDbContext.Folders);
            if (where != null)
            {
                q = q.Where(where);
            }
            return q;
        }

        protected IQueryable<DbFolderQuery> FromQueryWithShared(IQueryable<DbFolder> dbFiles)
        {
            return dbFiles
                .Select(r => new DbFolderQuery
                {
                    folder = r,
                    root = FilesDbContext.Folders
                    .AsQueryable()
                            .Join(FilesDbContext.Tree, a => a.Id, b => b.ParentId, (folder, tree) => new { folder, tree })
                            .Where(x => x.folder.TenantId == r.TenantId)
                            .Where(x => x.tree.FolderId == r.ParentId)
                            .OrderByDescending(r => r.tree.Level)
                            .Select(r => r.folder)
                            .Take(1)
                            .FirstOrDefault(),
                    shared = FilesDbContext.Security
                    .AsQueryable()
                            .Where(r => r.EntryType == FileEntryType.Folder)
                            .Where(x => x.EntryId == r.Id.ToString())
                            .Any()
                });
        }

        protected IQueryable<DbFolderQuery> FromQuery(IQueryable<DbFolder> dbFiles)
        {
            return dbFiles
                .Select(r => new DbFolderQuery
                {
                    folder = r,
                    root = FilesDbContext.Folders
                    .AsQueryable()
                            .Join(FilesDbContext.Tree, a => a.Id, b => b.ParentId, (folder, tree) => new { folder, tree })
                            .Where(x => x.folder.TenantId == r.TenantId)
                            .Where(x => x.tree.FolderId == r.ParentId)
                            .OrderByDescending(x => x.tree.Level)
                            .Select(x => x.folder)
                            .Take(1)
                            .FirstOrDefault(),
                    shared = true
                });
        }

        public Folder<int> ToFolder(DbFolderQuery r)
        {
            if (r == null) return null;
            var result = ServiceProvider.GetService<Folder<int>>();
            result.ID = r.folder.Id;
            result.ParentFolderID = r.folder.ParentId;
            result.Title = r.folder.Title;
            result.CreateOn = TenantUtil.DateTimeFromUtc(r.folder.CreateOn);
            result.CreateBy = r.folder.CreateBy;
            result.ModifiedOn = TenantUtil.DateTimeFromUtc(r.folder.ModifiedOn);
            result.ModifiedBy = r.folder.ModifiedBy;
            result.FolderType = r.folder.FolderType;
            result.TotalSubFolders = r.folder.FoldersCount;
            result.TotalFiles = r.folder.FilesCount;
            result.RootFolderType = r.root?.FolderType ?? default;
            result.RootFolderCreator = r.root?.CreateBy ?? default;
            result.RootFolderId = r.root?.Id ?? default;
            result.Shared = r.shared;

            switch (result.FolderType)
            {
                case FolderType.COMMON:
                    result.Title = FilesUCResource.CorporateFiles;
                    break;
                case FolderType.USER:
                    result.Title = FilesUCResource.MyFiles;
                    break;
                case FolderType.SHARE:
                    result.Title = FilesUCResource.SharedForMe;
                    break;
                case FolderType.TRASH:
                    result.Title = FilesUCResource.Trash;
                    break;
                case FolderType.Projects:
                    result.Title = FilesUCResource.ProjectFiles;
                    break;
                case FolderType.BUNCH:
                    try
                    {
                        result.Title = GetProjectTitle(result.ID);
                    }
                    catch (Exception e)
                    {
                        //Global.Logger.Error(e);
                    }
                    break;
            }

            if (result.FolderType != FolderType.DEFAULT && 0.Equals(result.ParentFolderID)) result.RootFolderType = result.FolderType;
            if (result.FolderType != FolderType.DEFAULT && result.RootFolderCreator == default) result.RootFolderCreator = result.CreateBy;
            if (result.FolderType != FolderType.DEFAULT && 0.Equals(result.RootFolderId)) result.RootFolderId = result.ID;

            return result;
        }

        public async Task<string> GetBunchObjectID(int folderID)
        {
            return await Query(FilesDbContext.BunchObjects)
                .Where(r => r.LeftNode == (folderID).ToString())
                .Select(r => r.RightNode)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public async Task<Dictionary<string, string>> GetBunchObjectIDs(List<int> folderIDs)
        {
            return await Query(FilesDbContext.BunchObjects)
                .Where(r => folderIDs.Any(a => a.ToString() == r.LeftNode))
                .ToDictionaryAsync(r => r.LeftNode, r => r.RightNode)
                .ConfigureAwait(false);
        }

        private string GetProjectTitle(object folderID)
        {
            return "";
            //if (!ApiServer.Available)
            //{
            //    return string.Empty;
            //}

            //var cacheKey = "documents/folders/" + folderID.ToString();

            //var projectTitle = Convert.ToString(cache.Get<string>(cacheKey));

            //if (!string.IsNullOrEmpty(projectTitle)) return projectTitle;

            //var bunchObjectID = GetBunchObjectID(folderID);

            //if (string.IsNullOrEmpty(bunchObjectID))
            //    throw new Exception("Bunch Object id is null for " + folderID);

            //if (!bunchObjectID.StartsWith("projects/project/"))
            //    return string.Empty;

            //var bunchObjectIDParts = bunchObjectID.Split('/');

            //if (bunchObjectIDParts.Length < 3)
            //    throw new Exception("Bunch object id is not supported format");

            //var projectID = Convert.ToInt32(bunchObjectIDParts[bunchObjectIDParts.Length - 1]);

            //if (HttpContext.Current == null)
            //    return string.Empty;

            //var apiServer = new ApiServer();

            //var apiUrl = string.Format("{0}project/{1}.json?fields=id,title", SetupInfo.WebApiBaseUrl, projectID);

            //var responseApi = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(apiServer.GetApiResponse(apiUrl, "GET"))))["response"];

            //if (responseApi != null && responseApi.HasValues)
            //{
            //    projectTitle = Global.ReplaceInvalidCharsAndTruncate(responseApi["title"].Value<string>());
            //}
            //else
            //{
            //    return string.Empty;
            //}
            //if (!string.IsNullOrEmpty(projectTitle))
            //{
            //    cache.Insert(cacheKey, projectTitle, TimeSpan.FromMinutes(15));
            //}
            //return projectTitle;
        }


    }

    public class DbFolderQuery
    {
        public DbFolder folder { get; set; }
        public DbFolder root { get; set; }
        public bool shared { get; set; }
    }

    public static class FolderDaoExtention
    {
        public static DIHelper AddFolderDaoService(this DIHelper services)
        {
            services.TryAddScoped<IFolderDao<int>, FolderDao>();
            services.TryAddTransient<Folder<int>>();
            services.TryAddTransient<Folder<string>>();

            return services
                .AddFactoryIndexerService<DbFolder>()
                .AddTenantManagerService()
                .AddUserManagerService()
                .AddFilesDbContextService()
                .AddTenantUtilService()
                .AddSetupInfo()
                .AddTenantExtraService()
                .AddTenantStatisticsProviderService()
                .AddCoreBaseSettingsService()
                .AddCoreConfigurationService()
                .AddSettingsManagerService()
                .AddAuthContextService()
                .AddGlobalSpaceService();
        }
    }
}