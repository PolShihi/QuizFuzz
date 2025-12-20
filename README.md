# QuizFuzz

Веб-приложение для проведения многопользовательских игр-викторин с нечеткими критериями принятия ответа.

## О проекте

QuizFuzz — это платформа для реалтайм-викторин с интеллектуальной системой проверки ответов, которая учитывает опечатки, алиасы и семантическую близость.

### Ключевые возможности

- Реалтайм игры через SignalR
- Нечеткое сопоставление ответов (Fuzzy Matching)
- Система очков и лидерборды
- Публичные и приватные комнаты
- Модерация пользовательского контента
- Поддержка текстовых, графических и аудио вопросов
- Детальная статистика игроков

## Архитектура

Проект построен на **Clean Architecture** с четким разделением слоев:

```
src/
├── Core/
│   ├── QuizFuzz.Domain         # Domain Layer - бизнес-логика
│   └── QuizFuzz.Application    # Application Layer - use cases
├── Infrastructure/
│   └── QuizFuzz.Infrastructure # Infrastructure - EF Core, репозитории
├── Web/
│   └── QuizFuzz.Web.Api        # Web API + SignalR Hubs
└── Clients/
    ├── QuizFuzz.Client         # Blazor WebAssembly
    └── QuizFuzz.Shared         # Shared DTOs
```

## Технологический стек

### Backend
- **.NET 8** - основной фреймворк
- **ASP.NET Core** - Web API
- **SignalR** - реалтайм коммуникация
- **Entity Framework Core 8** - ORM
- **PostgreSQL 15+** - база данных
- **Redis** - кэширование и pub/sub

### Frontend
- **Blazor WebAssembly** - клиентское приложение
- **MudBlazor** - UI компоненты
- **Fluxor** - state management

### Инфраструктура
- **Docker** - контейнеризация
- **Serilog** - логирование
- **OpenTelemetry** - observability

## Текущий статус

**Прогресс разработки:** 31% (4 из 13 основных задач)

### Завершено
- [x] Структура проекта (Clean Architecture)
- [x] Domain Layer (сущности, value objects, events)
- [x] Application Layer (интерфейсы, DTOs, commands)
- [x] Infrastructure Layer (EF Core, конфигурации)

### В работе
- [ ] Миграции базы данных
- [ ] Реализация репозиториев
- [ ] Fuzzy Matching Service
- [ ] Web API Controllers
- [ ] SignalR Hubs
- [ ] Authentication/Authorization
- [ ] Blazor WASM Client
- [ ] Redis кэширование
- [ ] Observability
- [ ] Тестирование
- [ ] Docker/Compose

## Разработка

### Требования

- .NET SDK 8.0+
- PostgreSQL 15+
- Redis (опционально)
- Docker (опционально)

### Сборка проекта

```bash
# Клонирование репозитория
git clone <repository-url>
cd QuizFuzz

# Восстановление зависимостей
dotnet restore

# Сборка всего решения
dotnet build

# Запуск тестов
dotnet test
```

### Структура базы данных

```bash
# Создание миграции
dotnet ef migrations add InitialCreate --project src/Infrastructure/QuizFuzz.Infrastructure --startup-project src/Web/QuizFuzz.Web.Api

# Применение миграций
dotnet ef database update --project src/Infrastructure/QuizFuzz.Infrastructure --startup-project src/Web/QuizFuzz.Web.Api
```

## Лицензия

Этот проект создан в образовательных целях.

## Команда

Разработка: Команда QuizFuzz

---

**Версия:** 0.1.0-alpha  
**Последнее обновление:** 20.12.2025  
**Файлов кода:** 119 C# файлов (~8000 строк)  
**Статус сборки:** ✅ Domain, Application, Infrastructure
