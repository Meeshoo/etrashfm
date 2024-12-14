using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DJ>();
builder.Services.AddHostedService<DJ>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

var app = builder.Build();


string BASE_URL = "http://127.0.0.1:5500";
string API_URL = "http://127.0.0.1:8000";


app.UseCors( x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins(BASE_URL));


// Endpoints
app.MapGet("/getcurrentvideoid", ([FromServices] DJ dj) =>
{
    string currentSongID = dj.GetCurrentSongID();

    return currentSongID;
});

app.MapGet("/getcurrentvideotime", ([FromServices] DJ dj) =>
{
    int currentSongTimestamp = dj.GetCurrentSongTimestamp();

    return currentSongTimestamp;
});

app.MapGet("/getqueue", ([FromServices] DJ dj) => {

    string result = @"<br><br><h2 id=""queue_title"">Queue</h2><br>";

    IEnumerable<string> queue = dj.GetQueue();
    List<string> queue_list = queue.ToList();
    IEnumerable<string> queue_ids = dj.GetQueueIds();
    List<string> queue_id_list = queue_ids.ToList();

    if (!queue.Any()) {
        result += @"<p>Queue's empy 🤷</p><div id=""queue_list"">";
    } else {
        foreach (var song in queue_list) {
            string id = queue_id_list[queue_list.IndexOf(song)];
            result += $@"
                <div class=""queue_entry"">
                <p>{song}</p>
                <button class=""thin_button"" hx-post=""{API_URL}/removesongfromqueue?video_id={id}""
                    hx-trigger=""click"">
                    Remove
                </button>
                </div>
                <br>
                ";
        }
        result += "</div>";
    }

    return result;
});

app.MapGet("/skipcurrentsong", ([FromServices] DJ dj) =>
{
    dj.SkipCurrentSong();

    return "Skipped, please refresh";
});


app.MapPost("/addsongtoqueue", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) =>
{
    if (validateUrl(youtubeVideoURL)) {
        string id = getIdFromUrl(youtubeVideoURL);
        dj.AddSongToQueue(id);
        return "Added to queue (probably)";
    } else {
        return "Bad link I am afraid";
    }

}).DisableAntiforgery();

app.MapPost("/removesongfromqueue", ([FromQuery] string video_id, [FromServices] DJ dj) => {

        dj.RemoveSongFromQueue(video_id);


}).DisableAntiforgery();

app.MapGet("/forgetcurrentsong", ([FromServices] DJ dj) => {

        dj.ForgetCurrentSong();

        return "forgo 💀";

}).DisableAntiforgery();

app.MapGet("/getcurrentvibe", ([FromServices] DJ dj) => {

        string theVibe = dj.GetCurrentVibe();

        return theVibe;

}).DisableAntiforgery();

app.MapPost("/setcurrentvibe", ([FromServices] DJ dj, [FromForm] string vibe) => {

        string theVibe = dj.SetCurrentVibe(vibe);

        return theVibe + " type beat";

}).DisableAntiforgery();

app.MapPost("/submitvibe", ([FromServices] DJ dj, [FromForm] string vibe, [FromQuery] string video_id) => {

        dj.SubmitVibe(vibe, video_id);

        return "Vibe updated (I hope)";

}).DisableAntiforgery();

app.MapGet("/getbacklog", ([FromServices] DJ dj) => {

        IEnumerable<DJ.Song> songsInBacklog = dj.GetBacklog();

        string backlogHTML = "<table><tr><th>Title</th><th>Vibe</th><th>Playcount</th><th>Vibe Selection</th></tr>";

        foreach (var song in songsInBacklog) {
            backlogHTML += $@"<tr><td>{song.title}</td><td>{song.vibe}</td><td>{song.play_count}</td><td>
                    <div id=""vibe_updater"">
                        <form hx-post=""http://127.0.0.1:8000/submitvibe?video_id={song.video_id}"" hx-target=""#vibe_submit_result"" hx-swap=""innerHTML settle:3s"">
                            <label for=""vibe""></label>
                            <select name=""vibe"">
                                <option value=""christmas"">Christmas</option>
                                <option value=""dance"">Dance / Electronic / Pog</option>
                                <option value=""punk"">Punk / Ska</option>
                                <option value=""metal"">Metal</option>
                                <option value=""funk"">Funk</option>
                                <option value=""indie"">Indie / Rock</option>
                                <option value=""chill"">Chill</option>
                                <option value=""real"">Real</option>
                                <option value=""meme"">Meme</option>
                            </select>
                            <button class=""thin_button"">Update</button>
                        </form>
                        <p class=""message"" id=""vibe_submit_result""></p>
                    </div>
                    </td></tr>";
        }

        backlogHTML += "</table>";

        return backlogHTML;

}).DisableAntiforgery();

app.MapGet("/fillbacklog", ([FromServices] DJ dj) => {

    dj.FillBacklogTitle();
    dj.FillBacklogDuration();

}).DisableAntiforgery();

app.MapGet("/getbacklogcount", ([FromServices] DJ dj) => {

    int songCount = dj.GetBacklogCount();

    return songCount;

}).DisableAntiforgery();

string getIdFromUrl(string youtubeVideoURL) {
    int questionMarkPosition = youtubeVideoURL.IndexOf('?');
    string result = youtubeVideoURL.Substring(questionMarkPosition + 3, 11);
    return result;  
}

bool validateUrl(string youtubeVideoURL) {
    if (youtubeVideoURL.Contains("?v=")) {
        return true;
    } else {
        return false;
    }
}

app.Run();
