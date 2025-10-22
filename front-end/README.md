# Hood Board - Quick Start Guide

## 🚀 Getting Started in 3 Steps

### Step 1: Install Dependencies

```bash
npm install
```

### Step 2: Start Development Server

```bash
npm start
```

Your app will be available at `http://localhost:4200`

### Step 3: Generate API Models (when backend changes)

```bash
npm run api:gen
```

---

## 🐳 Docker Deployment

### Quick Docker Build & Run

```bash
npm run docker:full
```

### Development with Docker Compose

```bash
npm run docker:dev
```

### Individual Docker Commands

```bash
# Build Docker image
npm run docker:build

# Run Docker container
npm run docker:run

# Stop Docker services
npm run docker:stop
```

---

## 📋 Available Scripts

| Command                | Description                           |
| ---------------------- | ------------------------------------- |
| `npm start`            | Start development server              |
| `npm run build`        | Build for production                  |
| `npm run api:gen`      | Generate API models from Swagger      |
| `npm run docker:build` | Build Docker image                    |
| `npm run docker:run`   | Run Docker container                  |
| `npm run docker:dev`   | Start with Docker Compose             |
| `npm run docker:full`  | Build production + Docker image + run |

---

## 🔧 Prerequisites

- **Node.js 22+**
- **npm 10+**
- **Docker** (for containerized deployment)
- **Backend API** running on `http://localhost:8080`

---

## 📁 Key Files

- `src/app/app.ts` - Main application component
- `src/app/services/ads.service.ts` - API integration
- `src/app/models/generated/` - Auto-generated from Swagger
- `proxy.conf.json` - Development API proxy
- `Dockerfile` - Production container
- `docker-compose.yml` - Development environment

---

## 🎯 Features Overview

✅ **Marketplace Listings** - Browse, search, create, edit, delete  
✅ **User Authentication** - Secure user sessions  
✅ **Pagination** - 12 items per page for performance  
✅ **Comments System** - Interactive discussions  
✅ **Auto-Generated API** - Type-safe models from Swagger  
✅ **Docker Ready** - Production deployment containers

---

**Need help? Check `PROJECT_SUMMARY.md` for detailed documentation!**

---

## Original Angular CLI Information

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.6.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
