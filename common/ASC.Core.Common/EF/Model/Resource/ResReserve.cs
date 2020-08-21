﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.Core.Common.EF.Model.Resource
{
    [Table("res_reserve")]
    public class ResReserve
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public string Title { get; set; }
        public string CultureTitle { get; set; }
        public string TextValue { get; set; }
        public int Flag { get; set; }
    }
    public static class ResReserveExtension
    {
        public static ModelBuilder MySqlAddResReserve(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ResReserve>(entity =>
            {
                entity.HasKey(e => new { e.FileId, e.Title, e.CultureTitle })
                    .HasName("PRIMARY");

                entity.ToTable("res_reserve");

                entity.HasIndex(e => e.CultureTitle)
                    .HasName("resources_FK2");

                entity.HasIndex(e => e.Id)
                    .HasName("id")
                    .IsUnique();

                entity.Property(e => e.FileId).HasColumnName("fileid");

                entity.Property(e => e.Title)
                    .HasColumnName("title")
                    .HasColumnType("varchar(120)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.CultureTitle)
                    .HasColumnName("cultureTitle")
                    .HasColumnType("varchar(20)")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");

                entity.Property(e => e.Flag).HasColumnName("flag");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.TextValue)
                    .HasColumnName("textValue")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .HasCollation("utf8_general_ci");
            });

            return modelBuilder;
        }
        public static ModelBuilder PgSqlAddResReserve(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ResReserve>(entity =>
            {
                entity.HasKey(e => new { e.FileId, e.Title, e.CultureTitle })
                    .HasName("res_reserve_pkey");

                entity.ToTable("res_reserve", "onlyoffice");

                entity.Property(e => e.FileId).HasColumnName("fileid");

                entity.Property(e => e.Title)
                    .HasColumnName("title")
                    .HasMaxLength(120);

                entity.Property(e => e.CultureTitle)
                    .HasColumnName("cultureTitle")
                    .HasMaxLength(20);

                entity.Property(e => e.Flag).HasColumnName("flag");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.TextValue).HasColumnName("textValue");
            });

            return modelBuilder;
        }
    }
}
