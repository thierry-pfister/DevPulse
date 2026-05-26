# DevPulse — Project Context

> One concept. Every day. Get sharper.

An automated developer learning platform that publishes one focused technical concept per day across multiple platforms. Fully automated with a human intervention window before publishing.

---

## What It Is

**DevPulse** is a self-hosted, AI-powered content pipeline that generates and publishes one developer concept per day — functional programming patterns, DevOps techniques, security principles, architecture decisions, design patterns — anything that makes a developer meaningfully better.

The key differentiators:
- **One concept per day** — never more. The constraint is the quality filter.
- **Real-world anchor** — every concept connects to something the reader already knows (e.g. "useEffect is a closure", "user?.email is a manual Maybe monad")
- **Runnable code when it fits** — pure functions and algorithms run in the browser; infra and architecture are shown differently
- **Foreshadowing** — every post previews tomorrow's concept naturally, no artificial cliffhangers
- **Tag subscriptions** — readers subscribe to tags (functional, devops, security, etc.), not the whole blog
- **Daily streak** — readers mark concepts as learned, streak tracks consecutive days per subscribed tags
- **You can intervene** — 30-minute window before publish via Telegram or dashboard; fully automated otherwise

---

## Content Categories (Tags)

| Tag | Description |
|-----|-------------|
| `functional` | F# functional programming — Maybe, Result, Railway-Oriented, DU, CE |
| `devops` | Docker, Kubernetes, CI/CD, GitHub Actions, Helm |
| `security` | AppSec, Auth, JWT, STRIDE, RBAC, threat modeling |
| `architecture` | Design patterns, microservices, DDD, CQRS, event sourcing |
| `dotnet` | .NET / F# / C# specific tips, idioms, and patterns |
| `patterns` | General design patterns applicable across languages |

---

## Tech Stack

### Backend (Automation Engine)
- **Runtime:** .NET 9
- **Language:** F# and C# — use whichever suits the task better. F# preferred for domain logic, pipelines, and data transformation; C# preferred for integrations, boilerplate-heavy adapters, and anything where tooling expects it.
- **Scheduler:** HangFire (CRON-like job scheduling)
- **Database:** PostgreSQL (draft queue, subscriptions, streaks, analytics)
- **ORM:** Dapper or EF Core

### AI & Generation
- **Content generation:** Anthropic Claude API — `claude-sonnet-4-6`
- **Batch mode:** Enabled for scheduled (non-realtime) generation — 50% cost reduction
- **Image generation:** DALL-E 3 or self-hosted Stable Diffusion (homelab)
- **TTS (voice):** OpenAI TTS API or ElevenLabs Flash model
- **Video assembly:** Playwright (code screenshots) + FFmpeg (MP4 assembly)

### Publishing Targets
| Platform | Method | Notes |
|----------|--------|-------|
| Ghost | Content API (REST) | Primary site, canonical source for SEO |
| Dev.to | REST API v1 | Canonical → Ghost |
| Hashnode | GraphQL API | Canonical → Ghost |
| Medium | REST API | Canonical → Ghost |
| Reddit | OAuth + POST /api/submit | Per-topic subreddit mapping in config |
| YouTube Shorts | YouTube Data API v3 | TTS audio + code screenshots → MP4 via FFmpeg |

### Frontend (Intervention Dashboard)
- **Framework:** Next.js 15
- **Styling:** Tailwind CSS
- **Purpose:** Preview drafts, edit content, approve/delay/skip per platform
- **Auth:** Simple JWT-based, single user

### Notifications
- **Telegram bot** — intervention window alerts, topic override commands
- **Email** — reader tag subscriptions via Resend API
- **RSS per tag** — alternative subscription method

### Infrastructure
- **Self-hosted** — all services run on homelab (no hosting costs)
- **Containerized:** Docker Compose
- **Reverse proxy:** Nginx or Caddy

---

## Pipeline Flow

```
Topic Config (YAML)
      │
      ▼
HangFire Scheduler — picks today's concept
      │
      ▼
Context Enricher (optional) — GitHub trending / HN for freshness
      │
      ▼
Claude API — generates structured JSON episode
      │ (article + social variants + video script + image prompt)
      ▼
Format Branching
  ├── Ghost article (full markdown)
  ├── Dev.to / Hashnode / Medium (canonical)
  ├── Reddit post (subreddit mapped by tag)
  ├── YouTube Short (TTS → MP3 + Playwright screenshots → FFmpeg → MP4)
  └── Instagram caption (optional, Meta Graph API)
      │
      ▼
Draft Queue (PostgreSQL) — 30 min intervention window
      │  ← Telegram bot / Dashboard intervention here
      ▼
Auto-publish to all platforms
      │
      ▼
Tag webhooks → subscriber notifications (email / RSS)
      │
      ▼
Analytics aggregator — pulls engagement metrics daily
      │
      ▼
Performance ranker → feeds back into topic scheduler weights
```

---

## Claude Prompt Schema

Each generation call receives a structured input and returns structured JSON:

### Input
```json
{
  "concept": "The Maybe Monad",
  "tag": "functional",
  "language": "fsharp",
  "episodeNumber": 14,
  "realWorldAnchorHint": "user?.email optional chaining",
  "runnable": true,
  "foreshadowTopic": "Result<T> and railway-oriented programming",
  "tone": "precise, practical, no fluff",
  "targetAudience": "intermediate developers"
}
```

### Output (JSON)
```json
{
  "article": {
    "title": "The Maybe Monad",
    "subtitle": "You already use this — you just didn't have a name for it",
    "realWorldAnchor": "...",
    "body": "...",
    "runnableSnippet": "...",
    "imagePrompt": "...",
    "foreshadow": "Tomorrow: Result<T> — when None isn't enough and you need to know why something failed.",
    "tags": ["functional", "fsharp", "monad"]
  },
  "reddit": {
    "title": "...",
    "body": "..."
  },
  "youtube": {
    "title": "...",
    "description": "...",
    "script": "..."
  }
}
```

`reddit` and `youtube` are optional — omit them if the concept does not suit the platform.
Future platform variants (devto, hashnode, instagram) follow the same pattern: add a new optional key.

---

## Cost Estimate (per month at 1 post/day)

| Service | Est. Cost |
|---------|-----------|
| Claude API (Sonnet 4.6, batch mode) | ~$0.75 |
| OpenAI TTS or ElevenLabs Flash | ~$6–15 |
| DALL-E 3 / self-hosted SD | ~$0–3 |
| Hosting | $0 (self-hosted homelab) |
| **Total** | **~$7–20/month** |

---

## Topic Storage & Data Structure

Topics live in two places depending on their lifecycle phase.

### 1. `topic_config.yaml` — Seed List (flat file, hot-reloadable)

The source of truth for *what to write about*. You edit this to intervene — add a concept, bump a priority, mark something to skip. The scheduler reads this file without restarting.

```yaml
topics:
  - concept: "The Maybe Monad"
    tag: functional
    language: fsharp
    runnable: true
    foreshadow_next: "Result<T>"
    priority: 1
    skip: false

  - concept: "Docker Layer Caching"
    tag: devops
    runnable: false
    priority: 2
    skip: false

  - concept: "JWT Tokens Demystified"
    tag: security
    runnable: false
    priority: 3
    skip: false
```

**Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `concept` | string | The concept name — passed directly to Claude |
| `tag` | string | Maps to a content category tag |
| `language` | string? | Code language hint for generation (fsharp, csharp, bash, yaml…) |
| `runnable` | bool | Whether to generate a runnable embed |
| `foreshadow_next` | string? | Hint for what to preview at the end of the post |
| `priority` | int | Lower = sooner. Scheduler picks lowest unpublished priority |
| `skip` | bool | Set to true to skip without deleting |

---

### 2. PostgreSQL `episodes` Table — Full Lifecycle Record

Once a topic is picked by the scheduler it moves into the database and never goes back to YAML. The DB is the source of truth for everything that has been generated, published, and how it performed.

#### Episode Lifecycle States
```
queued → generating → draft → published
                    ↘ skipped
                    ↘ failed
```

#### Core Schema

```sql
-- Episodes (one row per published concept)
CREATE TABLE episodes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    concept         TEXT NOT NULL,
    tag             TEXT NOT NULL,
    language        TEXT,
    episode_number  INT NOT NULL,
    status          TEXT NOT NULL DEFAULT 'queued',  -- queued | generating | draft | published | skipped | failed

    -- Generation
    generated_at    TIMESTAMPTZ,
    content_json    JSONB,          -- raw Claude output, full episode JSON
    was_edited      BOOLEAN DEFAULT FALSE,

    -- Publishing
    publish_at      TIMESTAMPTZ,    -- scheduled publish time
    published_at    TIMESTAMPTZ,
    platform_ids    JSONB,          -- { ghost_id, devto_id, hashnode_id, reddit_id, youtube_id }

    -- Navigation
    previous_episode_id UUID REFERENCES episodes(id),
    next_episode_id     UUID REFERENCES episodes(id),
    related_concepts    TEXT[],     -- concept names this one connects to

    -- Performance
    performance_score   FLOAT,      -- computed from engagement metrics
    last_analytics_at   TIMESTAMPTZ,

    created_at      TIMESTAMPTZ DEFAULT now()
);

-- Analytics (pulled daily per platform per episode)
CREATE TABLE episode_analytics (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    episode_id  UUID NOT NULL REFERENCES episodes(id),
    platform    TEXT NOT NULL,      -- ghost | devto | hashnode | medium | reddit | youtube
    fetched_at  TIMESTAMPTZ NOT NULL,
    views       INT,
    reactions   INT,
    comments    INT,
    shares      INT,
    raw         JSONB               -- full platform response for future use
);

-- Tag subscriptions (readers)
CREATE TABLE subscriptions (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email       TEXT NOT NULL,
    tags        TEXT[] NOT NULL,
    method      TEXT NOT NULL,      -- email | rss
    created_at  TIMESTAMPTZ DEFAULT now()
);

-- Reader streaks
CREATE TABLE streaks (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reader_token    TEXT NOT NULL,  -- anonymous token stored in browser
    episode_id      UUID NOT NULL REFERENCES episodes(id),
    marked_at       TIMESTAMPTZ DEFAULT now()
);
```

---

### Repetition Prevention

Before picking the next concept, the scheduler queries recent episodes to avoid repetition. This list is also passed to Claude as context so generated content doesn't re-explain things already covered.

```sql
-- Recent concepts per tag — passed to scheduler and Claude prompt
SELECT concept, tag, published_at
FROM episodes
WHERE status = 'published'
  AND tag = $1
ORDER BY published_at DESC
LIMIT 20;
```

---

### Concept Graph (Navigation)

Topics naturally chain (Maybe → Result → Railway-Oriented Programming). The `previous_episode_id`, `next_episode_id`, and `related_concepts` fields build a navigable concept graph over time. Readers can follow "what to read before this" and "what comes next" — turning isolated posts into a structured learning path.

---

## Build Order

1. **Claude prompt schema** — the JSON structure that drives everything
2. **.NET backend skeleton** — HangFire scheduler, PostgreSQL, config loader
3. **Ghost + Dev.to publishers** — first two publishing targets
4. **Next.js intervention dashboard** — draft queue UI
5. **YouTube Shorts assembly** — Playwright + FFmpeg pipeline
6. **Tag subscription + streak system** — reader-facing features
7. **Analytics feedback loop** — engagement → topic weight adjustment
8. **Reddit + remaining platforms** — complete distribution

---

## Key Decisions & Rationale

- **F# and C# for backend** — both are first-class. F# for domain logic, pipelines, schedulers, and data transformation; C# for publisher adapters, API integrations, and boilerplate-heavy code. Use whatever suits the task.
- **Ghost as canonical** — full control, SEO ownership, all other platforms point back
- **Batch API mode** — 50% cheaper since posts are scheduled, not realtime
- **Self-hosted Stable Diffusion** — eliminates image generation costs entirely if homelab has GPU
- **No TikTok (yet)** — API approval slow, dev audience thin; YouTube Shorts is the better bet
- **Tag subscriptions over full blog subscriptions** — keeps content relevant per reader, reduces unsubscribes
- **Streak mechanic** — converts casual readers into daily habits without being annoying

---

## Notes for Claude Code / AI Assistants

- **Languages are F# and C#** — both are primary. Default to F# for domain models, pipelines, schedulers, and pure logic. Default to C# for publisher adapters, third-party integrations, and anything with heavy boilerplate. Never force one where the other is clearly a better fit.
- All AI generation goes through **Claude API only** (not OpenAI for content)
- **pfstr-core is the canonical source** — DevPulse publishes to pfstr-core first, all other platforms use the thierrypfister.dev/blog URL as canonical link
- The intervention system must be **non-blocking** — if no action in 30 min, auto-publish proceeds
- Topic config is a **YAML file**, hot-reloadable without restart
- Every publisher is an **independent adapter** — adding a new platform = adding one new module

---

## Code Structure Principles

### Single Responsibility First

Files, modules, and functions should have one clear responsibility.

Prefer:
- small focused files
- isolated responsibilities
- explicit boundaries
- composable units

Avoid:
- god files
- mixed responsibilities
- large utility dumping grounds
- deeply nested logic

---

### File Size Guideline

Files should generally remain below 100 lines.

Going slightly above is acceptable when readability benefits from staying together, but large files should usually be split.

Large files are often a signal of mixed responsibilities, hidden abstractions, or poor separation of concerns.

This is a guideline, not a hard rule.

---

### Small Functions

Functions should:
- do one thing
- have clear inputs/outputs
- remain easy to scan quickly
- avoid excessive nesting
- avoid hidden side effects

Prefer extracting logic into named functions over large inline blocks.

---

### No Premature Abstraction

Do not create a shared module until you have 2+ real uses.
Do not create a helper until the logic appears in 2+ places.
Duplication is fine when the pattern is not yet stable.

---

### Cleanup Is Not Optional

Every resource acquired must be released.
Background jobs: cancel on shutdown.
DB connections: dispose after use.
HTTP clients: use typed clients or `IHttpClientFactory`.
A component that does not clean up is a bug waiting to happen.

---

## Functional Programming Practices

Functional patterns are encouraged when they improve readability, maintainability, correctness, or predictability.

Examples:
- pipes
- options / maybes
- immutable data
- pure functions
- discriminated unions
- function composition
- railway-oriented programming (Result<T>)

Do not force functional patterns where they reduce clarity.

Avoid:
- functional abstractions used only for cleverness
- unreadable composition chains
- theoretical purity over practical maintainability

Pragmatism is preferred over ideology.

---

## Naming Principles

Prefer:
- explicit names
- domain terminology
- intention-revealing identifiers

Avoid:
- abbreviations unless universally understood
- generic names like `Manager`, `Helper`, `Util`
- misleading or overloaded terminology

Names should explain purpose clearly.

---

## Development Workflow

### Think Before Coding

Before writing code:
- understand the problem
- identify the domain boundaries
- think through data flow
- identify responsibilities
- evaluate tradeoffs
- prefer simple solutions first

Avoid:
- coding immediately without design thought
- speculative abstractions
- premature optimization
- framework-driven architecture decisions

### Break Big Problems Into Small Ones

Before writing any code, state the problem.
Then break it into the smallest useful step.
Only implement that step.

If a task feels big, it needs to be broken down further.
Never implement two things at once if one depends on the other.

### Incremental Development

Build systems in small verified steps.

Prefer:
- vertical slices
- small iterations
- testable progress
- stable intermediate states

Avoid:
- massive untested rewrites
- large unstable branches
- unfinished architectural foundations

---

## Testing Workflow

### Test-Driven Development

TDD is the default workflow.

Preferred cycle:
1. Write failing test
2. Implement minimal solution
3. Make test pass
4. Refactor safely

Tests should drive implementation, not merely verify it afterward.

### Unit Tests First

Domain and business logic should primarily be verified through unit tests.

Unit tests should:
- test behavior
- remain fast
- isolate logic clearly
- avoid unnecessary infrastructure

### Integration Tests for Connected Flows

When behavior spans multiple components or function chains, introduce integration tests.

Examples:
- database interactions
- HangFire job execution
- publisher adapter end-to-end
- multi-step pipeline flows
- pfstr-core API integration

Integration tests should verify systems working together correctly.

### Test Readability Matters

Tests should clearly explain:
- what is being tested
- expected behavior
- failure conditions

Avoid:
- overly abstract test helpers
- unreadable test setups
- implementation-coupled tests

---

## Git Workflow

### Branch Strategy

Use feature branches for all work.

Examples:
- `feature/ghost-publisher`
- `feature/hangfire-scheduler`
- `feature/intervention-dashboard`
- `fix/telegram-webhook`
- `refactor/episode-pipeline`

The main branch should remain stable and deployable.

### Small Commits

Commits should remain focused, readable, and logically grouped.

Avoid combining refactors, formatting, features, and unrelated fixes in a single commit.

### Commit Style — Gitmoji

```
✨ feat: add Ghost publisher adapter
🐛 fix: resolve HangFire job retry loop
♻️ refactor: simplify episode status transitions
🧪 test: add integration tests for Claude generation
📝 docs: update pipeline flow diagram
🚀 chore: update docker compose configuration
```

Commit messages should explain intent, not only changed files.

---

## Session Protocol

At the start of every session:
1. Read `CLAUDE.md` (this file)
2. State what we are building this session in one sentence
3. Break it into the smallest first step
4. Implement only that step
5. Verify: build passes, tests pass
6. Commit with Gitmoji
7. Move to the next step

Never skip ahead.
Never implement two steps in one go without discussion.

---

## Definition of Done

A task is considered complete when:
- code is understandable
- tests pass
- naming is clear
- responsibilities are separated correctly
- no unnecessary complexity was introduced
- behavior is verified
- integration points are tested where needed
- the implementation matches the intended domain behavior
- the codebase remains maintainable
