# Branch And Environment Workflow

## Branch Flow

- `homologation` is the integration branch for all feature work.
- Every `feature/*` branch must be created from `homologation` and merged back into `homologation` first.
- `main` and `production` represent production-ready code and use the production database path.
- After code reaches `production`, a `production-test/*` branch may be created to validate behavior against the production database before the final release.

## Database Mapping

| Branch pattern | Database environment | App setting |
| --- | --- | --- |
| `homologation` | Test | `CC_TEST_DATABASE_PATH` |
| `feature/*` | Test | `CC_TEST_DATABASE_PATH` |
| `feat/*` | Test | `CC_TEST_DATABASE_PATH` |
| `ProgramTranslation`, `Homologation` | Test | `CC_TEST_DATABASE_PATH` |
| `main` | Production | `CC_SHARED_DATABASE_PATH` |
| `production` | Production | `CC_SHARED_DATABASE_PATH` |
| `production-test/*` | Production | `CC_SHARED_DATABASE_PATH` |
| `Produccion`, `master` | Production | `CC_SHARED_DATABASE_PATH` |

`DatabaseConnectionProvider` is the single source of truth for branch-to-database resolution. It reads branch metadata from `CC_DATABASE_BRANCH`, common CI variables, or `.git/HEAD` when running from a checkout. If no branch metadata is available, it keeps the safe legacy behavior and uses production.

## Configuration

- `CC_SHARED_DATABASE_PATH`: production SQLite database path.
- `CC_TEST_DATABASE_PATH`: homologation/test SQLite database path.
- `CC_DATABASE_BRANCH`: optional explicit branch override for deployed builds or manual validation.

Do not put secrets in these settings. Use paths or environment-variable expansions only.
