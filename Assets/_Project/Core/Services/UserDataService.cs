using System;
using UnityEngine;

[Serializable]
public class UserData
{
    public string username;
    public string password;

    public string accessToken;
    public string refreshToken;

    public string gender;

    public int cap;
    public int tshirt;
    public int pants;
    public int boots;

    public int level;
    public int experience;
    public int dailySteps;
    public int coins;

    public string lastStepsDate; // для обнуления шагов раз в день
}

public class UserDataService
{
    private const string Key = "UserData";

    private UserData _data;

    public UserData Data => _data;

    // Удобные геттеры
    public string Username => _data.username;
    public string Password => _data.password;
    public string AccessToken => _data.accessToken;
    public string RefreshToken => _data.refreshToken;
    public int Coins => _data.coins;
    public int Level => _data.level;
    public int Experience => _data.experience;
    public int Steps => _data.dailySteps;

    public UserDataService()
    {
        Load();
        CheckDailyStepsReset();
    }

    /// <summary> Сохраняем данные в PlayerPrefs </summary>
    public void Save()
    {
        var json = JsonUtility.ToJson(_data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    /// <summary> Загружаем данные </summary>
    public void Load()
    {
        if (PlayerPrefs.HasKey(Key))
        {
            var json = PlayerPrefs.GetString(Key);
            _data = JsonUtility.FromJson<UserData>(json);
        }
        else
        {
            _data = new UserData();
        }
    }

    /// <summary> Устанавливаем логин/пароль и токены </summary>
    public void SetAuthData(string username, string password, string accessToken, string refreshToken)
    {
        _data.username = username;
        _data.password = password;
        _data.accessToken = accessToken;
        _data.refreshToken = refreshToken;
        Save();
    }

    /// <summary> Обновляем только accessToken </summary>
    public void UpdateAccessToken(string accessToken)
    {
        _data.accessToken = accessToken;
        Save();
    }

    /// <summary> Обновляем профиль </summary>
    public void SetProfile(string gender, int boots, int pants, int tshirt, int cap, int coins, int level, int exp, int steps)
    {
        _data.gender = gender;
        _data.boots = boots;
        _data.pants = pants;
        _data.tshirt = tshirt;
        _data.cap = cap;
        _data.coins = coins;
        _data.level = level;
        _data.experience = exp;
        _data.dailySteps = steps;
        Save();
    }

    /// <summary> Добавляем опыт и уровень </summary>
    public void AddExperience(int amount)
    {
        _data.experience += amount;
        // допустим, 100 XP = 1 уровень
        while (_data.experience >= 100)
        {
            _data.experience -= 100;
            _data.level++;
        }
        Save();
    }

    /// <summary> Добавляем шаги </summary>
    public void AddSteps(int amount)
    {
        CheckDailyStepsReset();
        _data.dailySteps += amount;
        Save();
    }

    /// <summary> Проверка и сброс шагов раз в день </summary>
    private void CheckDailyStepsReset()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_data.lastStepsDate != today)
        {
            _data.dailySteps = 0;
            _data.lastStepsDate = today;
            Save();
        }
    }

    /// <summary> Очистка данных (логаут) </summary>
    public void Clear()
    {
        _data = new UserData();
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
