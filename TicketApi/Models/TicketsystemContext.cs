﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TicketApi;

public partial class TicketsystemContext : DbContext
{
    public TicketsystemContext()
    {
    }

    public TicketsystemContext(DbContextOptions<TicketsystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Priority> Priorities { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<Response> Responses { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ticketsystem;Username=postgres;Password=11111111");


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("job_pk");

            entity.ToTable("job", "ts");

            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(50)
                .HasColumnName("job_title");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(e => e.PriorityId).HasName("priority_pk");

            entity.ToTable("priority", "ts");

            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.PriorityName)
                .HasMaxLength(50)
                .HasColumnName("priority_name");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("requests_pk");

            entity.ToTable("requests", "ts");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.ProblemName)
                .HasMaxLength(255)
                .HasColumnName("problem_name");
            entity.Property(e => e.Reqtime)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("reqtime");
            entity.Property(e => e.ResponseId).HasColumnName("response_id");
            entity.Property(e => e.Room)
                .HasMaxLength(50)
                .HasColumnName("room");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Priority).WithMany(p => p.Requests)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("requests_priority_fk");

            entity.HasOne(d => d.Response).WithMany(p => p.Requests)
                .HasForeignKey(d => d.ResponseId)
                .HasConstraintName("requests_responses_fk");

            entity.HasOne(d => d.Status).WithMany(p => p.Requests)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("requests_status_fk");

            entity.HasOne(d => d.User).WithMany(p => p.Requests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("requests_users_fk");
        });

        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasKey(e => e.ResponseId).HasName("response_pk");

            entity.ToTable("responses", "ts");

            entity.Property(e => e.ResponseId).HasColumnName("response_id");
            entity.Property(e => e.ResponseContent)
                .HasColumnType("character varying")
                .HasColumnName("response_content");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Responses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("responses_users_fk");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pk");

            entity.ToTable("role", "ts");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("status_pk");

            entity.ToTable("status", "ts");

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(50)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pk");

            entity.ToTable("users", "ts");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.Login)
                .HasMaxLength(255)
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Refreshtoken)
                .HasColumnType("character varying")
                .HasColumnName("refreshtoken");
            entity.Property(e => e.Refreshtokenexpiretime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("refreshtokenexpiretime");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");

            entity.HasOne(d => d.Job).WithMany(p => p.Users)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_job_fk");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_fk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
