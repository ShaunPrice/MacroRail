# NSW Commute

Estimates the travel time to work in New South Wales by combining:

- **Live road traffic.** Open incidents, roadworks, fires, floods and major events from the
  Transport for NSW (TfNSW) *Live Traffic Hazards* API, matched against the driving route.
- **Train and public transport times.** Journeys from the TfNSW *Trip Planner* API, including
  real-time estimated departures, delays, cancellations and service alerts.
- **Driving time.** Either a traffic-aware estimate from the Google Routes API (optional key),
  or a model estimate: OSRM free-flow route time × a Sydney time-of-day congestion profile,
  plus an allowance for each live hazard within 250 m of the route.

The app then recommends the best option. When you set a departure time, it picks the earliest
arrival. When you set an arrive-by time, it picks the latest departure that still arrives on
time. Cancelled services are never recommended.

It has no dependencies and needs only Node.js 20.12 or later.

## Setup

1. Register at <https://opendata.transport.nsw.gov.au/>, create an application and subscribe it
   to **Trip Planner APIs** and **Live Traffic Hazards**. Copy the API key.
2. `cp .env.example .env` and set `TFNSW_API_KEY`, `HOME_ADDRESS` and `WORK_ADDRESS`.
   A location can be an address, suburb, station name, TfNSW stop id, or `lat,lon`.
3. Optional: set `GOOGLE_MAPS_API_KEY` (with the Routes API enabled) for traffic-aware driving times.

## Usage

```bash
npm start             # web app at http://localhost:3000
npm run demo          # same, using simulated data (no key or network needed)

node cli.js                          # leave now, home -> work
node cli.js --arrive 08:45 --rail    # arrive by 08:45, trains only
node cli.js --reverse --depart 17:30 # trip home
node cli.js --from=-33.8173,151.0053 --to "Central" --json
```

The web page refreshes every 60 seconds while auto-refresh is ticked. It also remembers your
last start and destination in this browser only.

## HTTP API

| Endpoint | Parameters |
|---|---|
| `GET /api/commute` | `from`, `to` (default from `.env`), `mode=depart\|arrive`, `time=HH:MM`, `date=YYYY-MM-DD`, `railOnly=1` |
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
