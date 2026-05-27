using EduChatbot.Data.Identity;
using EduChatbot.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduChatbot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(document => document.Id);

            // Dùng tên bảng/cột lowercase để thao tác PostgreSQL trong DBeaver dễ hơn.
            entity.Property(document => document.Id).HasColumnName("id");
            entity.Property(document => document.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(255);
            entity.Property(document => document.StoredFileName).HasColumnName("stored_file_name").IsRequired().HasMaxLength(255);
            entity.Property(document => document.FilePath).HasColumnName("file_path").IsRequired().HasMaxLength(500);
            entity.Property(document => document.UploadedBy).HasColumnName("uploaded_by").IsRequired().HasMaxLength(100);
            entity.Property(document => document.UploadedById).HasColumnName("uploaded_by_id").HasMaxLength(450);
            entity.Property(document => document.ContentType).HasColumnName("content_type").IsRequired().HasMaxLength(100);
            entity.Property(document => document.FileSize).HasColumnName("file_size");
            entity.Property(document => document.ExtractedText).HasColumnName("extracted_text");
            entity.Property(document => document.ChunkCount).HasColumnName("chunk_count");
            entity.Property(document => document.EmbeddingPreview).HasColumnName("embedding_preview").HasMaxLength(500);
            entity.Property(document => document.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
            entity.Property(document => document.UploadedAt)
                .HasColumnName("uploaded_at")
                .HasColumnType("timestamp with time zone");

            entity.HasMany(document => document.Chunks)
                .WithOne(chunk => chunk.Document)
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(chunk => chunk.Id);

            // Bảng chunk lưu text nhỏ và embedding mock cho workflow RAG Assignment 1.
            entity.Property(chunk => chunk.Id).HasColumnName("id");
            entity.Property(chunk => chunk.DocumentId).HasColumnName("document_id");
            entity.Property(chunk => chunk.ChunkIndex).HasColumnName("chunk_index");
            entity.Property(chunk => chunk.Content).HasColumnName("content").IsRequired();
            entity.Property(chunk => chunk.EmbeddingData).HasColumnName("embedding_data").IsRequired().HasMaxLength(1000);
            entity.Property(chunk => chunk.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex })
                .IsUnique();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(100);
        });
    }
}
