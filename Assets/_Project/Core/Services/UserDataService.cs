using System;
using UnityEngine;

[Serializable]
public class UserData
{
    public string username;
    public string password;
    public string firstName;
    public string lastName;
    public string dateOfStart;

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

    public string lastStepsDate;
}

public class UserDataService
{
    private const string Key = "UserData";

    private UserData _data;

    public UserData Data => _data;

    public string Username => _data.username;
    public string Password => _data.password;
    
    public string FirstName => _data.firstName;
    public string LastName => _data.lastName;
    
    
    public string AccessToken => _data.accessToken;
    public string RefreshToken => _data.refreshToken;
    public int Coins => _data.coins;
    public int Level => _data.level;
    public int Experience => _data.experience;
    public int Steps => _data.dailySteps;
    
    public string DateOfStart => _data.dateOfStart;

    public UserDataService()
    {
        Load();
        CheckDailyStepsReset();
    }

    public void Save()
    {
        var json = JsonUtility.ToJson(_data);
        Debug.Log(json);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

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

    public void SetAuthData(string username, string password, string accessToken, string refreshToken)
    {
        _data.username = username;
        _data.password = password;
        _data.accessToken = accessToken;
        _data.refreshToken = refreshToken;
        Save();
    }

    public void UpdateAccessToken(string accessToken)
    {
        _data.accessToken = accessToken;
        Save();
    }

    public void SetProfile(string gender, int boots, int pants, int tshirt, int cap, int coins, int level, int exp, int steps, string firstName, string lastName, string dateOfStart)
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
        _data.lastName = lastName;
        _data.firstName = firstName;
        _data.dateOfStart = dateOfStart;
        Save();
    }

    public void AddExperience(int amount)
    {
        _data.experience += amount;
        while (_data.experience >= 100)
        {
            _data.experience -= 100;
            _data.level++;
        }
        Save();
    }

    public void AddSteps(int amount)
    {
        CheckDailyStepsReset();
        _data.dailySteps += amount;
        Save();
    }

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

    public void Clear()
    {
        _data = new UserData();
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
