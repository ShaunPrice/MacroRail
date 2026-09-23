# NSW Commute

Estimates the travel time to work in New South Wales by combining:

- **Live road traffic.** Open incidents, roadworks, fires, floods and major events from the
  Transport for NSW (TfNSW) *Live Traffic Hazards* API, matched against the driving route.
- **Train and public transport times.** Journeys from the TfNSW *Trip Planner* API, including
  real-time estimated departures, delays, cancellations and service alerts.
- **Park and ride.** Drive to one or more stations, then continue by public transport, with any
  changes between trains, metro, light rail, buses and ferries. For the trip home, it is the
  reverse: public transport to the station, then a drive home.
- **Driving time.** Either a traffic-aware estimate from the Google Routes API (optional key),
  or a model estimate: OSRM free-flow route time × a Sydney time-of-day congestion profile,
  plus an allowance for each live hazard within 250 m of the route.

The app then recommends the best option across all of these. When you set a departure time, it picks the earliest
arrival. When you set an arrive-by time, it picks the latest departure that still arrives on
time. Cancelled services are never recommended.

It has no dependencies and needs only Node.js 20.12 or later.

## Setup

1. Register at <https://opendata.transport.nsw.gov.au/>, create an application and subscribe it
   to **Trip Planner APIs** and **Live Traffic Hazards**. Copy the API key.
2. `cp .env.example .env` and set `TFNSW_API_KEY`, `HOME_ADDRESS` and `WORK_ADDRESS`.
   A location can be an address, suburb, station name, TfNSW stop id, or `lat,lon`.
3. Optional: set `PARK_AND_RIDE_STATIONS` (for example `Epping Station; Hornsby Station`) and
   `PARK_MINUTES` if you usually drive to a station.
4. Optional: set `GOOGLE_MAPS_API_KEY` (with the Routes API enabled) for traffic-aware driving times.

## Usage

```bash
npm start             # web app at http://localhost:3000
npm run demo          # same, using simulated data (no key or network needed)

node cli.js                          # leave now, home -> work
node cli.js --arrive 08:45 --rail    # arrive by 08:45, trains only
node cli.js --reverse --depart 17:30 # trip home (train to the station, then drive)
node cli.js --via "Epping Station; Hornsby Station" --park 7
node cli.js --no-via                 # skip park and ride
node cli.js --from=-33.8173,151.0053 --to "Central" --json
```

## Park and ride

For each station, the app plans the drive once. It then asks the Trip Planner for the next
services from that station to your destination, including any changes between services. For
each service it gives the **latest time to leave home**: the service's departure, minus the time
to park and walk to the platform, minus the drive time at that time of day (plus any hazards on
the way). If a service is running late, the app uses its timetabled departure, because a late
train can make up time.

- **Car at the start** (the morning trip): drive to the station, then public transport.
- **Car at the end** (the evening trip): public transport to the station, walk to the car, then
  drive home. In the web page, swapping From and To switches between these two; in the CLI,
  `--reverse` does the same.

Each station shows three options. The recommendation compares them with driving and with public
transport the whole way. If a station cannot be found, the app says so for that station and still
shows the other results.

The web page refreshes every 60 seconds while auto-refresh is ticked. It also remembers your
last start and destination in this browser only.

## Android app

`android/` contains a small native Android app (no third-party dependencies) that runs the same
interface in a WebView. It needs no server: the planner runs on the phone. Requests to Transport
for NSW, OSRM and Google go through a native bridge that only allows those three HTTPS hosts. You
enter your API key under **Settings** (the gear icon). It is stored only on the phone.

**Getting the APK.** Each push that changes `commute/` runs the *NSW Commute Android APK* GitHub
Actions workflow. The workflow runs the tests, builds the APK, and publishes it as a pre-release
named *NSW Commute build N*, with the `.apk` and a `.zip` of it. To install on the phone:

1. Open the release page on the phone and download the `.apk`.
2. Open the download. When Android asks, allow installs from that app (browser or My Files).
3. Open **NSW Commute**, tap the gear icon, and enter your key (or turn on demo mode).

**Signing.** Without signing secrets, the APK is signed with a temporary debug key, so each new
build must be installed after uninstalling the previous one. For in-place updates, create a
keystore once and add these repository secrets: `ANDROID_KEYSTORE_BASE64` (the keystore,
base64-encoded), `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` and `ANDROID_KEY_PASSWORD`.

```bash
keytool -genkeypair -v -keystore release.jks -alias nswcommute -keyalg RSA -keysize 2048 -validity 10000
base64 -w0 release.jks   # paste into the ANDROID_KEYSTORE_BASE64 secret
```

**Building locally** needs the Android SDK (platform 35) and JDK 17 or later:

```bash
node android/sync-web.mjs                   # copy public/ and lib/ into the app's assets
gradle -p android assembleDebug             # Gradle 8.9 or later
adb install android/app/build/outputs/apk/debug/app-debug.apk
```

To try the app's standalone mode in a desktop browser, run the server and open
`http://localhost:3000/?standalone=1`.

## HTTP API

| Endpoint | Parameters |
|---|---|
| `GET /api/commute` | `from`, `to` (default from `.env`), `mode=depart\|arrive`, `time=HH:MM`, `date=YYYY-MM-DD`, `railOnly=1`, `via=Station A; Station B`, `park=MINUTES`, `parkAt=start\|end` |
| `GET /api/locations` | `q`: location search (Trip Planner stop finder) |
| `GET /api/config` | defaults and whether demo mode is on |

All times are Sydney local time (AEST or AEDT). If one data source fails, the response still
returns the other results and reports the failure in `errors`.

## Accuracy and assumptions

- **Public transport** times come straight from TfNSW. Where real-time data exists, the app uses
  estimated times instead of timetabled times.
- **Model driving estimate (no Google key).** The congestion profile in `lib/traffic.js` is an
  assumption, not measured data: weekday peaks of ×1.6 at 08:00 and 17:00, with a lighter
  weekend profile. It does not account for school or public holidays. The delays assigned to
  hazards are also assumptions (15 min major, 6 min incident, 3 min roadwork, …). Calibrate
  both against trips you have actually driven. The public OSRM server is a shared demo service;
  set `OSRM_URL` to use your own instance for regular use.
- With a Google key, driving time uses Google's live and predictive traffic. Hazards are shown
  for information only and are not added on top.

## Tests

```bash
npm test
```

The unit tests cover the Sydney time zone and daylight-saving handling, the geometry, the
congestion model, recommendation logic and API response parsing. The integration tests run
the HTTP server against the simulated data source in `lib/demo.js`.

The parser fixtures in `test/fixtures` are modelled on the published TfNSW response formats. They
have not yet been checked against live responses. After adding a key, a quick
`node cli.js --json` against the live service will confirm them.
