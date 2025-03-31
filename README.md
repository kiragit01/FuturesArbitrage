# Futures Arbitrage Microservice Architecture

Этот проект представляет микросервисное решение для расчёта и мониторинга арбитражных возможностей между двумя фьючерсными контрактами на один актив (например, BTC). Система собирает данные о ценах с биржи, рассчитывает разницу между ценами квартальных фьючерсов (спред) и сохраняет результаты в базе данных.

## Обзор архитектуры

Система состоит из следующих компонентов:

1. **API Gateway** – точка входа, осуществляет маршрутизацию и балансировку запросов к внутренним сервисам.
2. **Market Data Service** – получает ценовые данные фьючерсов с биржи (например, Binance).
3. **Arbitrage Calculator Service** – вычисляет разницу цен между двумя фьючерсами (спред) за определенный период.
4. **Data Storage Service** – сохраняет рассчитанные спреды в базе данных PostgreSQL.
5. **Scheduler Service** – планировщик заданий, периодически инициирует сбор данных и расчёт спреда.

### Используемые технологии

- .NET 9.0 (C#) – для реализации сервисов (ASP.NET Core Web API).
- Docker & Docker Compose – для контейнеризации микросервисов.
- PostgreSQL – база данных для хранения результатов.
- Entity Framework Core – ORM для работы с БД.
- Quartz.NET – планирование периодических задач (Scheduler).
- YARP – API Gateway (reverse proxy).
- Polly – механизм повторных попыток для устойчивости HTTP-запросов.
- Serilog – логирование.
- Prometheus & Grafana – мониторинг и визуализация метрик.
- Swagger (OpenAPI) – документация API.


## Начало работы

### Предварительные требования

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) и Docker Compose
- [Git](https://git-scm.com/downloads)

### Установка и настройка

1. Клонируйте репозиторий:

```bash
git clone https://github.com/yourusername/futures-arbitrage.git
cd futures-arbitrage
```

2. Сборка и запуск сервисов с помощью Docker Compose:

```bash
docker-compose up -d
```

3. Доступ к сервисам:
    - **API Gateway**: http://localhost:5000
    - **Swagger-документация**: http://localhost:5000/swagger
    - **Prometheus**: http://localhost:9090
    - **Grafana**: http://localhost:3000 (по умолчанию логин/пароль: admin/admin)

## Подробности о сервисах

### API Gateway

API Gateway является единой точкой входа в экосистему микросервисов. Он отвечает за:
- Маршрутизацию запросов к соответствующим сервисам.
- Балансировку нагрузки.
- Обнаружение сервисов.
- Аутентификацию/авторизацию (при необходимости).

### Market Data Service

Этот сервис отвечает за:
- Подключение к API криптовалютных бирж.
- Получение ценовых данных для указанных фьючерсных контрактов.
- Обработку проблем с подключением с использованием политик устойчивости.
- Предоставление стандартизированных ценовых данных другим сервисам.

### Arbitrage Calculator Service

Этот сервис вычисляет арбитражные возможности, выполняя:
- Получение ценовых данных для двух фьючерсных контрактов.
- Расчёт разницы цен.
- Применение необходимых вычислений или формул.
- Обработку случаев, когда данные могут отсутствовать для одного или обоих контрактов.

### Data Storage Service

Этот сервис управляет сохранением данных, выполняя:
- Хранение результатов арбитража в PostgreSQL.
- Предоставление доступа к данным через шаблон репозитория.
- Управление миграциями базы данных.
- Реализацию надлежащей валидации данных.

### Scheduler Service

Этот сервис координирует рабочий процесс, выполняя:
- Планирование расчётов арбитража с настраиваемыми интервалами (почасово/ежедневно).
- Управление выполнением заданий с правильной обработкой ошибок.
- Возможность ручного запуска расчётов.
- Обеспечение правильного управления конкурентностью заданий.

## Конфигурация

Каждый сервис имеет свой файл `appsettings.json` с настройками, специфичными для сервиса. Общие конфигурации включают:

### Подключение к базе данных

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Database=arbitrage;Username=postgres;Password=postgres"
}
```

### Настройки API биржи

```json
"ExchangeSettings": {
  "BaseUrl": "https://api.binance.com",
  "ApiKey": "<your-api-key>",
  "ApiSecret": "<your-api-secret>"
}
```

### Настройки планировщика

```json
"SchedulerSettings": {
  "CalculationInterval": "hourly"  // или "daily"
}
```

## Мониторинг

Решение включает Prometheus и Grafana для мониторинга:
- **Prometheus** собирает метрики со всех сервисов.
- **Grafana** предоставляет дашборды для визуализации метрик.
- Конечные точки проверки состояния отслеживают статус сервисов.

## Обработка ошибок

Приложение реализует комплексные стратегии обработки ошибок:
- Шаблоны устойчивости с использованием Polly для внешних API-вызовов.
- Структурированная обработка исключений и логирование.
- Механизмы отката при отсутствии данных.
- Прерыватели цепи для отказывающих зависимостей.

## Особенности архитектуры

### Принципы SOLID

- **Single Responsibility (Принцип единственной ответственности)**: Каждый сервис и класс имеет одну чётко определённую задачу.
- **Open/Closed (Принцип открытости/закрытости)**: Функциональность расширяется через абстракции, а не модификацию.
- **Liskov Substitution (Принцип подстановки Барбары Лисков)**: Интерфейсы и базовые классы используются корректно с заменяемыми реализациями.
- **Interface Segregation (Принцип разделения интерфейса)**: Узконаправленные интерфейсы предотвращают реализацию ненужных методов.
- **Dependency Inversion (Принцип инверсии зависимостей)**: Модули высокого уровня зависят от абстракций, а не от конкретных реализаций.

### Шаблоны проектирования

- **Repository Pattern (Шаблон репозитория)**: Для абстракции доступа к данным.
- **Factory Pattern (Шаблон фабрики)**: Для создания клиентов сервисов.
- **Strategy Pattern (Шаблон стратегии)**: Для различных стратегий вычислений.
- **CQRS Pattern (Шаблон CQRS)**: Для разделения операций чтения и записи.
- **Circuit Breaker Pattern (Шаблон прерывателя цепи)**: Для обработки сбоев во внешних сервисах.

### Чистая архитектура

Решение реализует чистую архитектуру с разделением на слои:
- **Domain Layer (Слой домена)**: Основная бизнес-логика.
- **Application Layer (Слой приложения)**: Реализация сценариев использования.
- **Infrastructure Layer (Слой инфраструктуры)**: Внешние зависимости, такие как базы данных и API.
- **Presentation Layer (Слой представления)**: Контроллеры API и конечные точки.

## Лицензия

Этот проект распространяется под лицензией MIT — подробности см. в файле LICENSE.

## Благодарности

- Документация API Binance.
- Лучшие практики микросервисной архитектуры.
- Принципы чистой архитектуры.