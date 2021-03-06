﻿// <auto-generated />
using System;
using ASC.Core.Common.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASC.Core.Common.Migrations.MySql.DbContextMySql
{
    [DbContext(typeof(MySqlDbContext))]
    [Migration("20201006100150_DbContextMySql")]
    partial class DbContextMySql
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ASC.Core.Common.EF.Model.DbipLocation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int");

                    b.Property<string>("AddrType")
                        .IsRequired()
                        .HasColumnName("addr_type")
                        .HasColumnType("enum('ipv4','ipv6')")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasColumnName("city")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnName("country")
                        .HasColumnType("varchar(2)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<string>("District")
                        .HasColumnName("district")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<int>("GeonameId")
                        .HasColumnName("geoname_id")
                        .HasColumnType("int");

                    b.Property<string>("IPEnd")
                        .IsRequired()
                        .HasColumnName("ip_end")
                        .HasColumnType("varchar(39)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<string>("IPStart")
                        .IsRequired()
                        .HasColumnName("ip_start")
                        .HasColumnType("varchar(39)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<long>("Latitude")
                        .HasColumnName("latitude")
                        .HasColumnType("bigint");

                    b.Property<long>("Longitude")
                        .HasColumnName("longitude")
                        .HasColumnType("bigint");

                    b.Property<int>("Processed")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("processed")
                        .HasColumnType("int")
                        .HasDefaultValueSql("'1'");

                    b.Property<string>("StateProv")
                        .IsRequired()
                        .HasColumnName("stateprov")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<string>("TimezoneName")
                        .HasColumnName("timezone_name")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<double>("TimezoneOffset")
                        .HasColumnName("timezone_offset")
                        .HasColumnType("double");

                    b.Property<string>("ZipCode")
                        .HasColumnName("zipcode")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.HasKey("Id");

                    b.HasIndex("IPStart")
                        .HasName("ip_start");

                    b.ToTable("dbip_location");
                });

            modelBuilder.Entity("ASC.Core.Common.EF.Model.MobileAppInstall", b =>
                {
                    b.Property<string>("UserEmail")
                        .HasColumnName("user_email")
                        .HasColumnType("varchar(255)")
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("MySql:Collation", "utf8_general_ci");

                    b.Property<int>("AppType")
                        .HasColumnName("app_type")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastSign")
                        .HasColumnName("last_sign")
                        .HasColumnType("datetime");

                    b.Property<DateTime>("RegisteredOn")
                        .HasColumnName("registered_on")
                        .HasColumnType("datetime");

                    b.HasKey("UserEmail", "AppType")
                        .HasName("PRIMARY");

                    b.ToTable("mobile_app_install");
                });

            modelBuilder.Entity("ASC.Core.Common.EF.Model.Regions", b =>
                {
                    b.Property<string>("Region")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<string>("ConnectionString")
                        .HasColumnName("connection_string")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Provider")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Region");

                    b.ToTable("regions");
                });
#pragma warning restore 612, 618
        }
    }
}
