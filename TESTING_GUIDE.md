# 🧪 QuizFuzz - Руководство по тестированию

## 📊 Статус запуска

**Backend API:** Запущен в отдельном окне PowerShell  
**Frontend:** Запущен в отдельном окне PowerShell  

⏰ **Время запуска:**
- Backend API: ~30-60 секунд
- Frontend (Blazor WASM): ~2-3 минуты (первая компиляция)

---

## 🌐 URL-адреса

- **Frontend:** https://localhost:5002
- **Backend API:** https://localhost:7001
- **Swagger UI:** https://localhost:7001/swagger

---

## 🧪 Тестирование вручную

### 1️⃣ Проверка Backend API

Откройте в браузере: **https://localhost:7001/swagger**

Должны увидеть Swagger UI с endpoints:
- ✅ /api/auth/register
- ✅ /api/auth/login
- ✅ /api/rooms
- ✅ /api/questions
- ✅ /api/game
- ✅ И другие...

### 2️⃣ Проверка Frontend

Откройте в браузере: **https://localhost:5002**

Должны увидеть:
- ✅ QuizFuzz главная страница
- ✅ Login/Register кнопки
- ✅ MudBlazor UI

---

## 🎮 Полный тестовый сценарий

### Шаг 1: Регистрация
1. Откройте https://localhost:5002
2. Нажмите "Register"
3. Заполните форму:
   - **Username:** testuser
   - **Email:** test@quizfuzz.com
   - **Password:** Test123456
   - **Confirm Password:** Test123456
4. Нажмите "Register"
5. ✅ Должны автоматически войти и перейти на /lobby

### Шаг 2: Lobby
На странице /lobby должны увидеть:
- ✅ Приветствие: "Welcome back, testuser!"
- ✅ Кнопка "Create Room"
- ✅ Список комнат (возможно пустой)
- ✅ Кнопка "Logout"

### Шаг 3: Создание комнаты
1. Нажмите "Create Room"
2. Заполните форму:
   - **Room Name:** Test Room
   - **Max Players:** 10
   - **Private Room:** нет
   - **Victory Condition:** POINTS
   - **Victory Value:** 100
3. Нажмите "Create Room"
4. ✅ Должны перейти на /room/{id}

### Шаг 4: Room (Lobby комнаты)
На странице /room/{id} должны увидеть:
- ✅ Название комнаты: "Test Room"
- ✅ Вы в списке игроков с меткой "Host"
- ✅ Кнопка "Start Game" (для владельца)
- ✅ Кнопка "Leave Room"
- ✅ Room Settings панель

**Примечание:** Для теста игры нужны:
- ⚠️ Минимум 2 игрока
- ⚠️ Вопросы в базе данных

### Шаг 5: Profile
1. В меню навигации выберите "Profile"
2. ✅ Должны увидеть:
   - Username и email
   - Статистику (Games Played, Games Won, etc.)
   - Win Rate и Accuracy (0% для нового пользователя)
   - Achievements

---

## 👮 Тестирование Admin Panel

### Предварительные требования:
Нужен пользователь с ролью Admin или Moderator.

### Через Swagger UI (https://localhost:7001/swagger):

1. **Зарегистрируйте Admin пользователя:**
   - POST /api/auth/register
   - Body:
     ```json
     {
       "username": "admin",
       "email": "admin@quizfuzz.com",
       "password": "Admin123456"
     }
     ```

2. **Вручную добавьте роль в БД** (через SQL или программно):
   - Вам нужно добавить роль "Admin" этому пользователю

### После получения роли:

1. Войдите как admin
2. В навигации появится раздел "Administration"
3. ✅ Должны увидеть:
   - Admin Dashboard (/admin)
   - Moderation Queue (/admin/moderation)
   - Questions Management (/admin/questions)
   - Users Management (/admin/users) - только Admin
   - Tags Management (/admin/tags) - только Admin

---

## 🎯 Критические проверки

### ✅ Что должно работать:

**Authentication:**
- [x] Регистрация нового пользователя
- [x] Вход существующего пользователя
- [x] Logout
- [x] JWT токены сохраняются в LocalStorage
- [x] Защищенные маршруты перенаправляют на /login

**Lobby:**
- [x] Отображение списка комнат
- [x] Создание новой комнаты
- [x] Join в существующую комнату

**UI/UX:**
- [x] MudBlazor компоненты отображаются
- [x] Responsive design работает
- [x] Navigation menu работает
- [x] Loading states показываются

---

## ⚠️ Известные ограничения

1. **Игра требует минимум 2 игрока:**
   - Откройте второе окно браузера (Incognito)
   - Зарегистрируйте второго пользователя
   - Присоединитесь к той же комнате

2. **Вопросы должны быть в БД:**
   - Используйте Admin Panel для создания вопросов
   - Или добавьте seed data

3. **Real-time обновления требуют SignalR:**
   - Убедитесь, что SignalR Hubs работают
   - Проверьте в консоли браузера (F12)

---

## 🔧 Troubleshooting

### Backend не запускается:
```bash
# Проверьте порты
netstat -an | findstr "7001"

# Проверьте логи в окне PowerShell с Backend
```

### Frontend не компилируется:
```bash
# Очистите и пересоберите
cd src/Clients/QuizFuzz.Client
dotnet clean
dotnet build
dotnet run
```

### Ошибка подключения к БД:
```bash
# Проверьте PostgreSQL
Get-Service -Name "*postgres*"

# Проверьте connection string в appsettings.json
```

### SignalR не подключается:
- Проверьте консоль браузера (F12 → Console)
- SignalR должен подключиться к /hubs/game и /hubs/lobby
- Проверьте, что Backend запущен

---

## 📊 Ожидаемые результаты

После успешного запуска:

✅ Backend API отвечает на https://localhost:7001  
✅ Swagger UI доступен  
✅ Frontend загружается на https://localhost:5002  
✅ Можно зарегистрироваться и войти  
✅ Можно создать комнату  
✅ UI выглядит профессионально (MudBlazor)  
✅ Navigation работает  
✅ Profile page отображается  

---

## 🎉 Если все работает:

**ПОЗДРАВЛЯЮ! Проект QuizFuzz работает!** 🚀

Вы можете:
- Создавать вопросы через Admin Panel
- Приглашать друзей для игры
- Тестировать full game flow
- Демонстрировать проект

---

## 📞 Помощь

Если возникли проблемы:
1. Проверьте логи в окнах PowerShell
2. Проверьте консоль браузера (F12)
3. Убедитесь, что PostgreSQL запущен
4. Проверьте, что порты 7001 и 5002 свободны

**Удачного тестирования!** 🎮
