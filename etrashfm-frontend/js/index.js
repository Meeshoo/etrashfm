const YTPlayer = require('yt-player')
const player = new YTPlayer('#player')

API_URL = "http://localhost:8000"

window.onload = function() {
  setTimeout(() => loadCurrentVideo(), 1000);
  player.setVolume(5)
};

player.on('ended', () => {
  setTimeout(() => loadCurrentVideo(), 1000);
})


// async function seekToServerTime() {
//   const url = API_URL.concat("/getcurrentvideotime");
//   try {
//     const response = await fetch(url);
//     if (!response.ok) {
//       throw new Error(`Response status: ${response.status}`);
//     }

//     video_time = await response.json();

//   } catch (error) {
//     console.error(error.message);
//   }

//   console.log("Time to seek to : ".concat(video_time))
//   player.seek(video_time)

// }

async function loadCurrentVideo() {
  const url = API_URL.concat("/getcurrentvideoid");
  try {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }

    video_id = await response.text();
  } catch (error) {
    console.error(error.message);
  }

  const url2 = API_URL.concat("/getcurrentvideotime");
  try {
    const response = await fetch(url2);
    if (!response.ok) {
      throw new Error(`Response status: ${response.status}`);
    }

    video_time = await response.text();
  } catch (error) {
    console.error(error.message);
  }

  console.log("Video ID: ".concat(video_id," at ", video_time))
  player.load(video_id, true, video_time)

}