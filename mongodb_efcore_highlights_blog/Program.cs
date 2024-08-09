using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using System.ComponentModel.DataAnnotations;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

if (connectionString == null)
{
    Console.WriteLine("You must set your 'MONGODB_URI' environment variable. To learn how to set it, see https://www.mongodb.com/docs/drivers/csharp/current/quick-start/#set-your-connection-string");
    Environment.Exit(0);
}

var client = new MongoClient(connectionString);
var database = client.GetDatabase("sample_mflix");
var db = MflixDbContext.Create(database);

var movie = db.Movies.First(m => m.title == "Back to the Future");
Console.WriteLine(movie.plot);

movie.adapted_from_book = false;
await db.SaveChangesAsync();


var moviesCollection = client.GetDatabase("sample_mflix").GetCollection<Movie>("movies");
Console.WriteLine("Before creating a new Index:");
PrintIndexes();

var moviesIndex = new CreateIndexModel<Movie>(Builders<Movie>.IndexKeys
    .Ascending(m => m.title)
    .Ascending(x => x.rated));
await moviesCollection.Indexes.CreateOneAsync(moviesIndex);

Console.WriteLine("After creating a new Index:");
PrintIndexes();

void PrintIndexes()
{
    var indexes = moviesCollection.Indexes.List();
    foreach (var index in indexes.ToList())
    {
        Console.WriteLine(index);
    }
}


//LINQ to find all PG-13 movies sorted by title and containing the work "shark" in their plot
var myMovies = db.Movies.
    Where(m => m.rated == "PG-13" && m.plot.Contains("shark")).
    OrderBy(m => m.title);

foreach (var m in myMovies)
{
    Console.WriteLine(m.title);
}


Movie myMovie1= new Movie {
    title = "The Rise of EF Core 1",
    plot = "Entity Framework (EF) Core is a lightweight, extensible, open source and cross-platform version of the popular Entity Framework data access technology.",
    rated = "G"
};

db.Movies.Add(myMovie1);
await db.SaveChangesAsync();

var dbContext2 = MflixDbContext.Create(database);
dbContext2.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
var myMovie2 = new Movie { title = "The Rise of EF Core 2" };
dbContext2.Movies.Add(myMovie2);

var myMovie3 = new Movie { title = "The Rise of EF Core 3" };
dbContext2.Movies.Add(myMovie3);
await dbContext2.SaveChangesAsync();


internal class MflixDbContext : DbContext
{
    public DbSet<Movie> Movies { get; init; }

    public static MflixDbContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<MflixDbContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    public MflixDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>().ToCollection("movies");
    }
}

internal class Movie
{
    public ObjectId _id { get; set; }
    public string title { get; set; }
    public string rated { get; set; }
    public string plot { get; set; }
    public bool? adapted_from_book { get; set; }

    [Timestamp]
    public long Version { get; set; }
}
