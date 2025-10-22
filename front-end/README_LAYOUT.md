# Hood Board (Angular)

A minimal Angular app that recreates the layout from the screenshot: a banner header, a search bar, and a masonry pinboard of cards (including RTL examples).

## Run locally (Windows PowerShell)

```powershell
cd .\hood-board
npm start
```

Then open http://localhost:4200/ in your browser.

## Project quick notes

- Angular CLI 20, standalone components, zoneless + SSR enabled by the CLI (dev server runs CSR by default).
- Components:
  - `Header` – banner with title and tagline.
  - `SearchBar` – input + go button, emits `search` event.
  - `PostBoard` – responsive masonry layout using CSS columns.
  - `PostCard` – individual card with optional image and RTL support via `[dir]`.
- Styles: global variables in `src/styles.scss`; light SCSS per-component.

## Customize

- Add real data: replace the `posts` array in `src/app/components/post-board/post-board.ts` with your API data.
- Handle search: implement geocoding or filtering by handling `(search)` from `app-search-bar` in `src/app/app.ts`.
- Tweak columns: adjust `column-count` and breakpoints in `post-board.scss`.
