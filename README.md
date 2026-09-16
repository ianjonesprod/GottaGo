# GottaGo

Find and review public bathrooms. A map of places you can actually go, with ratings for the
five things that matter when you get there: smell, cleanliness, amenities, accessibility and
ambience.

Seeded with 25 real, publicly-accessible restrooms in and around Cleveland, Ohio.

## What you need

- .NET 10 SDK
- Node 20 or later
- SQL Server LocalDB (ships with the SQL Server Express installer and with Visual Studio)

Verified against .NET 10.0.400, Node 24 and Angular CLI 22.

## Running it

```bash
# 1. Create the database and load the demo data
dotnet run --project src/api/GottaGo.Api -- --migrate
dotnet run --project src/api/GottaGo.Api -- --seed

# 2. Start the API (leave it running)
dotnet run --project src/api/GottaGo.Api

# 3. In another terminal, start the app
cd src/web
npm install
npm start
```

Then open <http://localhost:4200>. The API is on port 5277, with Swagger at
<http://localhost:5277/swagger>.

The dev server proxies `/api` through to the backend, so the browser only ever talks to
`localhost:4200`. Everything is same-origin, which is why there is no CORS configuration
anywhere in this repository and the auth cookie is first-party.

### Configuration

Two settings live outside the repository, in .NET user-secrets:

```bash
# Required. The API refuses to start without it rather than falling back to a
# predictable default, because a known signing key lets anybody mint valid tokens.
dotnet user-secrets set "Jwt:SigningKey" "<48 random bytes, base64>" --project src/api/GottaGo.Api

# Optional. Without it the map area explains itself and the results list carries the feature.
dotnet user-secrets set "GoogleMaps:ApiKey" "<your key>" --project src/api/GottaGo.Api
```

The Maps key is handed to the browser at runtime from `/api/client-config` rather than being
compiled into the bundle, so it never enters git history and rotating it needs no rebuild. It
is visible to anyone using the site either way, so restrict it by HTTP referrer in Google
Cloud — that, not secrecy, is what protects it.

Google sign-in is optional too. Set `Authentication:Google:ClientId` and `:ClientSecret` and
the button appears on the sign-in page; leave them unset and it does not.

## Testing

```bash
dotnet test                 # domain, application and layering tests
cd src/web && npm test      # frontend unit tests (Vitest)
cd e2e && npx playwright test   # end-to-end and accessibility, needs both servers running
```

The end-to-end suite blocks the Google Maps script. That keeps it off the network and free to
run, and it doubles as proof that every page works with no map at all.

## How it fits together

```
src/api/
  GottaGo.Domain          entities and rules, no dependencies at all
  GottaGo.Application     services and the repository interfaces
  GottaGo.Infrastructure  Dapper, SQL scripts, identity, storage
  GottaGo.Api             controllers, auth, Swagger
src/web/                  Angular 22 app
e2e/                      Playwright specs
tests/                    xUnit projects
```

Dependencies only ever point inward. The application layer declares what it needs and knows
nothing about how it is implemented — the only file in the solution that names an
infrastructure type is `Program.cs`. That is not a convention anyone has to remember:
`GottaGo.Architecture.Tests` reads the project files and fails the build if a forbidden
dependency is added.

The frontend talks to the API through one typed client, which maps wire shapes into the app's
own models. A field rename on the server breaks one mapper rather than a dozen templates.

## Some decisions worth knowing

**Ratings are averaged on read rather than stored.** At this size an average is cheaper than
keeping a denormalised copy correct on every write.

**High scores are not a plain average.** One five-star review would otherwise outrank a
bathroom forty people scored 4.8, so each average is pulled toward the middle of the scale by
three imaginary average reviews. The more real reviews something collects, the less that pull
matters. Both the raw average and the review count are shown so the ranking is explicable.

**One review per person per bathroom.** Reviewing again edits what you wrote.

**Access notes matter more than they look.** Half these restrooms are seasonal, event-only or
behind a fare gate. Somebody who walks twenty minutes to a locked door is worse served than
somebody who was told up front.

**The map is an enhancement, not the feature.** A list beside it carries every bathroom in
view as ordinary links, visible to everyone rather than hidden for screen readers. If the map
cannot load, nothing is lost.

**The demo reviews are invented, about real named places.** Every seeded row is flagged and
labelled in the UI, and the seeder refuses to run in production.
