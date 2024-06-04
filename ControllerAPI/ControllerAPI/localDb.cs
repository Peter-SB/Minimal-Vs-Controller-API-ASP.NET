using Microsoft.EntityFrameworkCore;

public class localDb : DbContext
{
    public localDb(DbContextOptions<localDb> options) : base(options) { }

    public DbSet<Song> Songs => Set<Song>();
    public DbSet<Playlist> Playlists => Set<Playlist>();

}