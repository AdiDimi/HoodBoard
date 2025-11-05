# Product Catalog – Frontend Plan

This document outlines the architecture and design decisions used to implement the Product Catalog UI that pairs with the .NET 7+ API (see swagger at http://localhost:8080/swagger/v1/swagger.json).

## Architecture

- Angular standalone components with signals for lightweight state.
- HTTP integration via a thin `ProductsService` that calls the REST endpoints:
  - GET /api/products
  - GET /api/products/{id}
  - POST /api/products
  - PUT /api/products/{id}
  - DELETE /api/products/{id}
  - GET /api/products/export (Blob XLSX)
- UI layout components:
  - `Header` with Add Product and Export buttons
  - `ProductBoardComponent` – list, filter, paginate
  - `ProductCardComponent` – single card view
  - `ProductFormModalComponent` – Add/Edit (Reactive Forms + validation)
- Pagination: page-size 12; derives meta if backend does not include it.

## Data model (frontend)

```ts
interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  stock: number;
  category: string;
  imageUrl: string;
}
```

## Validation

- Name required, max 120
- Description max 1000
- Price > 0
- Stock ≥ 0
- Category required
- Image URL required

## UI/UX + Responsiveness

- Grid uses CSS Grid with rem units.
- Breakpoints: desktop 4 cards, tablet 3, mobile 2.
- Stock indicators: In Stock, Low Stock (< 5), Out of Stock.
- All paddings, radii, borders converted to rem where edited/added.

## Assumptions

- API may return either an array of products or `{ data, meta }`. The UI normalizes both, deriving meta as needed.
- Search is driven by the header search bar and the board refetches on query changes.
- Category list is derived from loaded products when no reference data endpoint exists.

## Future improvements

- Server-driven sorting/pagination with query params once backend supports it.
- Inline editing on cards for small edits (price/stock).
- Image upload and thumbnailing service.
- E2E tests with Playwright; unit tests for service and form validators.
