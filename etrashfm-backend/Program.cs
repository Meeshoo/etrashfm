using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DJ>();
builder.Services.AddHostedService<DJ>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

var app = builder.Build();

string BASE_URL = "BaseUrlGoesHerePlease";

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

app.MapPost("/addsongtoqueue", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) =>
{
    string youtubeVideoID = youtubeVideoURL.Substring(youtubeVideoURL.Length - 11, 11);

    dj.AddSongToQueue(youtubeVideoID);

    return "Added (probably)";

}).DisableAntiforgery();

app.MapPost("/removesongfromqueue", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) => {

    string youtubeVideoID = youtubeVideoURL.Substring(youtubeVideoURL.Length - 11, 11);

    dj.RemoveSongFromQueue(youtubeVideoID);

    return "Removed (probably)";

}).DisableAntiforgery();

app.MapPost("/forgetsong", ([FromForm] string youtubeVideoURL, [FromServices] DJ dj) => {

    string youtubeVideoID = youtubeVideoURL.Substring(youtubeVideoURL.Length - 11, 11);

    dj.ForgetSong(youtubeVideoID);

    return "Ye shall never hear it again (probably)";

}).DisableAntiforgery();

app.Run();
