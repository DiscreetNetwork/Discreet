using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Wallets.Models;

namespace Discreet.Wallets
{
    public class WalletDBContext: DbContext
    {
        public DbSet<KVPair> KVPairs { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<HistoryTx> HistoryTxs { get; set; }
        public DbSet<UTXO> UTXOs { get; set; }

        private readonly string filename;

        public WalletDBContext(string filename)
        {
            this.filename = filename;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            SqliteConnectionStringBuilder sb = new()
            {
                DataSource = filename
            };
            
            optionsBuilder.UseSqlite(sb.ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<KVPair>().ToTable(nameof(KVPair));
            modelBuilder.Entity<Account>().ToTable(nameof(Account));
            modelBuilder.Entity<HistoryTx>().ToTable(nameof(HistoryTx));
            modelBuilder.Entity<UTXO>().ToTable(nameof(UTXOs));

            /*modelBuilder.Entity<Wallet>().Ignore(p => p.CoinName);
            modelBuilder.Entity<Wallet>().Ignore(p => p.Version);
            modelBuilder.Entity<Wallet>().HasKey(p => p.Label);
            modelBuilder.Entity<Wallet>().Property(p => p.Label).HasColumnType("varchar").IsRequired().ValueGeneratedNever();
            modelBuilder.Entity<Wallet>().Property(p => p.Encrypted).HasColumnType("bit");
            modelBuilder.Entity<Wallet>().Ignore(p => p.IsEncrypted);
            modelBuilder.Entity<Wallet>()
                .Property(p => p.Timestamp)
                .HasConversion(
                    l => Discreet.Coin.Serialization.UInt64(l),
                    b => Discreet.Coin.Serialization.GetUInt64(b, 0))
                .HasColumnType("binary")
                .HasMaxLength(8);
            modelBuilder.Entity<Wallet>().Property(p => p.EncryptedEntropy).HasColumnType("varbinary");
            modelBuilder.Entity<Wallet>().Ignore(p => p.Entropy);
            modelBuilder.Entity<Wallet>().Property(p => p.EntropyLen).HasColumnType("int");
            modelBuilder.Entity<Wallet>().Property(p => p.EntropyChecksum).HasColumnType("bigint");
            //modelBuilder.Entity<Wallet>().HasMany(p => p.Accounts).WithOne(p => p.Wallet);*/

            modelBuilder.Entity<KVPair>().HasKey(p => p.Name);
            modelBuilder.Entity<KVPair>().Property(p => p.Name).HasColumnType("varchar").HasMaxLength(100).IsRequired();
            modelBuilder.Entity<KVPair>().Property(p => p.Value).HasColumnType("varbinary").IsRequired();

            modelBuilder.Entity<Account>().HasKey(p => p.Address);
            modelBuilder.Entity<Account>().Property(p => p.Address).HasColumnType("varchar").IsRequired().ValueGeneratedNever();
            modelBuilder.Entity<Account>().Property(p => p.Name).HasColumnType("varchar").HasDefaultValue(null);
            modelBuilder.Entity<Account>()
                .Property(p => p.PubKey)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes, 
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<Account>()
                .Property(p => p.PubSpendKey)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<Account>()
                .Property(p => p.PubViewKey)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<Account>().Property(p => p.Type).HasColumnType("tinyint");
            modelBuilder.Entity<Account>().Property(p => p.Deterministic).HasColumnType("bit");
            modelBuilder.Entity<Account>().Property(p => p.EncryptedSecKeyMaterial).HasColumnType("varbinary");
            modelBuilder.Entity<Account>().Ignore(p => p.Balance);
            modelBuilder.Entity<Account>().Ignore(p => p.SecKey);
            modelBuilder.Entity<Account>().Ignore(p => p.SecSpendKey);
            modelBuilder.Entity<Account>().Ignore(p => p.SecViewKey);
            modelBuilder.Entity<Account>().Ignore(p => p.Encrypted);
            modelBuilder.Entity<Account>().Ignore(p => p.SortedUTXOs);

            modelBuilder.Entity<UTXO>().HasKey(p => p.Id);
            modelBuilder.Entity<UTXO>().Property(p => p.Id).HasColumnType("integer").ValueGeneratedOnAdd();
            modelBuilder.Entity<UTXO>().Property(p => p.Type).HasColumnType("bit");
            modelBuilder.Entity<UTXO>().Property(p => p.IsCoinbase).HasColumnType("bit");
            modelBuilder.Entity<UTXO>()
                .Property(p => p.TransactionSrc)
                .HasConversion(
                    h => h.Bytes,
                    b => new Discreet.Cipher.SHA256(b, false))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<UTXO>().Property(p => p.Amount)
                .HasConversion(
                    l => Common.Serialization.UInt64(l),
                    b => Common.Serialization.GetUInt64(b, 0))
                .HasColumnType("binary")
                .HasMaxLength(8);
            modelBuilder.Entity<UTXO>().Property(p => p.Index).HasColumnType("int");
            modelBuilder.Entity<UTXO>()
                .Property(p => p.UXKey)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<UTXO>().Ignore(p => p.UXSecKey);
            modelBuilder.Entity<UTXO>()
                .Property(p => p.Commitment)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<UTXO>().Property(p => p.DecodeIndex).HasColumnType("int");
            modelBuilder.Entity<UTXO>()
                .Property(p => p.TransactionKey)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<UTXO>().Ignore(p => p.DecodedAmount);
            modelBuilder.Entity<UTXO>()
                .Property(p => p.LinkingTag)
                .HasConversion(
                    k => k == null ? null : k.Value.bytes,
                    b => b == null ? null : new Discreet.Cipher.Key(b))
                .HasColumnType("binary")
                .HasMaxLength(32);
            modelBuilder.Entity<UTXO>().Ignore(p => p.Encrypted);
            modelBuilder.Entity<UTXO>().Ignore(p => p.Account);
            //modelBuilder.Entity<UTXO>().HasOne(p => p.Account).WithMany().HasForeignKey(p => p.Address).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UTXO>().Property(p => p.Address).HasColumnType("varchar");
            modelBuilder.Entity<UTXO>().HasIndex(p => p.Address);

            //https://stackoverflow.com/questions/69621200/microsoft-data-sqlite-sqliteexception-sqlite-error-1-autoincrement-is-only-all
            modelBuilder.Entity<HistoryTx>().HasKey(p => p.Id);
            modelBuilder.Entity<HistoryTx>().Property(p => p.Id).HasColumnType("integer").ValueGeneratedOnAdd();
            modelBuilder.Entity<HistoryTx>().Property(p => p.EncryptedRawData).HasColumnType("varbinary");
            modelBuilder.Entity<HistoryTx>().Ignore(p => p.TxID);
            modelBuilder.Entity<HistoryTx>().Ignore(p => p.Timestamp);
            modelBuilder.Entity<HistoryTx>().Ignore(p => p.Inputs);
            modelBuilder.Entity<HistoryTx>().Ignore(p => p.Outputs);
            modelBuilder.Entity<HistoryTx>().Ignore(p => p.Account);
            //modelBuilder.Entity<HistoryTx>().HasOne(p => p.Account).WithMany().HasForeignKey(p => p.Address).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<HistoryTx>().Property(p => p.Address).HasColumnType("varchar");
            modelBuilder.Entity<HistoryTx>().HasIndex(p => p.Address);
        }
    }
}
