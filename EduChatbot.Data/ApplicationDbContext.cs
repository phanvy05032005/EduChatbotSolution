using EduChatbot.Models.Identity;
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

    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

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

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("chat_conversations");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.UserId).HasColumnName("user_id").IsRequired().HasMaxLength(450);
            entity.Property(c => c.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(c => c.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Id).HasColumnName("id");
            entity.Property(m => m.ConversationId).HasColumnName("conversation_id");
            entity.Property(m => m.Role).HasColumnName("role").IsRequired().HasMaxLength(10);
            entity.Property(m => m.Content).HasColumnName("content").IsRequired();
            entity.Property(m => m.SourceChunks).HasColumnName("source_chunks");
            entity.Property(m => m.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(m => m.ConversationId);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(100);
        });
    }
}
