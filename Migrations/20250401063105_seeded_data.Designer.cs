﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebApplication1.Data;

#nullable disable

namespace WebApplication1.Migrations
{
    [DbContext(typeof(Web1Context))]
    [Migration("20250401063105_seeded_data")]
    partial class seeded_data
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("WebApplication1.Models.Items", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Price")
                        .HasColumnType("float");

                    b.Property<int?>("SerialNumberId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Items");

                    b.HasData(
                        new
                        {
                            Id = 7,
                            Name = "Hehe Product",
                            Price = 200.0,
                            SerialNumberId = 3
                        });
                });

            modelBuilder.Entity("WebApplication1.Models.SerialNumber", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("ItemId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ItemId")
                        .IsUnique()
                        .HasFilter("[ItemId] IS NOT NULL");

                    b.ToTable("SerialNumbers");

                    b.HasData(
                        new
                        {
                            Id = 3,
                            ItemId = 7,
                            Name = "heh710"
                        });
                });

            modelBuilder.Entity("WebApplication1.Models.SerialNumber", b =>
                {
                    b.HasOne("WebApplication1.Models.Items", "Item")
                        .WithOne("SerailNumber")
                        .HasForeignKey("WebApplication1.Models.SerialNumber", "ItemId");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("WebApplication1.Models.Items", b =>
                {
                    b.Navigation("SerailNumber");
                });
#pragma warning restore 612, 618
        }
    }
}
