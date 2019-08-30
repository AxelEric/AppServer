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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ASC.Common.Logging;
using ASC.Common.Threading.Progress;
using ASC.Core;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Data.Storage;
//using ASC.Mail.Core.Engine;
using ASC.MessagingSystem;
//using ASC.Web.CRM.Core;
using ASC.Web.Core;
//using ASC.Web.Files.Services.WCFService;
using ASC.Web.Studio.Core.Notify;
//using CrmDaoFactory = ASC.CRM.Core.Dao.DaoFactory;
using Microsoft.AspNetCore.Http;

namespace ASC.Data.Reassigns
{
    public class RemoveProgressItem : IProgressItem
    {
        private readonly HttpContext _context;
        private readonly Dictionary<string, string> _httpHeaders;

        private readonly int _tenantId;
        private readonly string _userName;
        private readonly Guid _currentUserId;
        private readonly bool _notify;

        //private readonly IFileStorageService _docService;
        //private readonly MailGarbageEngine _mailEraser;

        public object Id { get; set; }
        public object Status { get; set; }
        public object Error { get; set; }
        public double Percentage { get; set; }
        public bool IsCompleted { get; set; }
        public Guid FromUser { get; }
        public UserInfo User { get; }

        public MessageService MessageService { get; }
        public StudioNotifyService StudioNotifyService { get; }

        public RemoveProgressItem(HttpContext context, MessageService messageService, QueueWorkerRemove queueWorkerRemove, StudioNotifyService studioNotifyService, int tenantId, UserInfo user, Guid currentUserId, bool notify)
        {
            _context = context;
            MessageService = messageService;
            StudioNotifyService = studioNotifyService;
            _httpHeaders = QueueWorker.GetHttpHeaders(context.Request);

            _tenantId = tenantId;
            User = user;
            FromUser = user.ID;
            _userName = UserFormatter.GetUserName(user, DisplayUserNameFormat.Default);
            _currentUserId = currentUserId;
            _notify = notify;

            //_docService = Web.Files.Classes.Global.FileStorageService;
            //_mailEraser = new MailGarbageEngine();

            Id = queueWorkerRemove.GetProgressItemId(tenantId, FromUser);
            Status = ProgressStatus.Queued;
            Error = null;
            Percentage = 0;
            IsCompleted = false;
        }

        public void RunJob()
        {
            var logger = LogManager.GetLogger("ASC.Web");

            try
            {
                Percentage = 0;
                Status = ProgressStatus.Started;

                var tenant = CoreContext.TenantManager.SetCurrentTenant(_tenantId);
                SecurityContext.AuthenticateMe(_tenantId, _currentUserId);

                long crmSpace;
                GetUsageSpace(tenant, out var docsSpace, out var mailSpace, out var talkSpace);

                logger.InfoFormat("deleting user data for {0} ", FromUser);

                logger.Info("deleting of data from documents");

                Percentage = 25;
                //_docService.DeleteStorage(_userId);

                if (!CoreContext.Configuration.CustomMode)
                {
                    logger.Info("deleting of data from crm");

                    Percentage = 50;
                    //using (var scope = DIHelper.Resolve(_tenantId))
                    //{
                    //    var crmDaoFactory = scope.Resolve<CrmDaoFactory>();
                    crmSpace = 0;// crmDaoFactory.ReportDao.GetFiles(_userId).Sum(file => file.ContentLength);
                    //    crmDaoFactory.ReportDao.DeleteFiles(_userId);
                    //}
                }
                else
                {
                    crmSpace = 0;
                }

                logger.Info("deleting of data from mail");

                Percentage = 75;
                //_mailEraser.ClearUserMail(_userId);

                logger.Info("deleting of data from talk");

                Percentage = 99;
                DeleteTalkStorage();

                SendSuccessNotify(docsSpace, crmSpace, mailSpace, talkSpace);

                Percentage = 100;
                Status = ProgressStatus.Done;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Status = ProgressStatus.Failed;
                Error = ex.Message;
                SendErrorNotify(ex.Message);
            }
            finally
            {
                logger.Info("data deletion is complete");
                IsCompleted = true;
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        private void GetUsageSpace(Tenant tenant, out long docsSpace, out long mailSpace, out long talkSpace)
        {
            docsSpace = mailSpace = talkSpace = 0;

            var webItems = WebItemManager.Instance.GetItems(tenant, Web.Core.WebZones.WebZoneType.All, ItemAvailableState.All);

            foreach (var item in webItems)
            {
                IUserSpaceUsage manager;

                if (item.ID == WebItemManager.DocumentsProductID)
                {
                    manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                    if (manager == null) continue;
                    docsSpace = manager.GetUserSpaceUsage(FromUser);
                }

                if (item.ID == WebItemManager.MailProductID)
                {
                    manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                    if (manager == null) continue;
                    mailSpace = manager.GetUserSpaceUsage(FromUser);
                }

                if (item.ID == WebItemManager.TalkProductID)
                {
                    manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                    if (manager == null) continue;
                    talkSpace = manager.GetUserSpaceUsage(FromUser);
                }
            }
        }

        private void DeleteTalkStorage()
        {
            using var md5 = MD5.Create();
            var data = md5.ComputeHash(Encoding.Default.GetBytes(FromUser.ToString()));

            var sBuilder = new StringBuilder();

            for (int i = 0, n = data.Length; i < n; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            var md5Hash = sBuilder.ToString();

            var storage = StorageFactory.GetStorage(_tenantId.ToString(CultureInfo.InvariantCulture), "talk");

            if (storage != null && storage.IsDirectory(md5Hash))
            {
                storage.DeleteDirectory(md5Hash);
            }
        }

        private void SendSuccessNotify(long docsSpace, long crmSpace, long mailSpace, long talkSpace)
        {
            if (_notify)
                StudioNotifyService.SendMsgRemoveUserDataCompleted(_tenantId, _currentUserId, User, _userName,
                                                                            docsSpace, crmSpace, mailSpace, talkSpace);

            if (_httpHeaders != null)
                MessageService.Send(_httpHeaders, MessageAction.UserDataRemoving, MessageTarget.Create(FromUser), new[] { _userName });
            else
                MessageService.Send(MessageAction.UserDataRemoving, MessageTarget.Create(FromUser), _userName);
        }

        private void SendErrorNotify(string errorMessage)
        {
            if (!_notify) return;

            StudioNotifyService.SendMsgRemoveUserDataFailed(_tenantId, _currentUserId, User, _userName, errorMessage);
        }
    }
}
