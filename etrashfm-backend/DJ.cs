using System.Data;
using Microsoft.Data.Sqlite;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Dapper;

public class DJ : IHostedService, IDisposable {

    private Timer? _timer = null;
    private static string currentSongID = "a3QIRd0Tfbs";
    private static int currentSongDuration = 95;
    private static int currentTime = 0;
    private readonly IConfiguration configuration;
    readonly string youtubeApiKey;
    YouTubeService yt;
    readonly IDbConnection database;


    public DJ(IConfiguration configuration){

        database = new SqliteConnection(configuration.GetConnectionString("Database"));

        youtubeApiKey = configuration.GetValue<string>("YoutubeAPIKey");

        yt = new(new BaseClientService.Initializer() {
            ApiKey = youtubeApiKey,
            ApplicationName = "eTrash-FM"
        });

        Console.WriteLine("NEW");
        // ADD TEST SONGS
        // AddSongToQueue("Oly6ayyckZI");
    }

    public Task StartAsync(CancellationToken stoppingToken) {
        Console.WriteLine("DJ is running");
        _timer = new Timer(HandleMusic, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        return Task.CompletedTask;
    }

    private void HandleMusic(object? state) {

        if (currentTime >= currentSongDuration) {

            Console.WriteLine("Song ended");

            database.Execute($"DELETE FROM [queue] WHERE (video_id = \"{currentSongID}\")");

            int numberOfSongsInQueue = database.Query<string>("SELECT [video_id] FROM [queue]").Count();

            if (numberOfSongsInQueue == 0){
                Console.WriteLine("No songs left, grabbing random from DB");
                currentSongID = database.Query<string>("SELECT [video_id] FROM [backlog] ORDER BY RANDOM() LIMIT 1").First();
            } else {
                currentSongID = database.Query<string>("SELECT [video_id] FROM [queue] ORDER BY queue_id LIMIT 1").First();
            }

            var request = yt.Videos.List("contentDetails");
            request.Id = currentSongID;
            var result = request.Execute();
            currentSongDuration = Convert.ToInt16(XmlConvert.ToTimeSpan(result.Items.First().ContentDetails.Duration).TotalSeconds);

            Console.WriteLine($"Now playing: {currentSongID} with duration {currentSongDuration}s");

            int playcount = database.Query<int>($"SELECT [play_count] FROM [backlog] WHERE video_id = \"{currentSongID}\"").First();
            playcount++;
            database.Execute($"UPDATE [backlog] SET play_count = @play_count WHERE video_id = \"{currentSongID}\"", new
            {
                play_count  = playcount
            });

            currentTime = 0;
        }

        currentTime++; 

        Console.WriteLine($"Current time: {currentTime}");
    }

    public void AddSongToQueue(String videoID) {

        database.Execute("INSERT INTO [queue] VALUES(NULL, @video_id)", new
            {
                video_id = videoID,
            });
        Console.WriteLine($"Song added with ID: {videoID}");

        var song = database.Query<int>($"SELECT [play_count] FROM [backlog] WHERE [video_id] = \"{videoID}\"");
        Console.WriteLine($"Play count: {song.Count()}");
        
        if (!song.Any()) {
            database.Execute("INSERT INTO [backlog] VALUES(@video_id, @play_count)", new
            {
                video_id = videoID,
                play_count  = 0
            });
        }
    }

    public void RemoveSongFromQueue(String videoID) {
        database.Execute($"DELETE FROM [queue] WHERE (video_id = \"{videoID}\")");
    }

    public String GetCurrentSongID(){
        return currentSongID;
    }

    public int GetCurrentSongTimestamp(){
        return currentTime;
    }

    public void ForgetSong(string videoID){
        database.Execute($"DELETE FROM [backlog] WHERE (video_id = \"{videoID}\")");
    }

    public Task StopAsync(CancellationToken stoppingToken) {

        _timer?.Change(Timeout.Infinite, 0);
        Console.WriteLine("DJ stopped");

        return Task.CompletedTask;
    }

    public void Dispose() {
        _timer?.Dispose();
    }
}