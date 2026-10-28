using Microsoft.EntityFrameworkCore;
using PetWAMS.Web.Models;

namespace PetWAMS.Web.Data
{
    public class PetWamsDbContext : DbContext
    {
        public PetWamsDbContext(
            DbContextOptions<PetWamsDbContext> options)
            : base(options)
        {
        }


        // =============================================
        // CORE SYSTEM TABLES
        // =============================================

        public DbSet<User> Users { get; set; }

        public DbSet<Shelter> Shelters { get; set; }

        public DbSet<PetCategory> PetCategories { get; set; }

        public DbSet<Pet> Pets { get; set; }

        public DbSet<AdoptionRequest> AdoptionRequests { get; set; }

        public DbSet<Adoption> Adoptions { get; set; }

        public DbSet<HealthRecord> HealthRecords { get; set; }

        public DbSet<LostPet> LostPets { get; set; }

        public DbSet<FoundPet> FoundPets { get; set; }

        public DbSet<FollowUp> FollowUps { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        // =============================================
        // ADOPTER PREFERENCE AND RECOMMENDATION
        // =============================================

        public DbSet<AdopterPreference> AdopterPreferences { get; set; }

        public DbSet<PetRecommendation> PetRecommendations { get; set; }


        // =============================================
        // ADOPTER IDENTITY AND POLICY VERIFICATION
        // =============================================

        public DbSet<AdopterVerification> AdopterVerifications { get; set; }


        // =============================================
        // PET IMAGE MANAGEMENT
        // =============================================

        public DbSet<PetImage> PetImages { get; set; }


        // =============================================
        // PET CARE SHOP
        // =============================================

        public DbSet<PetShopProduct> PetShopProducts { get; set; }

        public DbSet<PetShopProductImage> PetShopProductImages { get; set; }

        public DbSet<PetShopProductVariant> PetShopProductVariants { get; set; }

        public DbSet<PetShopCartItem> PetShopCartItems { get; set; }

        public DbSet<PetShopOrder> PetShopOrders { get; set; }

        public DbSet<PetShopOrderItem> PetShopOrderItems { get; set; }

        public DbSet<PetShopComplaint> PetShopComplaints { get; set; }

        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        public DbSet<Invoice> Invoices { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =============================================
            // SQL SERVER TRIGGER CONFIGURATION
            // =============================================

            /*
                These tables contain SQL Server triggers.

                EF Core normally uses the SQL OUTPUT clause
                when inserting or updating records. Tables
                with triggers require this configuration to
                prevent SQL Server update errors.
            */

            modelBuilder.Entity<Pet>()
                .ToTable(
                    "Pets",
                    tableBuilder =>
                        tableBuilder.HasTrigger("TR_Pets_Update")
                );

            modelBuilder.Entity<AdoptionRequest>()
                .ToTable(
                    "AdoptionRequests",
                    tableBuilder =>
                        tableBuilder.HasTrigger(
                            "TR_AdoptionRequests_Update")
                );

            modelBuilder.Entity<Adoption>()
                .ToTable(
                    "Adoptions",
                    tableBuilder =>
                        tableBuilder.HasTrigger(
                            "TR_Adoptions_Update")
                );


            // =============================================
            // DECIMAL COLUMN CONFIGURATION
            // =============================================

            modelBuilder.Entity<PetShopProduct>()
                .Property(product => product.OriginalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PetShopProduct>()
                .Property(product => product.DiscountPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PetShopOrder>()
                .Property(order => order.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PetShopOrderItem>()
                .Property(item => item.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PetShopOrderItem>()
                .Property(item => item.LineTotal)
                .HasColumnType("decimal(18,2)");


            // =============================================
            // PET IMAGE RELATIONSHIP
            // =============================================

            modelBuilder.Entity<PetImage>()
                .HasOne(image => image.Pet)
                .WithMany()
                .HasForeignKey(image => image.PetID)
                .OnDelete(DeleteBehavior.Cascade);


            // =============================================
            // PRODUCT IMAGE RELATIONSHIP
            // =============================================

            modelBuilder.Entity<PetShopProductImage>()
                .HasOne(image => image.Product)
                .WithMany()
                .HasForeignKey(image => image.ProductID)
                .OnDelete(DeleteBehavior.Cascade);


            // =============================================
            // PRODUCT VARIANT RELATIONSHIP
            // =============================================

            modelBuilder.Entity<PetShopProductVariant>()
                .HasOne(variant => variant.Product)
                .WithMany()
                .HasForeignKey(variant => variant.ProductID)
                .OnDelete(DeleteBehavior.Cascade);


            // =============================================
            // CART VARIANT RELATIONSHIP
            // =============================================

            modelBuilder.Entity<PetShopCartItem>()
                .HasOne(cartItem => cartItem.ProductVariant)
                .WithMany()
                .HasForeignKey(cartItem =>
                    cartItem.ProductVariantID)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================================
            // ORDER ITEM VARIANT RELATIONSHIP
            // =============================================

            modelBuilder.Entity<PetShopOrderItem>()
                .HasOne(orderItem =>
                    orderItem.ProductVariant)
                .WithMany()
                .HasForeignKey(orderItem =>
                    orderItem.ProductVariantID)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================================
            // ADOPTER VERIFICATION CONFIGURATION
            // =============================================

            /*
                Each adopter account can have only one
                identity verification record.
            */

            modelBuilder.Entity<AdopterVerification>()
                .HasIndex(verification =>
                    verification.UserID)
                .IsUnique();


            /*
                The NID hash must be unique.

                This prevents multiple adopter accounts
                from using the same NID number.
            */

            modelBuilder.Entity<AdopterVerification>()
                .HasIndex(verification =>
                    verification.NIDNumberHash)
                .IsUnique();


            /*
                The verification record is connected to
                the adopter's user account.

                Restrict prevents the user from being deleted
                while a verification record is still connected.
            */

            modelBuilder.Entity<AdopterVerification>()
                .HasOne(verification =>
                    verification.User)
                .WithMany()
                .HasForeignKey(verification =>
                    verification.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}