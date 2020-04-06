/*
 *
 * (c) Copyright Ascensio System Limited 2010-2020
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


using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.UserFolder;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;

namespace ASC.Mail.Core.Dao
{
    public class UserFolderDao : BaseDao, IUserFolderDao
    {
        public UserFolderDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public UserFolder Get(uint id)
        {
            var userFolder = MailDb.MailUserFolder
                .Where(f => f.Tenant == Tenant && f.IdUser == UserId && f.Id == id)
                .Select(ToUserFolder)
                .SingleOrDefault();

            return userFolder;
        }

        public UserFolder GetByMail(uint mailId)
        {
            var folderId = MailDb.MailUserFolderXMail
                .Where(ufxm => ufxm.IdMail == mailId)
                .Select(ufxm => ufxm.IdFolder)
                .Distinct()
                .SingleOrDefault();

            if (folderId == 0)
                return null;

            var userFolder = MailDb.MailUserFolder
                .Where(f => f.Id == folderId)
                .Select(ToUserFolder)
                .SingleOrDefault();

            return userFolder;
        }

        public List<UserFolder> GetList(IUserFoldersExp exp)
        {
            var query = MailDb.MailUserFolder
                .Where(exp.GetExpression())
                .Select(ToUserFolder);

            if (exp.StartIndex.HasValue)
            {
                query.Skip(exp.StartIndex.Value);
            }

            if (exp.Limit.HasValue)
            {
                query.Take(exp.Limit.Value);
            }

            if (!string.IsNullOrEmpty(exp.OrderBy))
            {
                if (exp.OrderAsc != null && exp.OrderAsc.Value)
                {
                    if(exp.OrderBy == "timeModified")
                        query.OrderBy(uf => uf.TimeModified);
                    else
                        query.OrderBy(uf => uf.Name);
                }
                else
                {
                    if (exp.OrderBy == "timeModified")
                        query.OrderByDescending(uf => uf.TimeModified);
                    else
                        query.OrderByDescending(uf => uf.Name);
                }
            }

            var list = query.ToList();

            return list;
        }

        public UserFolder GetRootFolder(uint folderId)
        {
            var parentId = MailDb.MailUserFolderTree
                .Where(t => t.FolderId == folderId)
                .OrderByDescending(t => t.Level)
                .Select(t => t.ParentId)
                .Take(1)
                .SingleOrDefault();

            if (parentId == 0)
                return null;

            var userFolder = MailDb.MailUserFolder
                .Where(f => f.Id == parentId)
                .Select(ToUserFolder)
                .SingleOrDefault();

            return userFolder;
        }

        public UserFolder GetRootFolderByMailId(int mailId)
        {
            var folderId = MailDb.MailUserFolderXMail
                .Where(ufxm => ufxm.IdMail == mailId)
                .Select(ufxm => ufxm.IdFolder)
                .Distinct()
                .SingleOrDefault();

            if (folderId == 0)
                return null;

            return GetRootFolder(folderId);
        }

        public List<UserFolder> GetParentFolders(uint folderId)
        {
            var list = MailDb.MailUserFolder
                .Join(MailDb.MailUserFolderTree, uf => uf.Id, t => t.ParentId,
                (uf, t) => new
                {
                    UserFolder = uf,
                    UserFolderTree = t
                })
                .Where(o => o.UserFolderTree.FolderId == folderId)
                .OrderByDescending(o => o.UserFolderTree.Level)
                .Select(o => ToUserFolder(o.UserFolder))
                .ToList();

            return list;
        }

        public uint Save(UserFolder folder)
        {
            var mailUserFolder = new MailUserFolder
            {
                Id = folder.Id,
                ParentId = (int)folder.ParentId,
                Tenant = folder.Tenant,
                IdUser = folder.User,
                Name = folder.Name,
                FoldersCount = (uint)folder.FolderCount,
                UnreadMessagesCount = (uint)folder.UnreadCount,
                TotalMessagesCount = (uint)folder.TotalCount,
                UnreadConversationsCount = (uint)folder.UnreadChainCount,
                TotalConversationsCount = (uint)folder.TotalCount,
                ModifiedOn = folder.TimeModified
            };

            var entry = MailDb.MailUserFolder.Add(mailUserFolder);

            MailDb.SaveChanges();

            return entry.Entity.Id;
        }

        public int Remove(uint id)
        {
            var mailUserFolder = new MailUserFolder
            {
                Id = id,
                Tenant = Tenant,
                IdUser = UserId,
            };

            MailDb.MailUserFolder.Remove(mailUserFolder);

            var count = MailDb.SaveChanges();

            return count;
        }

        public int Remove(IUserFoldersExp exp)
        {
            var deleteQuery = MailDb.MailUserFolder.Where(exp.GetExpression());

            MailDb.MailUserFolder.RemoveRange(deleteQuery);

            var count = MailDb.SaveChanges();

            return count;
        }

        public void RecalculateFoldersCount(uint id)
        {
            var toUpdate = MailDb.MailUserFolder
                .Where(uf => MailDb.MailUserFolderTree
                    .Where(t=> t.FolderId == (int)id)
                    .Select(t => t.ParentId)
                    .Any(pId => pId == uf.Id)
                )
                .ToList();

            foreach (var f in toUpdate)
            {
                var count = MailDb.MailUserFolderTree
                    .Where(r => r.ParentId == f.Id)
                    .Count() - 1;

                f.FoldersCount = (uint)count;
            }

            var result = MailDb.SaveChanges();
        }

        public int SetFolderCounters(uint folderId, int? unreadMess = null, int? totalMess = null,
            int? unreadConv = null, int? totalConv = null)
        {
            if (!unreadMess.HasValue
                && !totalMess.HasValue
                && !unreadConv.HasValue
                && !totalConv.HasValue)
            {
                return -1;
            }

            var userFolder = MailDb.MailUserFolder
                .Where(uf => uf.Tenant == Tenant && uf.IdUser == UserId && uf.Id == folderId)
                .SingleOrDefault();

            if (userFolder == null)
                return -1;

            if (unreadMess.HasValue)
                userFolder.UnreadMessagesCount = (uint)unreadMess.Value;

            if (totalMess.HasValue)
                userFolder.TotalMessagesCount = (uint)totalMess.Value;

            if (unreadConv.HasValue)
                userFolder.UnreadConversationsCount = (uint)unreadConv.Value;

            if (totalConv.HasValue)
                userFolder.TotalConversationsCount = (uint)totalConv.Value;

            var result = MailDb.SaveChanges();

            return result;
        }

        public int ChangeFolderCounters(uint folderId, int? unreadMessDiff = null, int? totalMessDiff = null,
            int? unreadConvDiff = null, int? totalConvDiff = null)
        {
            if (!unreadMessDiff.HasValue
                && !totalMessDiff.HasValue
                && !unreadConvDiff.HasValue
                && !totalConvDiff.HasValue)
            {
                return -1;
            }

            var userFolder = MailDb.MailUserFolder
                .Where(uf => uf.Tenant == Tenant && uf.IdUser == UserId && uf.Id == folderId)
                .SingleOrDefault();

            if (userFolder == null)
                return -1;

            if (unreadMessDiff.HasValue)
            {
                if (unreadMessDiff.Value == 0)
                    userFolder.UnreadMessagesCount = (uint)unreadMessDiff.Value;
                else
                    userFolder.UnreadMessagesCount += (uint)unreadMessDiff.Value;
            }

            if (totalMessDiff.HasValue)
            {
                if (totalMessDiff.Value == 0)
                    userFolder.TotalMessagesCount = (uint)totalMessDiff.Value;
                else
                    userFolder.TotalMessagesCount += (uint)totalMessDiff.Value;
            }

            if (unreadConvDiff.HasValue)
            {
                if (unreadConvDiff.Value == 0)
                    userFolder.UnreadConversationsCount = (uint)unreadConvDiff.Value;
                else
                    userFolder.UnreadConversationsCount += (uint)unreadConvDiff.Value;
            }

            if (totalConvDiff.HasValue)
            {
                if (totalConvDiff.Value == 0)
                    userFolder.TotalConversationsCount = (uint)totalConvDiff.Value;
                else
                    userFolder.TotalConversationsCount += (uint)totalConvDiff.Value;
            }

            var result = MailDb.SaveChanges();

            return result;
        }

        protected UserFolder ToUserFolder(MailUserFolder r)
        {
            var folder = new UserFolder
            {
                Id = r.Id,
                ParentId = (uint)r.ParentId,
                
                Tenant = r.Tenant,
                User = r.IdUser,
                
                Name = r.Name,
                FolderCount = (int)r.FoldersCount,

                UnreadCount = (int)r.UnreadMessagesCount,
                TotalCount = (int)r.TotalMessagesCount,

                UnreadChainCount = (int)r.UnreadConversationsCount,
                TotalChainCount = (int)r.TotalConversationsCount,

                TimeModified = r.ModifiedOn
            };

            return folder;
        }
    }

    public static class UserFolderDaoExtension
    {
        public static DIHelper AddUserFolderDaoService(this DIHelper services)
        {
            services.TryAddScoped<UserFolderDao>();

            return services;
        }
    }
}