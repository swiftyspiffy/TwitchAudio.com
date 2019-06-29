# TwitchAudio - Identify Music Playing on Twitch Streams and Clips!
[TwitchAudio.com](https://twitchaudio.com)

***This repository is a work in progress. Each of the core components is being open sourced individually as they're available. Bear with me :)***

## Overview
TwitchAudio is a website that allows users to submit a Twitch username or clip slug, and the service will attempt to identify audio playing in the stream or clip. The service is made up of three main components: Frontend, Backend, Lambda function, Database. Each of these will be discussed below. The frontend, backend and lambda function code will be available in this repository.

### Frontend
The frontend is what users see when they visit TwitchAudio.com. The frontend has 3 different templates: unauthenticated page, authenticated page, and whitelisted page. The unauthenticated page allows anyone to use the service without first logging in with their Twitch account. The authenticated page requires logging in via the user's Twitch account. Whitelisted page requires the user be subscribed to specific channels to use the service. When a request is launched, a popup shows while the backend launches a Lambda function invocation to finger print the audio. When the request finishes, it either hides the popup (if identification fails), or it shows the available finger printing details, including title, artists, album, youtube, spotify, and deezer data.

### Backend
The backend performs four primary purposes: access control, determining parallel tasks and deduping, retrieving stored clip data results, and invoking of the Lambda finger printing function. Access control is dictated by a settings table. Either the user will be able to use the service unauthenticated, required to login with their Twitch account, or required to login with their Twitch account as well as be subscribed to one of a few channels. Additionally, the backend handles checking if existing jobs are finger printing streams, and merging requests. Also, the backend checks to ensure only one job per IP runs at any given time. Finally, the backend will retrieve results immediately for clips that have been already fingerprinted, meaning finger printing isn't necessary.

### Lambda Function
The Lambda function handles the actual finger printing of a Twitch stream or clip. The following flows are used for each:
**Stream Fingerprinting:**
 * Update finger printing database to indicate job is being run
 * A check is performed to see if dependencies already exist on the file system from a previuous invoke (ie a non-cold start). If not:
   * *ffmpeg*: used for capturing stream audio
   * *acrcloud_extr_linux*: used for creating a finger print from an mp3 that can be sent to acrcloud to be analyzed
 * Get stream's m3u8 raw stream file using undocumented Twitch API endpoints
 * ffmpeg is started using the m3u8 link
 * Program waits 6 seconds, then executes kill -9 to kill ffmpeg process
 * Mp3 is sent through *acrcloud_extr_linux* to generate finger print file
 * Finger print file is uploaded to acrcloud.com where it's analyzed
 * Mp3 and finger print files are removed from file system
 * Update finger printing database to indicate job has finished, and save results
 
**Clip Fingerprinting:**
 * Update finger printing database to indicate job is being run
 * A check is performed to see if dependencies already exist on the file system from a previuous invoke (ie a non-cold start). If not:
   * *ffmpeg*: used for capturing stream audio
   * *acrcloud_extr_linux*: used for creating a finger print from an mp3 that can be sent to acrcloud to be analyzed
 * Mp4 passed as identifier string to Lambda function from backend is downloaded to file system
 * ffmpeg is started using the m3u8 link
 * Program waits 6 seconds, then executes kill -9 to kill ffmpeg process
 * Mp4 is sent through *acrcloud_extr_linux* to generate finger print file
 * Finger print file is uploaded to acrcloud.com where it's analyzed
 * Mp4 and finger print files are removed from file system
 * Update finger printing database to indicate job has finished, and save results

### Databases
TwitchAudio uses two different databases. The first database is the access control and logging database. This is a MySQL database that runs on a shared Namecheap server. The core responsibilities of this database are the following:
 * Access codes: Manual activation for when website is in subscriber-only mode. This allows users that are not subscribed to specified streamers to still use the service.
 * Logging: API hits, job launches, website hits, login success/failures, etc.
 * API keys: Keys used for accessing the service via API calls.
 * Blacklisted values: Prevents finger printing from users that meet certain conditions, or streams/clips that meet certain conditions.
 * Subscribe Broadcasters: If enabled, users must be subscribed to certain broadcasters to use the service.
 * Website Settings: Force login, requiring subscriptions, maintenance mode, website active, stream/clip toggling, message of the day.
 * Whitelisted Users: Users able to use the service when required subscription mode is enabled.

The second database is a AWS DynamoDB, and it's responsibilities are the following:
 * Stream fingerprinting job state
 * Clip fingerprinting job state

Where the tables have the following scheme:
 * Album(S)
 * Artists(SS)
 * ChannelId(S)/ClipId(S)
 * CreatedAt(S)
 * DeezerArtistsIds(SS)
 * DeezerTrackId(S)
 * Id(S)
 * Label(S)
 * SpotifyArtistsId(SS)
 * SpotifyTrackingId(S)
 * Status(S)
 * Title(S)
 * YouTubeId(S)


## Services Used
- [ACRCloud](https://acrcloud.com): This service has generously provided free audio fingerprinting. Check them out!
- [AWS](https://aws.amazon.com): Amazon Web Services is what hosts the identification DynamoDB database and Lambda function.
- [Namecheap](https://namecheap.com): Namecheap is used to host the frontend webserver and the access control MySQL database.
- [ffmpeg](https://ffmpeg.org/): ffmpeg is used to capture audio from Twitch streams, and save it as an mp3 file.

## Contributors
 * Cole ([@swiftyspiffy](http://twitter.com/swiftyspiffy))
 
## License
MIT License. &copy; 2019 Cole