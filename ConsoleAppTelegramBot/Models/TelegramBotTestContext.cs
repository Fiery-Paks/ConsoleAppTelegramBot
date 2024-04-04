using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ConsoleAppTelegramBot.Models
{
    public partial class TelegramBotTestContext : DbContext
    {
        public TelegramBotTestContext()
        {
        }

        public TelegramBotTestContext(DbContextOptions<TelegramBotTestContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Admin> Admins { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<Wave> Waves { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=.\\SQLEXPRESS;Initial Catalog=TelegramBotTest; Integrated Security=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.Idtelegram);

                entity.Property(e => e.Idtelegram)
                    .ValueGeneratedNever()
                    .HasColumnName("IDTelegram");

                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.FullName)
                    .HasMaxLength(100)
                    .HasColumnName("FullNAme");

                entity.Property(e => e.Idtelegram).HasColumnName("IDTelegram");

                entity.Property(e => e.Image).HasColumnType("image");

                entity.Property(e => e.NubmerPc).HasColumnName("NubmerPC");

                entity.HasOne(d => d.WaveNavigation)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.Wave)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Users_Waves");
            });

            modelBuilder.Entity<Wave>(entity =>
            {
                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
