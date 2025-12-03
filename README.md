# Система управления библиотекой

Полнофункциональная система управления библиотекой на ASP.NET Core с PostgreSQL.

## Технологии

- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: PostgreSQL (прямой доступ через Npgsql, без Entity Framework)
- **Frontend**: ASP.NET Core Razor Pages с Bootstrap 5
- **Аутентификация**: JWT (Microsoft.AspNetCore.Authentication.JwtBearer)
- **Безопасность**: BCrypt для хэширования паролей
- **Отчёты**: iText7 (PDF), ClosedXML (XLSX)

## Структура проекта

```
LibraryManagement/
├── Controllers/          # API контроллеры
├── Services/            # Бизнес-логика
├── DataAccess/          # SQL-запросы через Npgsql
├── Models/              # Модели данных
├── DTOs/                # Data Transfer Objects
├── Pages/               # Razor Pages (Frontend)
│   ├── Auth/           # Страницы входа/регистрации
│   ├── Books/          # Каталог книг
│   ├── User/           # Личный кабинет пользователя
│   └── Admin/          # Административная панель
├── wwwroot/            # Статические файлы
│   ├── images/         # Изображения обложек книг
│   ├── css/            # Стили
│   └── js/             # JavaScript
├── init.sql            # SQL-скрипт для создания БД
├── Program.cs          # Точка входа приложения
└── appsettings.json    # Конфигурация
```

## Установка и настройка

### 1. Требования

- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022 или VS Code

### 2. Настройка базы данных

1. Установите PostgreSQL и создайте базу данных:
```sql
CREATE DATABASE library_management;
```

2. Подключитесь к базе данных и выполните скрипт `init.sql`:
```bash
psql -U postgres -d library_management -f init.sql
```

Или через pgAdmin: откройте файл `init.sql` и выполните его.

### 3. Настройка подключения

Отредактируйте `appsettings.json` и укажите параметры подключения к PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=library_management;Username=postgres;Password=ваш_пароль"
  }
}
```

### 4. Установка зависимостей

Зависимости уже указаны в `.csproj` файле. При первом запуске Visual Studio автоматически восстановит пакеты NuGet, или выполните:

```bash
dotnet restore
```

### 5. Запуск приложения

```bash
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:5001` или `http://localhost:5000`

## Использование

### Регистрация и вход

1. Перейдите на `/auth/register` для создания нового аккаунта
2. После регистрации вы автоматически войдёте в систему
3. Для входа используйте `/auth/login`

### Роли пользователей

- **user** - обычный пользователь (может брать книги, просматривать каталог)
- **admin** - администратор (полный доступ, включая управление книгами и генерацию отчётов)

### Основные функции

#### Для всех пользователей:
- Просмотр каталога книг (`/books`)
- Поиск и фильтрация книг
- Просмотр деталей книги
- Взятие книг в займ (требуется авторизация)
- Возврат книг (`/user/loans`)

#### Для администраторов:
- Добавление книг (`/admin/books/add`)
- Редактирование книг
- Удаление книг
- Загрузка обложек книг
- Генерация отчётов (`/admin/reports`)

## API Endpoints

### Аутентификация
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Вход

### Книги
- `GET /api/books` - Список книг (с пагинацией и фильтрами)
- `GET /api/books/{id}` - Детали книги
- `GET /api/books/isbn/{isbn}` - Поиск по ISBN
- `POST /api/books` - Создание книги (admin)
- `PUT /api/books/{id}` - Обновление книги (admin)
- `DELETE /api/books/{id}` - Удаление книги (admin)
- `POST /api/books/{id}/loan` - Взять книгу (auth)
- `POST /api/books/{id}/return` - Вернуть книгу (auth)
- `POST /api/books/{id}/image` - Загрузить обложку (admin)

### Авторы
- `GET /api/authors` - Список авторов
- `GET /api/authors/{id}` - Детали автора
- `GET /api/authors/{id}/books` - Книги автора
- `POST /api/authors` - Создание автора (admin)
- `PUT /api/authors/{id}` - Обновление автора (admin)
- `DELETE /api/authors/{id}` - Удаление автора (admin)

### Пользователи
- `GET /api/users/{id}/loans` - Займы пользователя (auth)

### Отчёты
- `POST /api/reports/pdf?fromDate={date}&toDate={date}` - PDF отчёт (admin)
- `POST /api/reports/excel?fromDate={date}&toDate={date}` - Excel отчёт (admin)

## Структура базы данных

База данных содержит 17 таблиц:
1. Users - Пользователи
2. Roles - Роли
3. Authors - Авторы
4. Books - Книги
5. Genres - Жанры
6. Publishers - Издательства
7. Languages - Языки
8. Series - Серии
9. BookCopies - Копии книг
10. Loans - Займы
11. Reservations - Резервации
12. Fines - Штрафы
13. Reviews - Отзывы
14. BookAuthors - Связь книг и авторов (Many-to-Many)
15. BookGenres - Связь книг и жанров (Many-to-Many)
16. Branches - Филиалы библиотеки
17. AuditLogs - Журнал аудита

## Особенности

- **Прямой SQL-доступ**: Все запросы выполняются через Npgsql без использования ORM
- **Транзакции**: Критические операции выполняются в транзакциях
- **Безопасность**: Пароли хэшируются через BCrypt, JWT для аутентификации
- **Валидация**: DataAnnotations для валидации данных
- **Пагинация**: Поддержка пагинации для списков
- **Поиск и фильтрация**: Поиск по названию/ISBN, фильтры по жанрам и авторам
- **Автоматические штрафы**: Триггер в БД автоматически создаёт штрафы при просрочке

## Тестовые данные

После выполнения `init.sql` в базе данных будут созданы:
- Роли: `user`, `admin`
- Несколько тестовых книг, авторов, жанров
- По 5 копий каждой книги в центральной библиотеке

## Разработка

### Добавление новых функций

1. Создайте модель в `Models/`
2. Создайте DTO в `DTOs/`
3. Добавьте методы в соответствующий Repository в `DataAccess/`
4. Добавьте методы в Service в `Services/`
5. Создайте/обновите Controller в `Controllers/`
6. При необходимости создайте Razor Page в `Pages/`

## Лицензия

Этот проект создан в образовательных целях.

