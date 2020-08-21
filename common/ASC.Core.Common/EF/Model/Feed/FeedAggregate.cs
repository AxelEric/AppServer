﻿using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Core.Common.EF.Model
{
    [Table("feed_aggregate")]
    public class FeedAggregate : BaseEntity
    {
        public string Id { get; set; }
        public int Tenant { get; set; }
        public string Product { get; set; }
        public string Module { get; set; }
        public Guid Author { get; set; }

        [Column("modified_by")]
        public Guid ModifiedBy { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [Column("modified_date")]
        public DateTime ModifiedDate { get; set; }

        [Column("group_id")]
        public string GroupId { get; set; }

        [Column("aggregated_date")]
        public DateTime AggregateDate { get; set; }
        public string Json { get; set; }
        public string Keywords { get; set; }

        public override object[] GetKeys() => new object[] { Id };
    }
    public static class FeedAggregateExtension
    {
        public static ModelBuilder MySqlAddFeedAggregate(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FeedAggregate>(entity =>
            {
                entity.ToTable("feed_aggregate");

                entity.HasIndex(e => new { e.Tenant, e.AggregateDate })
                    .HasName("aggregated_date");

                entity.HasIndex(e => new { e.Tenant, e.ModifiedDate })
                    .HasName("modified_date");

                entity.HasIndex(e => new { e.Tenant, e.Product })
                    .HasName("product");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("varchar(88)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.AggregateDate)
                    .HasColumnName("aggregated_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnName("author")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("created_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.GroupId)
                    .HasColumnName("group_id")
                    .HasColumnType("varchar(70)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Json)
                    .IsRequired()
                    .HasColumnName("json")
                    .HasColumnType("mediumtext")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Keywords)
                    .HasColumnName("keywords")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnName("modified_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Module)
                    .IsRequired()
                    .HasColumnName("module")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Product)
                    .IsRequired()
                    .HasColumnName("product")
                    .HasColumnType("varchar(50)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Tenant).HasColumnName("tenant");
            });

            return modelBuilder;
        }
        public static ModelBuilder PgSqlAddFeedAggregate(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FeedAggregate>(entity =>
            {
                entity.ToTable("feed_aggregate", "onlyoffice");

                entity.HasIndex(e => new { e.Tenant, e.AggregateDate })
                    .HasName("aggregated_date");

                entity.HasIndex(e => new { e.Tenant, e.ModifiedDate })
                    .HasName("modified_date");

                entity.HasIndex(e => new { e.Tenant, e.Product })
                    .HasName("product");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasMaxLength(88);

                entity.Property(e => e.AggregateDate).HasColumnName("aggregated_date");

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasColumnName("author")
                    .HasMaxLength(38)
                    .IsFixedLength();

                entity.Property(e => e.CreatedDate).HasColumnName("created_date");

                entity.Property(e => e.GroupId)
                    .HasColumnName("group_id")
                    .HasMaxLength(70)
                    .HasDefaultValueSql("NULL::character varying");

                entity.Property(e => e.Json)
                    .IsRequired()
                    .HasColumnName("json");

                entity.Property(e => e.Keywords).HasColumnName("keywords");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasMaxLength(38)
                    .IsFixedLength();

                entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");

                entity.Property(e => e.Module)
                    .IsRequired()
                    .HasColumnName("module")
                    .HasMaxLength(50);

                entity.Property(e => e.Product)
                    .IsRequired()
                    .HasColumnName("product")
                    .HasMaxLength(50);

                entity.Property(e => e.Tenant).HasColumnName("tenant");
            });

            return modelBuilder;
        }
    }
}
