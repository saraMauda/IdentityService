## IdentityService (JWT HS256 + Refresh Tokens)

### Configuration (no secrets in git)

Set the shared secret via **User Secrets** (development) or **environment variables** (prod).

#### User Secrets (dev)

From `Backend/IdentityService`:

```powershell
dotnet user-secrets set "Jwt:Secret" "PUT_A_LONG_RANDOM_SECRET_HERE_AT_LEAST_32_CHARS"
dotnet user-secrets set "Jwt:Issuer" "IdentityService"
dotnet user-secrets set "Jwt:Audience" "Team2"
dotnet user-secrets set "Jwt:AccessTokenMinutes" "15"
dotnet user-secrets set "Jwt:RefreshTokenDays" "7"
```

#### Environment variables (example)

```powershell
$env:Jwt__Secret="PUT_A_LONG_RANDOM_SECRET_HERE_AT_LEAST_32_CHARS"
$env:Jwt__Issuer="IdentityService"
$env:Jwt__Audience="Team2"
```

### Database

Apply migrations:

```powershell
dotnet ef database update
```

### RabbitMQ (publishing user events)

Set via environment variables (recommended for secrets):

```powershell
$env:RabbitMq__Host="localhost"
$env:RabbitMq__Port="5672"
$env:RabbitMq__VirtualHost="/"
$env:RabbitMq__Username="guest"
$env:RabbitMq__Password="guest"
```

### Endpoints

- `POST /api/auth/register` → creates user and publishes `StudentRegisteredEvent` for student registrations
- `POST /api/auth/login` → issues access + refresh tokens
- `POST /api/auth/refresh` → rotates refresh token and issues a new pair
- `POST /api/auth/logout` → revokes refresh token

