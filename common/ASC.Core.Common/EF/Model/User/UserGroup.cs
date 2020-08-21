﻿using System;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace ASC.Core.Common.EF
{
    [Table("core_usergroup")]
    public class UserGroup : BaseEntity
    {
        public int Tenant { get; set; }

        public Guid UserId { get; set; }

        public Guid GroupId { get; set; }

        [Column("ref_type")]
        public UserGroupRefType RefType { get; set; }

        public bool Removed { get; set; }

        [Column("last_modified")]
        public DateTime LastModified { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { Tenant, UserId, GroupId, RefType };
        }
    }

    public static class DbUserGroupExtension
    {
        public static void MySqlAddUserGroup(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasKey(e => new { e.Tenant, e.UserId, e.GroupId, e.RefType })
                    .HasName("PRIMARY");

                entity.ToTable("core_usergroup");

                entity.HasIndex(e => e.LastModified)
                    .HasName("last_modified");

                entity.Property(e => e.Tenant).HasColumnName("tenant");

                entity.Property(e => e.UserId)
                    .HasColumnName("userid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.GroupId)
                    .HasColumnName("groupid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.RefType).HasColumnName("ref_type");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Removed).HasColumnName("removed");
            });
        }
        public static void PgSqlAddUserGroup(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasKey(e => new { e.Tenant, e.UserId, e.GroupId, e.RefType })
                    .HasName("core_usergroup_pkey");

                entity.ToTable("core_usergroup", "onlyoffice");

                entity.HasIndex(e => e.LastModified)
                    .HasName("last_modified_core_usergroup");

                entity.Property(e => e.Tenant).HasColumnName("tenant");

                entity.Property(e => e.UserId)
                    .HasColumnName("userid")
                    .HasMaxLength(38);

                entity.Property(e => e.GroupId)
                    .HasColumnName("groupid")
                    .HasMaxLength(38);

                entity.Property(e => e.RefType).HasColumnName("ref_type");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Removed).HasColumnName("removed");
            });

        }
    }
}
