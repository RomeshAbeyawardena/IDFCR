﻿using CF.GameEngine.Infrastructure.SqlServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.GameEngine.Infrastructure.SqlServer.EntityConfiguration;

internal class ElementEntityConfiguration : IEntityTypeConfiguration<Element>
{
    public void Configure(EntityTypeBuilder<Element> builder)
    {
        var entityName = nameof(Element);
        builder.ToTable(entityName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName($"{entityName}Id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ElementTypeId)
            .IsRequired();

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(120);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Description)
            .HasMaxLength(250);

        builder.Property(x => x.SortOrder)
            .IsRequired(false);

        builder.Property(x => x.ParentElementId)
            .IsRequired(false);

        builder.HasOne(x => x.ElementType)
            .WithMany()
            .HasForeignKey(x => x.ElementTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParentElement)
            .WithMany()
            .HasForeignKey(x => x.ParentElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
