using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DJ>();
builder.Services.AddHostedService<DJ>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

var app = builder.Build();

string BASE_URL = "BaseUrlGoesHerePlease";
//string BASE_URL = "http://127.0.0.1:5500";


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

app.MapGet("/getqueue", ([FromServices] DJ dj) =>
{
    IEnumerable<string> queue = dj.GetQueue();
    string result = @"<br><br><h2 id=""queue_title"">Queue</h2><br>";

    foreach (var song in queue) {
        result += $"<p>{song}</p><br>";
    }

    return result;
});

app.MapGet("/skipcurrentsong", ([FromServices] DJ dj) =>
{
    dj.SkipCurrentSong();

    //return;
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

app.MapPost("/removesongfromqueue", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) => {

    if (validateUrl(youtubeVideoURL)) {
        string id = getIdFromUrl(youtubeVideoURL);
        dj.RemoveSongFromQueue(id);
        return "It's gone! (most likely)";
    } else {
        return "Bad link pal";
    }

}).DisableAntiforgery();

app.MapPost("/forgetsong", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) => {

    if (validateUrl(youtubeVideoURL)) {
        string id = getIdFromUrl(youtubeVideoURL);
        dj.ForgetSong(id);
        return "Ye shall never hear it again (probably)";
    } else {
        return "Bad link buddy";
    }

}).DisableAntiforgery();

string getIdFromUrl(string youtubeVideoURL) {
    string result = youtubeVideoURL.Substring(youtubeVideoURL.Length - 11, 11);
    return result;  
}

bool validateUrl(string youtubeVideoURL) {
    if (youtubeVideoURL.Length != 43) {
        return false;
    } else {
        return true;
    }    
}

app.Run();
