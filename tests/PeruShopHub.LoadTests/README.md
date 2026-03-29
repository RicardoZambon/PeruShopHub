# PeruShopHub Load Tests

Load tests for critical API endpoints using [NBomber](https://nbomber.com/).

## Scenarios

| Scenario | Rate | Duration | Target |
|----------|------|----------|--------|
| `webhook_processing` | 100 req/s | 10s | POST `/api/webhooks/mercadolivre` |
| `order_detail_fetch` | 50 req/s | 10s | GET `/api/orders/{id}` |
| `product_create` | 20 req/s | 10s | POST `/api/products` |

## Performance Targets

| Scenario | p50 | p95 | p99 | Error Rate |
|----------|-----|-----|-----|------------|
| Webhook processing | < 100ms | < 300ms | < 500ms | < 1% |
| Order detail fetch | < 500ms | < 1500ms | < 2000ms | < 1% |
| Product create | < 500ms | < 1500ms | < 2000ms | < 1% |

## Running

```bash
# Start the API first (with PostgreSQL + Redis running)
dotnet run --project src/PeruShopHub.API

# Run load tests (default: http://localhost:5062)
dotnet run --project tests/PeruShopHub.LoadTests

# Custom base URL
dotnet run --project tests/PeruShopHub.LoadTests -- --base-url http://localhost:5000

# Or via environment variable
BASE_URL=http://localhost:5000 dotnet run --project tests/PeruShopHub.LoadTests
```

## Reports

Results are saved to `load-test-results/` in TXT and HTML formats after each run.
Key metrics reported: p50, p95, p99 latency, throughput (RPS), and error rate.

## Prerequisites

- API running with PostgreSQL and Redis
- No rate limiting configured (or rate limits set high enough for test load)

## CI Integration

To run in CI, add to `.github/workflows/ci.yml`:

```yaml
- name: Load Tests
  run: dotnet run --project tests/PeruShopHub.LoadTests -- --base-url ${{ env.API_URL }}
  continue-on-error: true  # Optional: don't fail CI on perf regression
```
