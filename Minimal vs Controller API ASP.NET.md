# Minimal vs Controller API [Draft]

When building a web api using [ASP.NET](http://ASP.NET) there are two main arcitectures. A the name suggests, quick and conviniant approach: Minimal API . Or a more involved approach, with a focuse on more controll and a seporation of concerns: Controller Based API. 

In this article we will walk though designing a simple crud restfull api using first the simpler approach, minimal api, then watch how as we expand the functionality of the api a controller base design is explored.

---

---

# Minimal API

A lightweight way of building HTTP web apis, this approach prioriteses a quick set u

## Simple CRUD API

We will start by building a simple crud api to interface with a song mock database.  

### Data Structure

This class will reprisent our Song object

```csharp
public class Song
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Artist { get; set; }
}
```

### In Memory Mock Database

We will use an in memory database as a mock database for storing our songs. 

```csharp
using Microsoft.EntityFrameworkCore;

public class localDb : DbContext
{
    public localDb(DbContextOptions<localDb> options): base(options) { }

    public DbSet<Song> Songs => Set<Song>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Playlist> Playlists => Set<Playlist>(); // Used later in the example
}
```

### Program.cs

The first section sets up the app using WebApplication. We also add the inmemory mock database.

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<localDb>(opt => opt.UseInMemoryDatabase("SongList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();
```

We then add our endpoints. We can quick declare an andpoint using a lambda functuon.

```csharp
app.MapGet("/songs", async (localDb db) =>
    await db.Songs.ToListAsync());
```

But for ease of readability and maintanability we can group our endpoints and pass functions: 

```csharp
var songItems = app.MapGroup("/songs"); 

songItems.MapGet("/", GetAllSongs); 
songItems.MapGet("/{id}", GetSong);
songItems.MapPost("/", SaveSong);
songItems.MapPut("/{id}", UpdateSong);
songItems.MapDelete("/{id}", DeleteSong);
```

Finaly we start the web api.

```csharp
app.Run();
```

Here our funcion definitiosn for our endpoints

```csharp
static async Task<IResult> GetAllSongs(localDb db)
{
    return TypedResults.Ok(await db.Songs.ToArrayAsync());
}

static async Task<IResult> GetSong(int id, localDb db)
{
    return await db.Songs.FindAsync(id)
        is Song song
            ? TypedResults.Ok(song)
            : TypedResults.NotFound();
}

static async Task<IResult> SaveSong(Song song, localDb db)
{
    db.Songs.Add(song);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/songs/{song.Id}", song);
}

static async Task<IResult> UpdateSong(int id, Song inputSong, localDb db)
{
    var song = await db.Songs.FindAsync(id);

    if (song is null) return TypedResults.NotFound();

    song.Name = inputSong.Name;
    song.Artist = inputSong.Artist;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteSong(int id, localDb db)
{
    if (await db.Songs.FindAsync(id) is Song song)
    {
        db.Songs.Remove(song);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
```

# Controller Based API

## Extending Our Minimal CRUD API’s Functionality

Now say we would like to add playlists and alow songs to andded. Lets add the playlist datascrtuctue and endpoints.

### Data Structure

This class will reprisent our Song object

```csharp
public class Playlist
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required List<int> Songs { get; set; } = new List<int>();
}
```

### Program.cs

The first section sets up the app using WebApplication. We also add the inmemory mock database.

```csharp
var playlistItems = app.MapGroup("/playlists");

playlistItems.MapGet("/", GetAllPlaylists);
playlistItems.MapGet("/{id}", GetPlaylist);
playlistItems.MapPost("/", SavePlaylist);
playlistItems.MapPut("/{id}", UpdatePlaylist);
playlistItems.MapPost("/{playlistId}/songs/{songId}", AddSongToPlaylist);
playlistItems.MapDelete("/{id}", DeletePlaylist);
```

Here our funcion definitiosn for our endpoints

```csharp
static async Task<IResult> GetAllPlaylists(localDb db)
{
    return TypedResults.Ok(await db.Playlists.Include(p => p.Songs).ToArrayAsync());
}

static async Task<IResult> GetPlaylist(int id, localDb db)
{
    return await db.Playlists.Include(p => p.Songs).FirstOrDefaultAsync(p => p.Id == id)
        is Playlist playlist
            ? TypedResults.Ok(playlist)
            : TypedResults.NotFound();
}

static async Task<IResult> SavePlaylist(Playlist playlist, localDb db)
{
    db.Playlists.Add(playlist);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/playlists/{playlist.Id}", playlist);
}

static async Task<IResult> UpdatePlaylist(int id, Playlist inputPlaylist, localDb db)
{
    var playlist = await db.Playlists.Include(p => p.Songs).FirstOrDefaultAsync(p => p.Id == id);

    if (playlist is null) return TypedResults.NotFound();

    playlist.Name = inputPlaylist.Name;
    playlist.Songs = inputPlaylist.Songs;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

static async Task<IResult> AddSongToPlaylist(int playlistId, int songId, localDb db)
{
    var playlist = await db.Playlists.Include(p => p.Songs).FirstOrDefaultAsync(p => p.Id == playlistId);
    if (playlist == null) return TypedResults.NotFound();

    var song = await db.Songs.FindAsync(songId);
    if (song == null) return TypedResults.NotFound();

    if (!playlist.Songs.Any(s => s == songId))
    {
        playlist.Songs.Add(song.Id);
        await db.SaveChangesAsync();
    }

    return TypedResults.NoContent();
}

static async Task<IResult> DeletePlaylist(int id, localDb db)
{
    if (await db.Playlists.FindAsync(id) is Playlist playlist)
    {
        db.Playlists.Remove(playlist);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}
```

## Upgrading To Controller Base API