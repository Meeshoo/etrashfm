using System.Data;
using Microsoft.Data.Sqlite;
using System.Xml;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Dapper;
using System.ComponentModel;

public class DJ : IHostedService, IDisposable {

    private Timer? _timer = null;
    private static string currentSongID;
    private static int currentSongDuration = 0;
    private static int currentTime = 1;
    private static string currentVibe = "any";
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
        // AddSongToQueue("xj3SZv5lle4");
        // AddSongToQueue("xX8p9l0UTsQ");
    }

    public Task StartAsync(CancellationToken stoppingToken) {
        Console.WriteLine("DJ is running");
        _timer = new Timer(HandleMusic, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        return Task.CompletedTask;
    }

    private void HandleMusic(object? state) {

        if (currentTime >= currentSongDuration) {

            Console.WriteLine("Song ended");
            currentTime = 0;

            int numberOfSongsInQueue = database.Query<string>("SELECT [video_id] FROM [queue]").Count();

            if (numberOfSongsInQueue == 0){
                Console.WriteLine("No songs left, grabbing random from DB");
                if (currentVibe == "any") {
                    currentSongID = database.Query<string>("SELECT [video_id] FROM [backlog] ORDER BY RANDOM() LIMIT 1").First();
                } else {
                    currentSongID = database.Query<string>($"SELECT [video_id] FROM [backlog] WHERE vibe = \"{currentVibe}\" ORDER BY RANDOM() LIMIT 1").FirstOrDefault("_YyzVXQyE_8");
                }
                currentSongDuration = database.Query<int>($"SELECT [duration] FROM [backlog] WHERE video_id = \"{currentSongID}\" LIMIT 1").FirstOrDefault(100);
                
            } else {
                currentSongID = database.Query<string>("SELECT [video_id] FROM [queue] ORDER BY queue_id LIMIT 1").First();

                currentSongDuration = database.Query<int>($"SELECT [duration] FROM [queue] WHERE video_id = \"{currentSongID}\" LIMIT 1").First();

                int playcount = database.Query<int>($"SELECT [play_count] FROM [backlog] WHERE video_id = \"{currentSongID}\"").FirstOrDefault(-1);
                if (playcount != -1) {
                    playcount++;
                    database.Execute($"UPDATE [backlog] SET play_count = @play_count WHERE video_id = \"{currentSongID}\"", new
                {
                    play_count  = playcount
                });
                }
            }

            database.Execute($"DELETE FROM [queue] WHERE (video_id = \"{currentSongID}\")");

            Console.WriteLine($"Now playing: {currentSongID} with duration {currentSongDuration}s");
        }

        currentTime++; 
    }

    public void AddSongToQueue(String videoID) {

        var request = yt.Videos.List("snippet");
        request.Id = videoID;
        var result = request.Execute();
        string songTitle = result.Items.First().Snippet.Title ?? "ERROR GETTING TITLE (soz)";

        var request2 = yt.Videos.List("contentDetails");
        request2.Id = videoID;
        var result2 = request2.Execute();
        int songDuration = Convert.ToInt16(XmlConvert.ToTimeSpan(result2.Items.First().ContentDetails.Duration).TotalSeconds);

        database.Execute("INSERT INTO [queue] VALUES(NULL, @video_id, @video_title, @duration)", new
            {
                video_id = videoID,
                video_title = songTitle,
                duration = songDuration
            });
        Console.WriteLine($"Song added with ID: {videoID}");

        var song = database.Query<int>($"SELECT [play_count] FROM [backlog] WHERE [video_id] = \"{videoID}\"");
        Console.WriteLine($"Play count: {song.Count()}");
        
        if (!song.Any()) {
            database.Execute("INSERT INTO [backlog] VALUES(@video_id, @play_count, @vibe, @title, @duration)", new
            {
                video_id = videoID,
                play_count = 0,
                vibe = "",
                title = songTitle,
                duration = songDuration
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

    public IEnumerable<string> GetQueue(){
        IEnumerable<string> queue = database.Query<string>("SELECT [video_title] FROM [queue]");
        return queue;
    }

    public IEnumerable<string> GetQueueIds(){
        IEnumerable<string> queue = database.Query<string>("SELECT [video_id] FROM [queue]");
        return queue;
    }

    public IEnumerable<Song> GetBacklog(){
        IEnumerable<Song> backlog = database.Query<Song>("SELECT * FROM [backlog]");
        return backlog;
    }

    public void SkipCurrentSong(){
         currentTime = currentSongDuration;
    }

    public void ForgetCurrentSong(){
        database.Execute($"DELETE FROM [backlog] WHERE (video_id = \"{currentSongID}\")");
    }

    public string GetCurrentVibe(){
        return currentVibe;
    }

    public string SetCurrentVibe(string vibe){
        return currentVibe = vibe;
    }

    public void SubmitVibe(string vibe, string video_id){
        database.Execute($"UPDATE [backlog] SET vibe=\"{vibe}\" WHERE video_id = \"{video_id}\"");
    }

    public void FillBacklogTitle() {
        IEnumerable<Song> backlog = database.Query<Song>("SELECT * FROM [backlog] WHERE title = NULL");
        foreach (var song in backlog) {
            var request = yt.Videos.List("snippet");
            request.Id = song.video_id;
            var result = request.Execute();
            string songTitle = result.Items.First().Snippet.Title ?? "ERROR GETTING TITLE (soz)";
            database.Execute($"UPDATE [backlog] SET title = \"{songTitle}\" WHERE video_id = \"{song.video_id}\"");
        }
    }

    public void FillBacklogDuration() {
        IEnumerable<Song> backlog = database.Query<Song>("SELECT * FROM [backlog] WHERE duration = NULL");
        foreach (var song in backlog) {
        var request2 = yt.Videos.List("contentDetails");
        request2.Id = song.video_id;
        var result2 = request2.Execute();
        int songDuration = Convert.ToInt16(XmlConvert.ToTimeSpan(result2.Items.First().ContentDetails.Duration).TotalSeconds);
            database.Execute($"UPDATE [backlog] SET duration = \"{songDuration}\" WHERE video_id = \"{song.video_id}\"");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken) {

        _timer?.Change(Timeout.Infinite, 0);
        Console.WriteLine("DJ stopped");

        return Task.CompletedTask;
    }

    public void Dispose() {
        _timer?.Dispose();
    }

    public struct Song {
        public string video_id;
        public int play_count;
        public string vibe;
        public string title;
        public int duration;
    }
}

//NEED TO ADD TITLE AND DURATION COLUMNS TO BACKLOG
