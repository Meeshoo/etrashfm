const YTPlayer = require('yt-player')
const player = new YTPlayer('#player')

API_URL = "http://localhost:8000"

loadCurrentVideo()

player.setVolume(5)

player.play()

setTimeout(() => seekToServerTime(), 1000);

player.getCurrentTime()

player.on('playing', () => {
  console.log(player.getCurrentTime())
})

player.on('ended', () => {
  setTimeout(() => loadCurrentVideo(), 1000);
})




async function seekToServerTime() {
  const url = API_URL.concat("/getcurrentvideotime");
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }

    const video_time = await response.json();
    console.log("Time to seek to : ".concat(video_time))
    player.seek(video_time)
  } catch (error) {
    console.error(error.message);
  }
}

async function loadCurrentVideo() {
  const url = API_URL.concat("/getcurrentvideoid");
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }

    const video_id = await response.text();
    console.log("Video ID: ".concat(video_id))
    player.load(video_id, true)
  } catch (error) {
    console.error(error.message);
  }
}