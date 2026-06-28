# PokéTrainer AI — Angular Client Implementation Guide

This document is a step-by-step implementation guide for building the **Client side** of the **PokéTrainer AI** project using modern Angular.

The goal is not to build everything at once.  
The goal is to build the application professionally, layer by layer, while always thinking ahead about the next steps.

At every stage:

- Build only what belongs to the current stage.
- Keep the project runnable and clean.
- Avoid shortcuts that will create problems later.
- Think about loading, errors, empty states, and future features.
- Stop before moving to the next stage.

---

## Backend Assumptions

The backend is already implemented with **ASP.NET Core Web API** and includes:

- Authentication with JWT
- Register / Login / Me endpoints
- Pokémon catalog stored in SQL Server / MSSQL
- PokeAPI import handled only through the server
- Dream Team management up to 5 Pokémon
- Nicknames for team Pokémon
- Smart Search with Gemini and rule-based fallback
- Dream Team Analyzer
- Nickname Generator
- Import readiness endpoint
- Global error handling
- Handling for 503 / import not ready / database unavailable

The Angular client must **never call PokeAPI directly**.  
All Pokémon data must come from the backend API.

---

# Core Development Principles

## 1. Professional Angular Structure

The client will use a modern Angular architecture:

- Standalone Components
- App Routes
- Functional Guards
- Functional HTTP Interceptors
- Feature-based folder structure
- Shared UI components
- Core services
- Clear separation of responsibilities

The project should look like a real product codebase, not like a quick demo.

---

## 2. UX Before Code

Before building every screen, ask:

- What is the user trying to do?
- What is the happy path?
- What should happen while data is loading?
- What should happen when there is no data?
- What should happen if the server returns an error?
- How can this interaction feel clear, polished, and fun?

---

## 3. Performance First

The UI should feel fast and responsive.

Important decisions:

- Use pagination or lazy loading for the Pokémon catalog.
- Do not load full Pokémon details for every card.
- Load details only when entering the details screen.
- Use image fallback handling.
- Avoid unnecessary repeated API calls.
- Keep animations lightweight.
- Prefer CSS animations over heavy UI libraries.

---

## 4. Wow Experience

The system should feel like an experience for Pokémon Trainers, not a basic admin panel.

Key UX ideas:

- Beautiful Pokémon cards
- Dream Team displayed as 5 visual slots
- Natural-language Smart Search
- AI-powered Team Analyzer
- 3D rotating Poké Ball loader
- Smooth hover effects
- Add-to-team animation
- Friendly empty and error states
- Type-based badges and glow effects

The UI should be professional, clean, impressive, and fun.

---

# Planned Folder Structure

```text
src/app
│
├── core
│   ├── auth
│   │   ├── auth.service.ts
│   │   ├── auth.guard.ts
│   │   ├── guest.guard.ts
│   │   └── token.storage.ts
│   │
│   ├── http
│   │   ├── auth.interceptor.ts
│   │   ├── error.interceptor.ts
│   │   └── api-error.model.ts
│   │
│   ├── layout
│   │   ├── app-shell.component.ts
│   │   ├── top-nav.component.ts
│   │   └── loading-bar.component.ts
│   │
│   └── config
│       └── api.config.ts
│
├── shared
│   ├── components
│   │   ├── pokemon-card
│   │   ├── empty-state
│   │   ├── error-state
│   │   ├── loading-spinner
│   │   ├── pokeball-loader
│   │   └── type-badge
│   │
│   ├── models
│   │   ├── pokemon.model.ts
│   │   ├── dream-team.model.ts
│   │   ├── auth.model.ts
│   │   ├── paged-result.model.ts
│   │   └── api-error.model.ts
│   │
│   └── utils
│
├── features
│   ├── auth
│   │   ├── login
│   │   └── register
│   │
│   ├── dashboard
│   │
│   ├── pokemon-catalog
│   │   ├── catalog-page.component.ts
│   │   ├── catalog-filter.component.ts
│   │   └── pokemon-catalog.service.ts
│   │
│   ├── pokemon-details
│   │   └── pokemon-details-page.component.ts
│   │
│   ├── smart-search
│   │   ├── smart-search-page.component.ts
│   │   └── smart-search.service.ts
│   │
│   ├── dream-team
│   │   ├── dream-team-page.component.ts
│   │   ├── dream-team-card.component.ts
│   │   ├── nickname-editor.component.ts
│   │   └── dream-team.service.ts
│   │
│   └── team-analyzer
│       ├── team-analyzer-page.component.ts
│       └── team-analyzer.service.ts
│
├── app.routes.ts
├── app.config.ts
└── app.component.ts
```

---

# Planned Routes

```text
/login
/register

/app/dashboard
/app/catalog
/app/pokemon/:id
/app/smart-search
/app/team
/app/analyzer

/**
```

## Route Behavior

- `/login` and `/register` are available for guests.
- `/app/**` is protected by an Auth Guard.
- A logged-in user who enters `/login` should be redirected to `/app/dashboard`.
- A guest user who enters a protected route should be redirected to `/login`.
- Unknown routes should redirect to a Not Found screen or to `/login`.

---

# Stage 1 — Create the Angular Project

## Goal

Create a clean modern Angular project with routing and SCSS.

## What to do

```bash
ng new poke-trainer-client
```

Recommended choices:

```text
Routing: Yes
Styles: SCSS
SSR: No for the first stage
```

Then run:

```bash
cd poke-trainer-client
ng serve
```

## What to verify

- The Angular app starts successfully.
- The browser opens the default Angular screen.
- There are no build errors.
- The project structure is clean.

## Thinking ahead

Do not start building screens directly inside `app.component`.

This project will need:

- Protected routes
- HTTP interceptors
- Auth state
- Shared components
- Feature folders
- Global styles
- Error handling

So the first step is only project creation.

## Final result of this stage

A clean Angular app that runs successfully.

---

# Stage 2 — Create the Initial Folder Structure

## Goal

Create a professional folder structure before writing business logic.

## What to do

Create these main folders:

```text
core
shared
features
```

Then create:

```text
core/auth
core/http
core/layout
core/config

shared/components
shared/models
shared/utils

features/auth/login
features/auth/register
features/dashboard
features/pokemon-catalog
features/pokemon-details
features/smart-search
features/dream-team
features/team-analyzer
```

## Why this matters

If we start coding too quickly, the project becomes messy.

This structure creates clear boundaries:

- `core` — global services and infrastructure
- `shared` — reusable UI and models
- `features` — business screens

## Thinking ahead

- Auth will be used across the app.
- HTTP interceptors must be registered globally.
- Pokémon Card will be reused in Catalog, Smart Search, and Dream Team.
- Loading, Error, and Empty states will be reused everywhere.

## Final result of this stage

A clean folder skeleton ready for development.

---

# Stage 3 — App Routes and Main Layout

## Goal

Create the main navigation structure without business logic yet.

## Initial route structure

```ts
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login-page.component')
        .then(m => m.LoginPageComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register-page.component')
        .then(m => m.RegisterPageComponent)
  },
  {
    path: 'app',
    component: AppShellComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component')
            .then(m => m.DashboardPageComponent)
      },
      {
        path: 'catalog',
        loadComponent: () =>
          import('./features/pokemon-catalog/catalog-page.component')
            .then(m => m.CatalogPageComponent)
      },
      {
        path: 'pokemon/:id',
        loadComponent: () =>
          import('./features/pokemon-details/pokemon-details-page.component')
            .then(m => m.PokemonDetailsPageComponent)
      },
      {
        path: 'smart-search',
        loadComponent: () =>
          import('./features/smart-search/smart-search-page.component')
            .then(m => m.SmartSearchPageComponent)
      },
      {
        path: 'team',
        loadComponent: () =>
          import('./features/dream-team/dream-team-page.component')
            .then(m => m.DreamTeamPageComponent)
      },
      {
        path: 'analyzer',
        loadComponent: () =>
          import('./features/team-analyzer/team-analyzer-page.component')
            .then(m => m.TeamAnalyzerPageComponent)
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      }
    ]
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
```

## Planned layout

`AppShellComponent` should contain:

- Top navigation
- Main content container
- Router outlet
- Optional global loading bar later

## Thinking ahead

Do not duplicate the navigation bar in every page.

All protected screens should be wrapped by the main shell.

Later, the `/app` route will receive the `authGuard`.

## Final result of this stage

Navigation between placeholder screens works.

---

# Stage 4 — Global Design System

## Goal

Create a consistent visual language before building the real screens.

## What to define

In the global SCSS files, define:

- Colors
- Backgrounds
- Card styles
- Buttons
- Forms
- Badges
- Spacing
- Shadows
- Basic animations

## Suggested design tokens

```scss
:root {
  --color-bg: #f6f8ff;
  --color-surface: #ffffff;
  --color-primary: #4f46e5;
  --color-primary-soft: #eef2ff;
  --color-accent: #f59e0b;
  --color-danger: #ef4444;
  --color-success: #22c55e;
  --color-text: #172033;
  --color-muted: #64748b;
  --radius-lg: 24px;
  --radius-md: 16px;
  --shadow-card: 0 18px 40px rgba(15, 23, 42, 0.12);
}
```

## Basic hover effect

```scss
.card-hover {
  transition: transform 180ms ease, box-shadow 180ms ease;
}

.card-hover:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-card);
}
```

## Thinking ahead

The design system must support:

- Pokémon cards
- Auth cards
- Dream Team slots
- Analyzer reports
- Loading states
- Error states

Do not design every screen separately without a shared visual language.

## Final result of this stage

The application has a consistent global design foundation.

---

# Stage 5 — Shared UI Components

## Goal

Build small reusable UI components that will be used across the app.

## Components to create

### 1. Poké Ball Loader

A polished loading component:

- Rotating Poké Ball
- CSS 3D rotate effect
- Optional context text

Example loading messages:

```text
Preparing your Pokédex...
AI Trainer is thinking...
Analyzing your Dream Team...
Searching for the perfect Pokémon...
```

Used in:

- Catalog loading
- Smart Search
- Team Analyzer
- Import readiness states

### 2. Empty State

Used when there is no data:

- No Dream Team yet
- No search results
- Catalog is empty
- Import is not ready

### 3. Error State

Used for friendly errors:

- 503
- Database unavailable
- Unauthorized
- Smart Search unavailable
- Network unavailable

### 4. Type Badge

Displays Pokémon type:

```text
Fire
Water
Electric
Grass
```

### 5. Pokémon Card

Reusable Pokémon card component.

It receives a Pokémon as input and emits actions:

- `viewDetails`
- `addToTeam`
- `removeFromTeam`

## Thinking ahead

`PokemonCardComponent` should not know where it is used.

It should work in:

- Catalog
- Smart Search
- Dream Team
- Details recommendations later

## Final result of this stage

Reusable UI building blocks are ready.

---

# Stage 6 — Client Models and API Contracts

## Goal

Define TypeScript interfaces based on backend responses.

## Recommended models

```ts
export interface PokemonListItem {
  id: number;
  name: string;
  imageUrl?: string | null;
  types: string[];
  hp?: number;
  attack?: number;
  defense?: number;
  speed?: number;
}
```

```ts
export interface PokemonDetails extends PokemonListItem {
  height?: number;
  weight?: number;
  abilities?: string[];
  description?: string | null;
}
```

```ts
export interface DreamTeamItem {
  id: number;
  pokemonId: number;
  pokemonName: string;
  imageUrl?: string | null;
  nickname?: string | null;
  types: string[];
}
```

```ts
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

```ts
export interface ApiError {
  status: number;
  message: string;
  code?: string;
  details?: string;
}
```

## Thinking ahead

Do not use `any`.

Every service should return typed data.

If the backend DTO changes, update the model in one place.

## Final result of this stage

Typed client-side contracts are ready.

---

# Stage 7 — Authentication Client

## Goal

Implement Login, Register, token storage, and basic authentication state.

## Files to create

```text
auth.service.ts
token.storage.ts
auth.guard.ts
guest.guard.ts
login-page.component.ts
register-page.component.ts
```

## AuthService methods

```ts
login(request)
register(request)
me()
logout()
isAuthenticated()
```

## Token storage

For the first implementation, use `localStorage`.

Later, if needed, consider a more secure cookie-based approach from the server side.

## Login screen states

- Initial
- Loading
- Validation error
- Server error
- Success redirect

## Register screen states

- Initial
- Loading
- Validation error
- Email already exists
- Success redirect

## Thinking ahead

The JWT token will be required by all protected API calls.

Do not manually add the `Authorization` header in every service.  
This will be handled later by the Auth Interceptor.

## Final result of this stage

The user can register, login, store a token, and access protected screens.

---

# Stage 8 — HTTP Interceptors and Global Error Handling

## Goal

Handle JWT and API errors in one central place.

## Auth Interceptor

Adds:

```text
Authorization: Bearer <token>
```

to API requests.

## Error Interceptor

Handles:

### 401 Unauthorized

- Clear token
- Logout
- Redirect to login
- Show friendly message:
  `Session expired. Please login again.`

### 403 Forbidden

Show:
`You do not have permission to perform this action.`

### 503 Import Not Ready

Show:
`The Pokédex is still being prepared. Please try again soon.`

### 503 Database Unavailable

Show:
`We are having trouble reaching your trainer data. Your team is safe.`

### 0 / Network Error

Show:
`Cannot reach the server right now. Please check your connection or try again later.`

## Thinking ahead

Every screen should receive clean and predictable errors.

Avoid duplicating low-level error logic inside each feature component.

## Final result of this stage

JWT and error handling are centralized.

---

# Stage 9 — Dashboard

## Goal

Build a friendly landing page after login.

## Screen content

- Trainer greeting
- Dream Team status
- Quick actions:
  - Explore Pokémon
  - Smart Search
  - Manage Dream Team
  - Analyze Team
- Optional import readiness message

## Suggested UX

```text
Welcome back, Trainer!

Dream Team:
3 / 5 Pokémon selected

[Explore Pokémon]
[Smart Search]
[Manage Dream Team]
[Analyze Team]
```

## Thinking ahead

The Dashboard should not be too heavy.

It should give the user context and guide them to the main actions.

Possible future improvements:

- Daily recommended Pokémon
- Team score
- Last smart search
- Suggested improvement

## Final result of this stage

A polished dashboard is available after login.

---

# Stage 10 — Pokémon Catalog

## Goal

Display all Pokémon in a fast and attractive way.

## Features

- Load Pokémon list from backend
- Pagination
- Text search by name
- Filter by type
- Sorting
- Pokémon Card grid
- Add to Dream Team
- View Details

## Screen states

- Loading
- Loaded
- Empty
- Error
- Import not ready
- Database unavailable

## Thinking ahead

The Catalog must be efficient:

- Do not load full details for every Pokémon.
- Use a lightweight list DTO.
- Add image fallback.
- Add-to-team should update the UI without a full page refresh.

## Final result of this stage

A working and polished Pokémon Catalog.

---

# Stage 11 — Pokémon Details

## Goal

Display deeper information about one Pokémon.

## Content

- Large image
- Name
- Types
- Stats
- Abilities
- Height / Weight
- Description
- Add to Dream Team
- If already in team, show current team state

## Thinking ahead

The Details screen can be reached from:

- Catalog
- Smart Search
- Dream Team

Using a route by ID is better than using only a modal.

## Final result of this stage

A clean details screen for a single Pokémon.

---

# Stage 12 — Dream Team Management

## Goal

Allow the trainer to manage a team of up to 5 Pokémon.

## Features

- Display 5 visual slots
- Add Pokémon
- Remove Pokémon
- Update nickname
- Generate nicknames
- View details
- Analyze team

## Suggested UI

```text
Dream Team
3 / 5 selected

[Slot 1] Pikachu   Nickname: Sparky
[Slot 2] Charizard Nickname: Blaze
[Slot 3] Empty
[Slot 4] Empty
[Slot 5] Empty
```

## Rules

- Maximum 5 Pokémon.
- The client should prevent invalid actions in the UI.
- The server remains the source of truth.
- If the server returns Team Full, show a friendly message.

## Thinking ahead

Dream Team is the core feature of the product.

Components from this area will also support the Analyzer screen.

## Final result of this stage

Full Dream Team management is available on the client side.

---

# Stage 13 — Nickname Generator

## Goal

Add a fun and personal AI feature to the team experience.

## Features

- Generate nickname suggestions for a specific Pokémon
- Display suggestions
- Select a suggestion with one click
- Save the selected nickname to the backend

## Suggested UI

```text
Nickname: [Sparky]
[Generate Suggestions]

Suggestions:
- Sparky
- VoltBuddy
- ThunderKid
```

## Thinking ahead

Nickname Generator should be an internal component, not a standalone page.

It is mainly used inside Dream Team.

## Final result of this stage

The user can generate and save nickname suggestions.

---

# Stage 14 — Smart Search

## Goal

Build the main Wow feature of the application.

## User input examples

```text
I want a fast electric Pokémon that is strong against water
```

```text
Find me a strong fire Pokémon with high attack
```

The client sends the natural-language query to the backend.  
The backend decides whether to use Gemini or the rule-based fallback.

## UI

- Large textarea
- Suggested prompt chips
- Search button
- Impressive loading state:
  - `AI Trainer is thinking...`
  - rotating Poké Ball
- Results as Pokémon cards
- AI or fallback explanation
- Add to Team
- View Details

## Screen states

- Initial
- Loading
- Results
- Empty
- AI fallback
- Error

## Thinking ahead

AI should feel like part of the product, not like a technical add-on.

This screen should be especially polished.

## Final result of this stage

Smart Search works with a strong Wow experience.

---

# Stage 15 — Team Analyzer

## Goal

Give the trainer insights about the Dream Team.

## Features

- Call the backend analyzer endpoint
- Display:
  - Strengths
  - Weaknesses
  - Type coverage
  - Suggestions
  - Overall summary
- Suggest improvements
- Navigate to Catalog based on recommendations

## UI

- Report card
- Score or progress indicator
- Type tags
- Strength and weakness lists
- CTA:
  - Improve Team

## Thinking ahead

Analyzer connects:

- Dream Team
- AI
- Pokémon Catalog

This is another strong place for a Wow effect.

## Final result of this stage

A polished Analyzer screen with clear insights.

---

# Stage 16 — Image Fallback Handling

## Goal

Make sure the UI never breaks if a Pokémon image is missing or unavailable.

## What to do

In Pokémon Card and Details:

- Add fallback image
- Add `onError` handler
- Add placeholder silhouette
- Add image skeleton loading

## Example

```html
<img
  [src]="pokemon.imageUrl || fallbackImage"
  (error)="onImageError($event)"
  [alt]="pokemon.name"
/>
```

## Thinking ahead

Images are central to the UX.

If images fail, the product should still look polished.

## Final result of this stage

No broken images appear in the UI.

---

# Stage 17 — Complete Loading / Error / Empty States

## Goal

Polish all non-happy-path states.

## Screens to test

- Login
- Register
- Dashboard
- Catalog
- Details
- Smart Search
- Dream Team
- Analyzer

## Error states to support

```text
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
503 Import Not Ready
503 DB Unavailable
0 Network Error
```

## Friendly messages

### Import not ready

```text
The Pokédex is still being prepared.
Please try again soon.
```

### Database unavailable

```text
We are having trouble reaching your trainer data.
Your team is safe. Please try again soon.
```

### Network Error

```text
Cannot reach the server right now.
Please check your connection or try again later.
```

## Thinking ahead

This is an important part of the assignment because edge cases were explicitly requested.

## Final result of this stage

No screen breaks or displays raw technical errors.

---

# Stage 18 — Polish and Animations

## Goal

Add a professional finishing layer.

## Ideas

- Page transitions
- Card hover effects
- Add-to-team animation
- Dream Team slot entrance animation
- Analyzer report reveal animation
- Poké Ball loader
- Subtle gradients
- Decorative background shapes
- Type-based glow effects

## Important limitation

Do not overload the application.

- Do not add heavy libraries without a real need.
- Do not create animations that slow down the Catalog.
- Prefer simple CSS animations.
- Performance comes before effects.

## Final result of this stage

The UI feels impressive but remains fast.

---

# Stage 19 — Manual Testing and Demo Flow

## Goal

Prepare the project for presentation.

## Recommended demo flow

1. Register
2. Login
3. Enter Dashboard
4. Open Catalog
5. Search for a Pokémon
6. Open Pokémon Details
7. Add Pokémon to Dream Team
8. Edit nickname
9. Generate nicknames
10. Use Smart Search:
    - `Find me a fast electric Pokémon`
11. Add a Pokémon from the results
12. Run Team Analyzer
13. Show edge-case handling:
    - Import not ready
    - Server unavailable
    - Empty result
    - Image fallback

## Thinking ahead

A good demo is not only about code.

You need to explain the story:

- Data comes from the server only.
- Auth is handled with JWT.
- Dream Team is stored in the database.
- AI is used, but there is a fallback.
- Edge cases are handled.
- UX is polished.

## Final result of this stage

A clear and impressive demo flow.

---

# Stage 20 — Technical Explanation for Interview

## Goal

Be ready to explain every major decision.

## Topics to explain

### Why Angular?

- Strong SPA framework
- Built-in routing
- Powerful forms
- Enterprise-friendly structure
- Suitable for protected dashboards

### Why does the server import from PokeAPI?

- The client does not depend on a third-party API.
- API keys and integration logic stay on the server.
- Better control over performance.
- Easier caching and persistence.
- Better error handling.
- Matches the assignment requirement.

### Why JWT?

- Works well with SPA applications.
- Easy to attach using an interceptor.
- Supports protected routes.

### Why Smart Search on the server?

- Gemini API key is not exposed to the client.
- The backend can provide rule-based fallback.
- Business logic remains centralized.
- Performance and errors are easier to control.

### Why feature-based structure?

- Clear separation of responsibilities.
- Easier maintenance.
- Easier future extension.
- Professional Angular architecture.

## Final result of this stage

You can explain the project clearly and confidently.

---

# Recommended Execution Order

Work with me in this order:

```text
Stage 1 — Create Angular app
Stage 2 — Folder structure
Stage 3 — Routes + Layout
Stage 4 — Basic Design System
Stage 5 — Shared Components
Stage 6 — Models
Stage 7 — Auth
Stage 8 — Interceptors
Stage 9 — Dashboard
Stage 10 — Catalog
Stage 11 — Details
Stage 12 — Dream Team
Stage 13 — Nicknames
Stage 14 — Smart Search
Stage 15 — Analyzer
Stage 16 — Image fallback
Stage 17 — Error states
Stage 18 — Polish
Stage 19 — Demo flow
Stage 20 — Interview explanation
```

---

# How We Will Work

For every stage:

1. Define the goal.
2. Explain why it matters.
3. Write the code.
4. Explain what was written.
5. Run and verify.
6. Stop before moving to the next stage.
7. Make sure the next stages were considered.

---

# Next Step

Start with **Stage 1 — Create the Angular Project**.

Only after the project is created and running successfully, move to the folder structure stage.
