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


using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.UserFolder;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    public class UserFolderTreeDao : BaseDao, IUserFolderTreeDao
    {
        public UserFolderTreeDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public List<UserFolderTreeItem> Get(IUserFoldersTreeExp exp)
        {
            var list = MailDb.MailUserFolderTree
                .Where(exp.GetExpression())
                .Select(ToUserFolderTreeItem)
                .ToList();

            return list;
        }

        public int Save(UserFolderTreeItem item)
        {
            var tree = new MailUserFolderTree
            {
                FolderId = item.FolderId,
                ParentId = item.ParentId,
                Level = item.Level
            };

            MailDb.MailUserFolderTree.Add(tree);

            var result = MailDb.SaveChanges();

            return result;
        }

        public int InsertFullPathToRoot(uint folderId, uint parentId)
        {
            var treeItems = MailDb.MailUserFolderTree
                .Where(t => t.FolderId == parentId);

            foreach (var t in treeItems)
            {
                t.Level += 1;
            }

            var result = MailDb.SaveChanges();

            return result;
        }

        public int Remove(IUserFoldersTreeExp exp)
        {
            var deleteQuery = MailDb.MailUserFolderTree
                .Where(exp.GetExpression());

            MailDb.MailUserFolderTree.RemoveRange(deleteQuery);

            var result = MailDb.SaveChanges();

            return result;
        }

        public void Move(uint folderId, uint toFolderId)
        {
            var exp = SimpleUserFoldersTreeExp.CreateBuilder()
                .SetParent(folderId)
                .Build();

            var subFolders = Get(exp)
                .ToDictionary(r => r.FolderId, r => r.Level);

            if (!subFolders.Any())
            {
                return;
            }

            var folderIds = subFolders.Keys;

            var deleteQuery = MailDb.MailUserFolderTree
                .Where(t => folderIds.Contains(t.FolderId) && !folderIds.Contains(t.ParentId));

            MailDb.MailUserFolderTree.RemoveRange(deleteQuery);
            MailDb.SaveChanges();

            foreach (var subFolder in subFolders)
            {
                var newTreeItems = MailDb.MailUserFolderTree
                    .Where(t => t.FolderId == toFolderId)
                    .Select(t => new MailUserFolderTree
                    {
                        FolderId = subFolder.Key,
                        ParentId = t.ParentId,
                        Level = t.Level + 1 + subFolder.Value
                    });

                foreach (var treeItem in newTreeItems)
                {
                    MailDb.AddOrUpdate(r => r.MailUserFolderTree, treeItem);
                }

            }

            MailDb.SaveChanges();
        }

        protected UserFolderTreeItem ToUserFolderTreeItem(MailUserFolderTree r)
        {
            var folder = new UserFolderTreeItem
            {
                FolderId = r.FolderId,
                ParentId = r.ParentId,
                Level = r.Level
            };

            return folder;
        }
    }

    public static class UserFolderTreeDaoExtension
    {
        public static DIHelper AddUserFolderTreeDaoService(this DIHelper services)
        {
            services.TryAddScoped<UserFolderTreeDao>();

            return services;
        }
    }
}