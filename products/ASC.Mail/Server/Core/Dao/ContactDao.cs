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


//using System.Collections.Generic;
//using ASC.Common.Data;
//using ASC.Common.Data.Sql;
//using ASC.Common.Data.Sql.Expressions;
//using ASC.Mail.Core.Dao.Interfaces;
//using ASC.Mail.Core.DbSchema;
//using ASC.Mail.Core.DbSchema.Interfaces;
//using ASC.Mail.Core.DbSchema.Tables;
//using ASC.Mail.Core.Entities;

namespace ASC.Mail.Core.Dao
{
    /*public class ContactDao : BaseDao, IContactDao
    {
        protected static ITable table = new MailTableFactory().Create<ContactsTable>();

        protected string CurrentUserId { get; private set; }

        public ContactDao(IDbManager dbManager, int tenant, string user)
            : base(table, dbManager, tenant)
        {
            CurrentUserId = user;
        }

        public int SaveContact(Contact contact)
        {
            var queryContact = new SqlInsert(ContactsTable.TABLE_NAME, true)
                .InColumnValue(ContactsTable.Columns.Id, contact.Id)
                .InColumnValue(ContactsTable.Columns.User, contact.User)
                .InColumnValue(ContactsTable.Columns.Tenant, contact.Tenant)
                .InColumnValue(ContactsTable.Columns.ContactName, contact.ContactName)
                .InColumnValue(ContactsTable.Columns.Address, contact.Address)
                .InColumnValue(ContactsTable.Columns.Description, contact.Description)
                .InColumnValue(ContactsTable.Columns.Type, contact.Type)
                .InColumnValue(ContactsTable.Columns.HasPhoto, contact.HasPhoto)
                .Identity(0, 0, true);

            return Db.ExecuteScalar<int>(queryContact);
        }

        public int RemoveContacts(List<int> ids)
        {
            var query = new SqlDelete(ContactInfoTable.TABLE_NAME)
                .Where(Exp.In(ContactInfoTable.Columns.ContactId, ids))
                .Where(ContactInfoTable.Columns.Tenant, Tenant)
                .Where(ContactInfoTable.Columns.User, CurrentUserId);

            return Db.ExecuteNonQuery(query);
        }
    }*/
}