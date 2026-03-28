using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using backend_api.Api.Models;

namespace backend_api.Api.Data;

public partial class QuantIQContext : DbContext
{
    public QuantIQContext()
    {
    }

    public QuantIQContext(DbContextOptions<QuantIQContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CorporateAction> CorporateActions { get; set; }

    public virtual DbSet<KycDocument> KycDocuments { get; set; }

    public virtual DbSet<DepositRequest> DepositRequests { get; set; }

    public virtual DbSet<Candle> Candles { get; set; }

    public virtual DbSet<CashWallet> CashWallets { get; set; }

    public virtual DbSet<MarginRatio> MarginRatios { get; set; }

    public virtual DbSet<RiskAlert> RiskAlerts { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Portfolio> Portfolios { get; set; }

    public virtual DbSet<Symbol> Symbols { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<NewsArticle> NewsArticles { get; set; }

    public virtual DbSet<StockComment> StockComments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<PriceAlert> PriceAlerts { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CorporateAction>(entity =>
        {
            entity.HasKey(e => e.ActionId).HasName("PK__CorporateActions");

            entity.ToTable("CorporateActions");

            entity.HasIndex(e => new { e.PaymentDate, e.Status }, "IX_CorporateActions_PaymentDate_Status");

            entity.Property(e => e.ActionId).HasColumnName("ActionID");
            entity.Property(e => e.Symbol)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ActionType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Ratio).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.SymbolNavigation).WithMany(p => p.CorporateActions)
                .HasForeignKey(d => d.Symbol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CorporateActions_Symbols");
        });

        modelBuilder.Entity<Candle>(entity =>
        {
            entity.HasKey(e => new { e.Symbol, e.Timestamp });

            entity.HasIndex(e => new { e.Symbol, e.Timestamp }, "IX_Candles_Symbol_Time");

            entity.Property(e => e.Symbol)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Close).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.High).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Low).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Open).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.SymbolNavigation).WithMany(p => p.Candles)
                .HasForeignKey(d => d.Symbol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Candles_Symbols");
        });

        modelBuilder.Entity<CashWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__CashWall__84D4F92E6EACE5E0");

            entity.HasIndex(e => e.UserId, "UQ__CashWall__1788CCADC377DAFB").IsUnique();

            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.AvailableBalance)
                .HasComputedColumnSql("([Balance]-[LockedAmount])", true)
                .HasColumnType("decimal(19, 4)");
            entity.Property(e => e.Balance)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LockedAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.LoanAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.CashWallet)
                .HasForeignKey<CashWallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CashWallets_Users");
        });

        modelBuilder.Entity<MarginRatio>(entity =>
        {
            entity.HasKey(e => e.RatioId).HasName("PK__MarginRa__FBB7F82CA2252F6C");

            entity.Property(e => e.RatioId).HasColumnName("RatioID");
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.ExpiredDate).HasColumnType("datetime");
            entity.Property(e => e.InitialRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MaintenanceRate).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Symbol)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.SymbolNavigation).WithMany(p => p.MarginRatios)
                .HasForeignKey(d => d.Symbol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Margin_Symbols");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFF43CAF5F");

            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.AvgMatchedPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MatchedQty).HasDefaultValue(0);
            entity.Property(e => e.OrderType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Side)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.Symbol)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.SymbolNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.Symbol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Symbols");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Symbol });

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.Symbol)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.AvailableQuantity).HasComputedColumnSql("([TotalQuantity]-[LockedQuantity])", true);
            entity.Property(e => e.AvgCostPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.LockedQuantity).HasDefaultValue(0);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TotalQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.SymbolNavigation).WithMany(p => p.Portfolios)
                .HasForeignKey(d => d.Symbol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Portfolios_Symbols");

            entity.HasOne(d => d.User).WithMany(p => p.Portfolios)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Portfolios_Users");
        });

        modelBuilder.Entity<Symbol>(entity =>
        {
            entity.HasKey(e => e.Symbol1);

            entity.Property(e => e.Symbol1)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Symbol");
            entity.Property(e => e.CompanyName).HasMaxLength(255);
            entity.Property(e => e.Exchange)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Sector).HasMaxLength(100);
            entity.Property(e => e.Pe).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MarketCap).HasColumnType("decimal(20, 2)");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransId).HasName("PK__Transact__9E5DDB1C74131A74");

            entity.Property(e => e.TransId).HasColumnName("TransID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BalanceBefore).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RefId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RefID");
            entity.Property(e => e.TransTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TransType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash).IsUnicode(false);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.KycStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("INACTIVE");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("User");
        });

        modelBuilder.Entity<KycDocument>(entity =>
        {
            entity.HasKey(e => e.KycId).HasName("PK__KycDocuments");

            entity.ToTable("KycDocuments");

            entity.HasIndex(e => e.UserId, "IX_KycDocuments_UserID");
            entity.HasIndex(e => e.Status, "IX_KycDocuments_Status");

            entity.Property(e => e.KycId).HasColumnName("KycID");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.CardNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.DateOfBirth)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Sex)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.HomeTown).HasMaxLength(500);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ExpiryDate)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CardType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ImagePath)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.RejectReason).HasMaxLength(500);
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.KycDocuments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KycDocuments_Users");
        });

        modelBuilder.Entity<DepositRequest>(entity =>
        {
            entity.HasKey(e => e.DepositId).HasName("PK__DepositRequests");

            entity.ToTable("DepositRequests");

            // OrderCode phải UNIQUE — đảm bảo idempotency
            entity.HasIndex(e => e.OrderCode, "UQ_DepositRequests_OrderCode").IsUnique();
            entity.HasIndex(e => e.UserId, "IX_DepositRequests_UserID");
            entity.HasIndex(e => e.Status, "IX_DepositRequests_Status");

            entity.Property(e => e.DepositId).HasColumnName("DepositID");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.CheckoutUrl)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaidAt).HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.DepositRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DepositRequests_Users");
        });

        modelBuilder.Entity<RiskAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__RiskAlerts");

            entity.ToTable("RiskAlerts");

            entity.HasIndex(e => e.UserId, "IX_RiskAlerts_UserID");
            entity.HasIndex(e => e.CreatedAt, "IX_RiskAlerts_CreatedAt");

            entity.Property(e => e.AlertId).HasColumnName("AlertID").ValueGeneratedOnAdd();
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UserID");
            entity.Property(e => e.AlertType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Rtt).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.RiskAlerts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RiskAlerts_Users");
        });

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.HasKey(e => e.ArticleId);
            entity.ToTable("NewsArticles");
            entity.HasIndex(e => e.Url).IsUnique();
            entity.HasIndex(e => new { e.Symbol, e.PublishedAt }).HasDatabaseName("IX_NewsArticles_Symbol_Date");

            entity.Property(e => e.Symbol).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(1000).IsUnicode(false).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Sentiment).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.SentimentScore).HasColumnType("float");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");
            entity.Property(e => e.PublishedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<StockComment>(entity =>
        {
            entity.HasKey(e => e.CommentId);
            entity.ToTable("StockComments");
            entity.HasIndex(e => e.Symbol).HasDatabaseName("IX_StockComments_Symbol");

            entity.Property(e => e.Symbol).HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockComments_Users");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.ToTable("AuditLogs");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditLogs_UserID");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
            entity.Property(e => e.UserId).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Action).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.IpAddress).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<PriceAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId);
            entity.ToTable("PriceAlerts");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_PriceAlerts_UserID");
            entity.HasIndex(e => new { e.Symbol, e.IsActive }).HasDatabaseName("IX_PriceAlerts_Symbol_Active");

            entity.Property(e => e.UserId)
                .HasMaxLength(50).IsUnicode(false).HasColumnName("UserID");
            entity.Property(e => e.Symbol)
                .HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.AlertType)
                .HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Condition)
                .HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.ThresholdValue)
                .HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TriggeredAt)
                .HasColumnType("datetime");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PriceAlerts_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
