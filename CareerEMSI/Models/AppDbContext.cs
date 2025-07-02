namespace CareerEMSI.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options):base(options){}
    
    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Recruiter> Recruiters { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<UserSkill> UserSkills { get; set; }
    public DbSet<School> Schools { get; set; } 
    public DbSet<Education> Educations { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<Connection> Connections { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostImage> PostImages { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }
    public DbSet<JobPost> JobPosts { get; set; }
    public DbSet<InternshipPost> InternshipPosts { get; set; }
    public DbSet<Application> Applications { get; set; }
    
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // one to one - user & student
        modelBuilder.Entity<User>()
            .HasOne(u => u.Student)
            .WithOne(s => s.User)
            .HasForeignKey<Student>(s => s.UserID);
        // ont to one - user & recruiter
        modelBuilder.Entity<User>()
            .HasOne(u => u.Recruiter)
            .WithOne(r => r.User)
            .HasForeignKey<Recruiter>(r => r.UserID);
        
        // Many to one - student & school
        modelBuilder.Entity<School>()
            .HasMany(s => s.Students)
            .WithOne(st => st.School)
            .HasForeignKey(st => st.SchoolId); 
        // Many to one - recruiter & company
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Recruiters)
            .WithOne(r => r.Company)
            .HasForeignKey(r => r.CompanyId);
        //Many to Many
        modelBuilder.Entity<UserSkill>()
            .HasKey(s => new { s.UserID, s.SkillID });
        
        modelBuilder.Entity<UserSkill>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSkills)
            .HasForeignKey(us => us.UserID);
        
        modelBuilder.Entity<UserSkill>()
            .HasOne(us => us.Skill)
            .WithMany(s => s.UserSkills)
            .HasForeignKey(us => us.SkillID);
        
        // Education and User and School
        modelBuilder.Entity<Education>()
            .HasOne(e => e.User)
            .WithMany(u => u.Educations)
            .HasForeignKey(e => e.UserId);
        modelBuilder.Entity<Education>()
            .HasOne(e => e.School)
            .WithMany()
            .HasForeignKey(e => e.SchoolId);
        
        // Exprience and User and Company
        modelBuilder.Entity<Experience>()
            .HasOne(e => e.User)
            .WithMany(u => u.Experiences)
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<Experience>()
            .HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId);
        
        // Connections
        modelBuilder.Entity<Connection>()
            .HasOne(c => c.Requester)
            .WithMany(u => u.SentConnections)
            .HasForeignKey(c => c.RequesterId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<Connection>()
            .HasOne(c => c.Receiver)
            .WithMany(u => u.ReceivedConnections)
            .HasForeignKey(c => c.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Post and User
        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // PostImage and Post
        modelBuilder.Entity<PostImage>()
            .HasOne(pi => pi.Post)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Like and Post/User
        modelBuilder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Comment and Post/User
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // CommentLike and Comment/User
        modelBuilder.Entity<CommentLike>()
            .HasOne(cl => cl.Comment)
            .WithMany(c => c.Likes)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<CommentLike>()
            .HasOne(cl => cl.User)
            .WithMany()
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Unique constraint for Like (User can like a post only once)
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.PostId })
            .IsUnique();
            
        // Unique constraint for CommentLike (User can like a comment only once)
        modelBuilder.Entity<CommentLike>()
            .HasIndex(cl => new { cl.UserId, cl.CommentId })
            .IsUnique();
        
        // Configure JobPost inheritance
        modelBuilder.Entity<JobPost>()
            .HasBaseType<Post>()
            .ToTable("job_posts");
            
        // Configure InternshipPost inheritance
        modelBuilder.Entity<InternshipPost>()
            .HasBaseType<Post>()
            .ToTable("internship_posts");
        
        modelBuilder.Entity<Application>()
            .HasOne(a => a.JobPost)
            .WithMany()
            .HasForeignKey(a => a.JobPostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.InternshipPost)
            .WithMany()
            .HasForeignKey(a => a.InternshipPostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}