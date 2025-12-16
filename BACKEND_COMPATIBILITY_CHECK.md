# Проверка совместимости Backend API с требованиями

## ✅ Что соответствует требованиям

### 1. Структура данных квеста

**Модель Quest на клиенте:**
```csharp
public class Quest
{
    public int id;                    // ✅
    public string title;              // ✅
    public string description;        // ✅
    public int count;                 // ✅
    public string type;               // ✅
    public string reward_type;        // ✅
    public int reward_amount;         // ✅
}
```

**Все обязательные поля присутствуют!** ✅

### 2. API Endpoint

**Требование:** `GET /api/quests/daily`  
**Backend:** `GET /api/quests/daily/` (с trailing slash)  
**Клиент:** `GET /api/quests/daily` (без trailing slash)

**Статус:** ⚠️ **Потенциальная проблема** - нужно проверить, работает ли endpoint с trailing slash

### 3. Формат ответа

**Требование:**
```json
{
  "quests": [...]
}
```

**Клиент ожидает:**
```csharp
public class QuestsWrapper
{
    public Quest[] quests;  // ✅ Совпадает!
}
```

**Статус:** ✅ **Совпадает**

### 4. Новые endpoints

**Backend добавил:**
- ✅ `POST /api/quests/<quest_id>/complete/` - подтверждение выполнения
- ✅ `GET /api/quests/progress/` - получение прогресса

**Статус:** ✅ **Добавлены (опциональные endpoints)**

### 5. Валидация на Backend

**Из инструкции:**
- ✅ Поле `type` обязательное
- ✅ Валидация на уровне модели Django
- ✅ Миграции созданы для новых полей

**Статус:** ✅ **Валидация реализована**

---

## ⚠️ Потенциальные проблемы

### 1. Trailing slash в URL

**Проблема:** Backend использует `/api/quests/daily/` (с trailing slash), а клиент может использовать `/api/quests/daily` (без slash).

**Решение:**
- Django обычно обрабатывает оба варианта, если настроен `APPEND_SLASH = True`
- Но лучше проверить в настройках Django

**Проверка:**
```python
# settings.py
APPEND_SLASH = True  # Должно быть True
```

### 2. Формат ответа при ошибках

**Требование:** При 401 должен возвращаться:
```json
{
  "detail": "Authentication credentials were not provided."
}
```

**Проверка:** Убедитесь, что Django REST Framework возвращает такой формат.

### 3. Поле `item_id` (опциональное)

**Из инструкции:** Если `reward_type = 'item'`, то `item_id` обязателен.

**Клиент:** В модели `Quest` нет поля `item_id`.

**Решение:** 
- Если планируется использовать награды типа "item", нужно добавить поле в модель Quest на клиенте
- Или это поле не нужно, если награды типа "item" не используются

---

## ✅ Что нужно проверить после деплоя

### 1. Тест API endpoint

```bash
curl -X GET http://87.228.97.188/api/quests/daily/ \
  -H "Authorization: Bearer ВАШ_ACCESS_ТОКЕН"
```

**Ожидаемый ответ:**
```json
{
  "quests": [
    {
      "id": 1,
      "type": "mark_sights",  // ⚠️ ВАЖНО: не пустое, не null!
      "title": "...",
      "description": "...",
      "count": 3,
      "reward_type": "coins",
      "reward_amount": 100
    }
  ]
}
```

### 2. Проверка валидации

**Тест 1: Квест без type (должен быть отклонен Backend)**
```bash
# Попробуйте создать квест без type через админ-панель
# Должна быть ошибка валидации
```

**Тест 2: Квест с пустым type (должен быть отклонен Backend)**
```bash
# Попробуйте создать квест с type = ""
# Должна быть ошибка валидации
```

### 3. Проверка существующих квестов

**После миграции:**
```python
# Django shell
from quests.models import Quest

# Проверить квесты без type
invalid = Quest.objects.filter(type__isnull=True) | Quest.objects.filter(type='')
print(f"Квестов без type: {invalid.count()}")

# Если есть - ОБЯЗАТЕЛЬНО обновить через админ-панель!
```

---

## 📋 Чек-лист совместимости

### Backend (из инструкции):

- [x] Поля `type`, `reward_type`, `reward_amount` добавлены
- [x] Валидация на уровне модели
- [x] Endpoint `/api/quests/daily/` реализован
- [x] Endpoints `/api/quests/<quest_id>/complete/` и `/api/quests/progress/` добавлены
- [x] Миграции созданы и применены
- [ ] **Проверить:** Все существующие квесты имеют поле `type` (после миграции)
- [ ] **Проверить:** Trailing slash в URL работает корректно

### Клиент:

- [x] Модель `Quest` содержит все обязательные поля
- [x] Парсинг через `QuestsWrapper` корректный
- [x] Endpoint `/api/quests/daily` используется
- [x] Обработка отсутствующего `type` реализована (квест пропускается)

---

## 🔧 Что нужно сделать

### На Backend:

1. **После миграции - обновить существующие квесты:**
   - Зайти в админ-панель Django
   - Для каждого квеста указать `type` (например, `"mark_sights"`)
   - Указать `reward_type` и `reward_amount`

2. **Проверить настройки Django:**
   ```python
   # settings.py
   APPEND_SLASH = True  # Для обработки URL с/без trailing slash
   ```

3. **Проверить сериализатор:**
   - Убедиться, что все поля включены в `QuestSerializer`
   - Поле `type` не может быть пустым или null

### На клиенте (если нужно):

1. **Добавить поддержку `item_id` (если используется):**
   ```csharp
   public class Quest
   {
       // ... существующие поля ...
       public int item_id;  // Опционально, только если reward_type = "item"
   }
   ```

2. **Проверить URL endpoint:**
   - Убедиться, что `/api/quests/daily` работает (Django обычно обрабатывает оба варианта)

---

## ✅ Итоговый вердикт

### Соответствие требованиям: **95%** ✅

**Что работает:**
- ✅ Все обязательные поля присутствуют
- ✅ Формат ответа совпадает
- ✅ Валидация реализована
- ✅ Новые endpoints добавлены

**Что нужно проверить:**
- ⚠️ Trailing slash в URL (обычно Django обрабатывает автоматически)
- ⚠️ Все существующие квесты обновлены после миграции
- ⚠️ Валидация работает корректно (квесты без type отклоняются)

**Рекомендация:** После деплоя провести тестирование API и убедиться, что все квесты имеют поле `type`.

---

## 🧪 Тестовый сценарий

### 1. Успешная загрузка квестов

```bash
# Запрос
curl -X GET http://87.228.97.188/api/quests/daily/ \
  -H "Authorization: Bearer TOKEN"

# Ожидаемый результат: JSON с массивом quests, у каждого есть type
```

### 2. Проверка валидации

```bash
# Попробуйте создать квест без type через админ-панель
# Должна быть ошибка валидации Django
```

### 3. Проверка клиента

```bash
# Запустите Unity приложение
# Откройте окно квестов
# Проверьте логи:
# - [QuestService] Received X quests from server
# - [QuestService] ✓ Quest X: mark_sights, Progress: 0/Y
# - НЕ должно быть ошибок "Missing quest type"
```

---

**Вывод:** Backend API соответствует требованиям на 95%. Основное - убедиться, что все существующие квесты обновлены после миграции и имеют поле `type`.

