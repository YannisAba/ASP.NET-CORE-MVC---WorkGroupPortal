using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WorkGroupPortal.Models;

public partial class LabDBContext : DbContext
{
    public LabDBContext()
    {
    }

    public LabDBContext(DbContextOptions<LabDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupInvitation> GroupInvitations { get; set; }

    public virtual DbSet<GroupUser> GroupUsers { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=IOANNIS_DELL\\SQLEXPRESS;Database=WorkGroupPortal_db;Trusted_Connection=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Contacts__3214EC07BD17EE77");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.ContactNavigation).WithMany(p => p.ContactContactNavigations).HasConstraintName("FK__Contacts__Contac__7C4F7684");

            entity.HasOne(d => d.User).WithMany(p => p.ContactUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contacts__UserId__7B5B524B");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Groups__3214EC07DA9508F6");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.Groups).HasConstraintName("FK__Groups__CreatedB__3C69FB99");
        });

        modelBuilder.Entity<GroupInvitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GroupInv__3214EC078760BB1F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupInvitations).HasConstraintName("FK__GroupInvi__Group__59FA5E80");

            entity.HasOne(d => d.Receiver).WithMany(p => p.GroupInvitationReceivers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupInvi__Recei__5BE2A6F2");

            entity.HasOne(d => d.Sender).WithMany(p => p.GroupInvitationSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupInvi__Sende__5AEE82B9");
        });

        modelBuilder.Entity<GroupUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GroupUse__3214EC070A652243");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupUsers).HasConstraintName("FK__GroupUser__Group__4E88ABD4");

            entity.HasOne(d => d.User).WithMany(p => p.GroupUsers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupUser__UserI__4F7CD00D");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC0793141BCE");

            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Group).WithMany(p => p.Messages).HasConstraintName("FK__Messages__GroupI__6477ECF3");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Sender__656C112C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07EFF0E533");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
