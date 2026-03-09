# Estapar Parking Challenge

Backend para o desafio técnico da **Estapar**, implementado em **ASP.NET Core 10** com foco em código limpo, arquitetura extensível e boas práticas de engenharia.

A API gerencia o ciclo completo de estacionamento — entrada, parada e saída de veículos — com precificação dinâmica baseada na taxa de ocupação, webhook com validação de `webhook secret` via header, idempotência de eventos e cálculo de receita por setor.

---

## Índice

- [Stack Tecnológico](#stack-tecnológico)
- [Arquitetura](#arquitetura)
- [Endpoints da API](#endpoints-da-api)
- [Regras de Negócio](#regras-de-negócio)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Como Executar](#como-executar)
- [Docker](#docker)
- [Simulador](#simulador)
- [Testes](#testes)
- [Configuração](#configuração)
- [Decisões Técnicas](#decisões-técnicas)

---

## Stack Tecnológico

| Componente        | Tecnologia                                       |
| ----------------- | ------------------------------------------------ |
| Runtime           | .NET 10 / ASP.NET Core 10                        |
| ORM               | Entity Framework Core 10                         |
| Banco de Dados    | PostgreSQL 14 **ou** SQL Server (multi-provider) |
| Cache             | Redis (opcional, fallback para in-memory)         |
| Autenticação      | JWT Bearer (HS256)                               |
| Documentação API  | Scalar + OpenAPI 3                               |
| Logging           | Serilog + New Relic                              |
| Testes            | MSTest + WebApplicationFactory                   |
| Containerização   | Docker multi-stage (Alpine) + NGINX LB           |

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                        NGINX (LB)                           │
│                     Round-robin × 3                         │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    ASP.NET Core 10                           │
│                                                             │
│  Filters                                                    │
│  ├── HandleExceptionFilter (ApiException → 422, 500)        │
│  └── RequireWebhookSignatureAttribute (secret no header)    │
│                                                             │
│  Controllers                                                │
│  ├── HealthController       GET  /api/health                │
│  ├── WebhookController      POST /api/webhook               │
│  └── RevenueController      GET  /api/revenue               │
│                                                             │
│  Services                                                   │
│  ├── WebhookProcessingService  (ciclo Entry→Parked→Exit)    │
│  ├── ParkingService            (cálculo de receita)         │
│  ├── ParkingPricingService     (multiplicador + cobrança)   │
│  ├── GarageSyncService         (sync garagem/simulador)     │
│  ├── SimulatorClientService    (HTTP client + cache)        │
│  ├── ApplicationService        (JWT token generation)       │
│  └── CacheService              (Redis / in-memory)          │
│                                                             │
│  DataStorage                                                │
│  ├── IDatabaseProvider → PostgresDatabaseProvider            │
│  │                     → SqlServerDatabaseProvider           │
│  └── AppDbContext (abstract) → PostgresDbContext             │
│                              → SqlServerDbContext            │
└──────────────────┬──────────────────┬───────────────────────┘
                   │                  │
            ┌──────▼──────┐    ┌──────▼──────┐
            │ PostgreSQL  │    │    Redis     │
            │   14-alpine │    │    (opt.)    │
            └─────────────┘    └─────────────┘
```

---

## Endpoints da API

### `GET /api/health`

Health check para probes de Kubernetes/Docker.

```json
{ "status": "ok", "timestamp": "2026-03-09T10:00:00.000Z" }
```

### `POST /api/webhook`

Recebe eventos do simulador de estacionamento. Protegido por validação de `webhook secret` em header (configurável).

**Request:**

```json
{
  "license_plate": "ZUL0001",
  "entry_time": "2026-01-01T10:00:00.000Z",
  "exit_time": "2026-01-01T12:00:00.000Z",
  "lat": -23.561684,
  "lng": -46.655981,
  "event_type": "ENTRY"
}
```

| Campo           | Tipo     | Obrigatório | Descrição                                |
| --------------- | -------- | ----------- | ---------------------------------------- |
| `license_plate` | string   | Sim         | Placa do veículo                         |
| `event_type`    | string   | Sim         | `ENTRY`, `PARKED` ou `EXIT`              |
| `entry_time`    | datetime | Não         | Horário de entrada (padrão: UTC agora)   |
| `exit_time`     | datetime | Não         | Horário de saída                         |
| `lat`           | decimal  | Não         | Latitude da vaga (obrigatório p/ PARKED) |
| `lng`           | decimal  | Não         | Longitude da vaga (obrigatório p/ PARKED)|

**Response:** `200 OK` (body vazio)

### `GET /api/revenue?date={date}&sector={sector}`

Retorna a receita total de um setor em uma data específica.

```json
{ "amount": 27.00, "currency": "BRL", "timestamp": "2026-01-01T12:05:30.123Z" }
```

---

## Regras de Negócio

### Ciclo de Vida do Estacionamento

```
ENTRY → PARKED → EXIT
```

1. **Entry** — Cria sessão de estacionamento se houver capacidade. Calcula o multiplicador de preço baseado na ocupação no momento da entrada.
2. **Parked** — Associa o veículo a uma vaga específica (lat/lng). Marca a vaga como ocupada.
3. **Exit** — Encerra a sessão, libera a vaga e calcula o valor cobrado.

### Precificação Dinâmica

O preço é ajustado por um multiplicador baseado na taxa de ocupação da garagem **no momento da entrada**:

| Ocupação   | Multiplicador |
| ---------- | ------------- |
| < 25%      | 0.90×         |
| 25% – 50%  | 1.00×         |
| 50% – 75%  | 1.10×         |
| > 75%      | 1.25×         |

**Fórmula de cobrança:**

```
valor = preço_base × ⌈horas⌉ × multiplicador
```

- Primeiros **30 minutos** são gratuitos
- Horas são arredondadas **para cima** (ceiling)
- Resultado arredondado para 2 casas decimais

**Exemplo:** Entrada 10:00, Saída 12:15, preço base R$10, ocupação 10%
- Duração: 2h15 → ⌈2.25⌉ = 3 horas
- Valor: 10 × 3 × 0.90 = **R$ 27,00**

### Idempotência

Cada evento de webhook gera uma chave de idempotência (`{placa}|{tipo}|{timestamp}|{lat}|{lng}`). Eventos duplicados são detectados e ignorados, garantindo processamento seguro em cenários de retry.

---

## Estrutura do Projeto

```
EstaparParkingChallenge.sln
│
├── Api/                          # Contratos compartilhados
│   ├── Enums.cs                  # ParkingEventType, ErrorCodes
│   ├── ErrorModel.cs             # Payload de erro padronizado
│   ├── ApiConstants.cs           # Constantes (roles, claims)
│   ├── DataAnnotations/          # CodeAttribute (enum → string)
│   ├── Paginated/                # PaginatedSearchParams/Response
│   └── Parking/                  # WebhookEventRequest, RevenueResponse
│
├── Site/                         # Web API principal
│   ├── Program.cs                # Pipeline de startup
│   ├── Controllers/              # Health, Webhook, Revenue
│   ├── Services/                 # Lógica de negócio
│   ├── Configuration/            # Binding de configuração
│   ├── DataStorage/              # Multi-provider EF (Postgres/SqlServer)
│   ├── Entities/                 # DbContext + entidades
│   ├── Filters/                  # Exception handling, webhook signature
│   ├── Classes/                  # ApiException, EnumEncoding
│   └── Migrations/               # Postgres/ e SqlServer/
│
├── Tests/                        # Testes automatizados
│   ├── Unit/                     # Testes unitários (PricingService)
│   └── Integration/              # Testes de integração (WebApplicationFactory)
│
├── nginx/                        # Load balancer config
├── docker-compose.yml            # Stack básica (web + postgres + redis)
└── docker-compose-load-balancer.yml  # Produção (3× instâncias + NGINX)
```

---

## Como Executar

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 14+ **ou** SQL Server
- (Opcional) Docker e Docker Compose

### Setup local

```bash
# 1. Restaurar dependências
dotnet restore EstaparParkingChallenge.sln

# 2. Instalar EF CLI (se necessário)
dotnet tool install --global dotnet-ef

# 3. Aplicar migrations (PostgreSQL)
dotnet ef database update \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context PostgresDbContext

# 4. Executar a API
dotnet run --project Site/EstaparParkingChallenge.Site.csproj -lp EstaparParkingChallenge.Site
```

> Ou defina `"ApplyMigrations": true` em `appsettings` para aplicar migrations automaticamente na inicialização.

**URLs de desenvolvimento:**

| Recurso           | URL                                     |
| ------------------ | --------------------------------------- |
| API                | `https://localhost:7139`                |
| Dashboard (Site)   | `http://localhost:5139`                 |
| Scalar (API docs)  | `http://localhost:5139/scalar/v1`       |
| OpenAPI spec       | `http://localhost:5139/openapi/v1.json` |
| Health check       | `GET /api/health`                       |
| Parking state      | `GET /api/parking/state`                |
| Parking sync       | `POST /api/parking/sync`                |

### Gerar token JWT (aplicação)

```bash
dotnet run --project Site/EstaparParkingChallenge.Site.csproj -- --gen-app-token -n MinhaAplicacao
# Com validade customizada (dias):
dotnet run --project Site/EstaparParkingChallenge.Site.csproj -- --gen-app-token -n MinhaAplicacao -v 365
```

---

## Docker

### Stack Site + Simulador (recomendada para demo funcional)

```bash
docker compose -f docker-compose.site-simulator.yml up --build
```

Sobe: **site** (API + dashboard), **simulator** (UI interativa + envio de eventos), **postgres** e **redis**.

URLs:

- Site: `http://localhost:8080`
- Simulator: `http://localhost:8081`

Com essa stack voce consegue:

- Visualizar a topologia e estado atual do estacionamento no Site.
- Simular eventos de entrada/estacionamento/saida pelo Simulator.
- Trocar perfil de topologia no Simulator e validar sincronizacao no Site.
- Consultar receita por setor/data durante os testes.

### Stack básica

```bash
docker-compose up --build
# API: http://localhost:8080
```

Sobe: **web** (API) + **postgres** (banco) + **redis** (cache)

### Com load balancer (produção)

```bash
docker compose -f docker-compose-load-balancer.yml up --build --scale web=3
```

Sobe: **3 instâncias web** + **NGINX** (round-robin na porta 8080) + **postgres** + **redis**

A imagem Docker usa **multi-stage build** com Alpine para mínimo footprint.

---

## Testes

```bash
# Executar todos os testes
dotnet test Tests/EstaparParkingChallenge.Tests.csproj

# Apenas unitários
dotnet test Tests/EstaparParkingChallenge.Tests.csproj --filter "TestCategory=Unit"

# Apenas integração (requer banco configurado)
dotnet test Tests/EstaparParkingChallenge.Tests.csproj --filter "TestCategory=Integration"
```

### Cobertura

| Componente                  | Cobertura |
| --------------------------- | --------- |
| WebhookController           | 100%      |
| ParkingPricingService       | 95%       |
| RevenueController           | 89%       |
| WebhookProcessingService    | 85%       |

### Estratégia

- **Unitários** — Regras de precificação isoladas (multiplicadores, fórmula de cálculo)
- **Integração** — Fluxo completo Entry → Parked → Exit → Revenue usando `WebApplicationFactory<Program>` com banco real, reset automático de dados entre testes e seed controlado via `TestDatabaseManager`

---

## Configuração

Todas as configurações são gerenciadas via `appsettings.{Environment}.json` com binding via `IOptions<T>`:

| Seção              | Classe                   | Descrição                                              |
| ------------------ | ------------------------ | ------------------------------------------------------ |
| `Startup`          | `StartupConfig`          | Provider de banco (1=Postgres, 2=SqlServer), migrations|
| `Jwt`              | `JwtConfig`              | Secret e Issuer para tokens JWT                        |
| `Redis`            | `RedisConfig`            | Habilitar/conexão Redis (fallback: in-memory)          |
| `SimulatorClient`  | `SimulatorClientConfig`  | URL do simulador, cache, sync na inicialização         |
| `WebhookSignature` | `WebhookSignatureConfig` | Validação por secret (habilitável, header, secret)     |
| `Serilog`          | —                        | Logging estruturado com enrichers                      |

### Migrations

O projeto mantém migrations separadas para cada provider:

```bash
# Criar migration (PostgreSQL)
dotnet ef migrations add NomeDaMigration \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context PostgresDbContext \
  --output-dir Migrations/Postgres

# Criar migration (SQL Server)
dotnet ef migrations add NomeDaMigration \
  --project Site/EstaparParkingChallenge.Site.csproj \
  --startup-project Site/EstaparParkingChallenge.Site.csproj \
  --context SqlServerDbContext \
  --output-dir Migrations/SqlServer
```

---

## Decisões Técnicas

### Multi-Database Provider

O `AppDbContext` abstrato define todas as entidades e configurações, enquanto `PostgresDbContext` e `SqlServerDbContext` herdam e aplicam detalhes específicos do provider. A seleção é feita em runtime via `Startup:Database`, permitindo trocar de banco sem alterar código — apenas configuração.

### Webhook com Secret e Idempotência

- **Webhook Secret no Header** comparado em tempo constante com `CryptographicOperations.FixedTimeEquals()`
- **Idempotência** via chave única por evento, armazenada em tabela dedicada com constraint único

### Precificação Dinâmica

O multiplicador é capturado **no momento da entrada** (snapshot), garantindo que o preço não mude retroativamente conforme a ocupação varia ao longo do dia.

### Separação de Responsabilidades

- **WebhookProcessingService** — orquestração do ciclo de eventos (Entry/Parked/Exit)
- **ParkingService** — cálculo de receita (query-only)
- **ParkingPricingService** — regras de preço isoladas e testáveis
- **GarageSyncService** — sincronização com simulador externo

### Error Handling Centralizado

`HandleExceptionFilter` captura todas as exceções: `ApiException` retorna **422** com payload estruturado (`ErrorModel`), exceções inesperadas retornam **500** com código de rastreamento único para suporte.

### Cache com Fallback

Redis é opcional. Quando desabilitado, o sistema usa `DistributedMemoryCache` automaticamente — zero impacto em ambientes de desenvolvimento sem Redis.
