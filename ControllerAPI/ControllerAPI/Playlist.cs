public class Playlist
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required List<int> Songs { get; set; } = new List<int>();
}