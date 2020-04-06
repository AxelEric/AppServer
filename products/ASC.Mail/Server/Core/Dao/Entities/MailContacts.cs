﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using ASC.Core.Common.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Mail.Core.Dao.Entities
{
    [Table("mail_contacts")]
    public partial class MailContacts : BaseEntity
    {
        [Key]
        [Column("id", TypeName = "int(11) unsigned")]
        public uint Id { get; set; }
        [Required]
        [Column("id_user", TypeName = "varchar(255)")]
        public string IdUser { get; set; }
        [Column("tenant", TypeName = "int(11)")]
        public int Tenant { get; set; }
        [Column("name", TypeName = "varchar(255)")]
        public string Name { get; set; }
        [Required]
        [Column("address", TypeName = "varchar(255)")]
        public string Address { get; set; }
        [Column("description", TypeName = "varchar(100)")]
        public string Description { get; set; }
        [Column("type", TypeName = "int(11)")]
        public int Type { get; set; }
        [Column("has_photo")]
        public bool HasPhoto { get; set; }
        [Column("last_modified", TypeName = "timestamp")]
        public DateTime LastModified { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { Id };
        }
    }
}