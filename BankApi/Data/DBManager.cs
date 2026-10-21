
using BankApi.Models;
using Microsoft.EntityFrameworkCore;
namespace BankApi.Data
{

    public class DBManager : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source = BankDB.db");
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                 .HasIndex(u => u.Username)
                 .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.AccountNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasMany(u => u.Accounts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Account>()
                .HasMany(a => a.Transactions)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            //data seeding
            //fixed seed (1) so that rand gives the same random num each run
            Random rand = new Random(1);

            //20 users
            string[] names = {"Sarah", "Bob", "Ben", "Alice", "Matilda", "Grace", "John", "Clio", "Daniel", "Liam", "Steve", "Daisy", "Coco", "Faith", "Hazel", "Olivia", "Nathan", "Ashley", "Tiara", "Amy"};

            List<User> users = new List<User>();
            for (int i = 1; i <= 20; i++)
            {
                string name = names[i - 1];

                //generate a random salt per user, gives the same salt every run 
                byte[] salt = new byte[16];
                rand.NextBytes(salt);

                User user = new User();
                user.Id = i;
                user.Username = name.ToLower();
                user.Name = name;
                user.Email = name.ToLower() + "@gmail.com";
                user.Address = i + "Road, Malaysia";
                user.Phone = "+6012000000" + i.ToString();
                user.PictureURL = "ProfilePictures/default.png";
                user.PasswordHash = HashingHelper.HashPassword("Password123", salt);
                user.PasswordSalt = Convert.ToBase64String(salt);
                user.IsActive = true;
                users.Add(user);
            }
            modelBuilder.Entity<User>().HasData(users);

            //30 accounts
            //first 10 users will have 2 acc each, the rest hv one only
            List<Account> accounts = new List<Account>();
            for (int i = 1; i <= 30; i++)
            {
                Account account = new Account();
                account.Id = i;
                account.AccountNumber = "ACCOUNT" + (1000 + i);
                account.Balance = 0;
                account.IsActive = true;
                if (i <= 20)
                {
                    //acc 1-20 belong to user 1-20
                    account.UserId = i;
                }
                else
                {
                    //acc 21-30 belong to user 1-10
                    account.UserId = i - 20;
                }
                accounts.Add(account);
            }

            //500 transactions
            List<Transaction> transactions = new List<Transaction>();
            //fixed start date, so seed data is same evert run
            DateTime startDate = new DateTime(2026, 1, 1, 8, 0, 0);

            for (int i = 1; i <= 500; i++)
            {
                Transaction transaction = new Transaction();
                transaction.Id = i;
                //each transaction will be 1 hr apart
                transaction.CreatedAt = startDate.AddHours(i);

                //first 30 acc will hv a transaction each and then cincai pick one
                
                int accID;
                if (i <= 30)
                {
                    accID = i;
                }
                else
                {
                    accID = rand.Next(1,31);
                }

                Account account = accounts[accID - 1];

                transaction.AccountId = accID;
                //user owns multiple acc but still under same ID
                transaction.RelatedAccountId = accID;

                decimal amount = rand.Next(10, 10000);
                transaction.Amount = amount;

                //either deposit or withdrawal
                //(2) means 0 -> true (withdraw), 1 -> false (deposit)
                if (i > 30 && rand.Next(2) == 0 && amount <= account.Balance)
                {
                    transaction.Type = "Withdrawal";
                    account.Balance = account.Balance - amount;
                }
                else
                {
                    transaction.Type = "Deposit";
                    account.Balance = account.Balance + amount;
                }

                transactions.Add(transaction);
            }

            modelBuilder.Entity<Account>().HasData(accounts);
            modelBuilder.Entity<Transaction>().HasData(transactions);
        }
    }
}
