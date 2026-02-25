using Microsoft.EntityFrameworkCore;
using Shared.Infra.Module.EntityFramework.Conventions;
using ToDoSystem.Core.Entities;

namespace ToDoSystem.Infra.Repositories.EfCore;

/// <summary>
/// DbContext for ToDo list items.
/// </summary>
public class DefaultDbContext : DbContext
{
    public DefaultDbContext(DbContextOptions<DefaultDbContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todo_items");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasComment("Identificador único do item")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Título/tarefa do item");

            entity.Property(e => e.Teste)
                .HasComment("Campo de teste para demonstração");

            entity.Property(e => e.Teste2);

            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasComment("Descrição opcional do item");

            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasComment("Indica se o item está concluído");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasComment("Data e hora de criação do item");

            entity.Property(e => e.UpdatedAt)
                .HasComment("Data e hora da última atualização do item");
        });
    }
}
