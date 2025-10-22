# API Code Generation

This project uses OpenAPI Generator to automatically generate TypeScript models and services from the Swagger API definition.

## Setup

The API generation is already configured in the project. The generated code will be placed in `src/app/models/generated/`.

## Usage

### Generate API models and services

```bash
npm run api:gen
```

This command will:

1. Fetch the OpenAPI/Swagger specification from `http://host.docker.internal:8080/swagger/v1/swagger.json`
2. Generate TypeScript interfaces for all API models
3. Generate Angular services for all API endpoints
4. Place the generated files in `src/app/models/generated/`

### Prerequisites

Before running the generation command:

1. Make sure your backend API server is running on `http://localhost:8080` (Docker will access it via `host.docker.internal`)
2. Ensure the Swagger JSON endpoint is accessible at `/swagger/v1/swagger.json`

### Generated Files Structure

```
src/app/models/generated/
├── models/          # TypeScript interfaces for API models
├── services/        # Angular services for API endpoints
├── api.module.ts    # Angular module configuration
└── configuration.ts # API configuration
```

### Using Generated Models

After generation, you can import and use the generated models in your services:

```typescript
import { Ad, Comment, Location } from '../models/generated/models';
import { AdsService } from '../models/generated/services';
```

### Configuration

The generation process is configured in:

- `package.json` - Contains the `api:gen` script
- `openapitools.json` - Contains OpenAPI generator configuration

## Updating Models

Whenever the backend API changes:

1. Run `npm run api:gen` to regenerate the models
2. Update your services to use the new generated interfaces
3. Test your application to ensure compatibility

## Current Status

✅ **Setup Complete**: The API generation capability has been successfully configured and tested.

🔧 **Configuration**:

- Docker integration enabled (works around Java 11+ requirement)
- OpenAPI Generator CLI v7.10.0 installed
- TypeScript Angular generator configured
- Proper directory structure and .gitignore rules in place

⏳ **Ready to Use**: The setup is fully functional and ready to generate models when your backend API server is running.

## Troubleshooting

If the generation fails:

1. **Backend Server**: Verify the backend server is running on `http://localhost:8080`
2. **Swagger Endpoint**: Check that `/swagger/v1/swagger.json` returns valid OpenAPI specification
3. **Network**: Ensure no firewall is blocking access to localhost:8080
4. **Docker**: Ensure Docker is running (required for Java compatibility)

### Common Issues:

- **"Connection refused"**: Backend server is not running, or Docker networking issue (solved by using `host.docker.internal`)
- **"Java version"**: Use the Docker integration (already configured with `useDocker: true`)
- **"Invalid spec"**: Check your Swagger JSON format at the endpoint

## ✅ **Status: Successfully Configured**

The API generation is working! Generated files include:

- **Models**: `Ad`, `Comment`, `Contact`, `Location`, `Photo`, DTOs
- **Services**: Complete Angular service with all API endpoints
- **Types**: Full TypeScript type safety
