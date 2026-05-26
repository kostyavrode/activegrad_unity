using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    public string username;
    public string password;
    public string firstName;
    public string lastName;
    public string dateOfStart;
    public int id;

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
    public int strength = 1;
    public int intelligence = 1;
    public int agility = 1;
    public int stat_upgrade_points;

    public string lastStepsDate;
    public int[] sights;
    public int[] partnerStoreIds;
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
    
    public int ID => _data.id;
    
    
    public string AccessToken => _data.accessToken;
    public string RefreshToken => _data.refreshToken;
    public int Coins => _data.coins;
    public int Level => _data.level;
    public int Experience => _data.experience;
    public int Strength => _data.strength;
    public int Intelligence => _data.intelligence;
    public int Agility => _data.agility;
    public int StatUpgradePoints => _data.stat_upgrade_points;

    public float GetProfessionLevel()
    {
        float total = _data.strength + _data.intelligence + _data.agility - 3f;
        return Mathf.Clamp01(total / 27f);
    }

    public int Steps => _data.dailySteps;
    
    public int[] Sights => _data.sights;
    public int[] PartnerStoreIds => _data.partnerStoreIds;
    
    public string DateOfStart => _data.dateOfStart;

    public UserDataService()
    {
        Load();
        CheckDailyStepsReset();
    }

    public void Save()
    {
        var json = JsonUtility.ToJson(_data);
//        Debug.Log(json);
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

    public void SetProfile(string gender, int boots, int pants, int tshirt, int cap, int coins, int level, int exp, int steps, string firstName, string lastName, string dateOfStart, int id)
    {
        SetProfile(gender, boots, pants, tshirt, cap, coins, level, exp, steps, firstName, lastName, dateOfStart, id, 1, 1, 1, 0);
    }

    public void SetProfile(string gender, int boots, int pants, int tshirt, int cap, int coins, int level, int exp, int steps, string firstName, string lastName, string dateOfStart, int id, int strength, int intelligence, int agility, int statUpgradePoints)
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
        _data.id = id;
        _data.strength = strength > 0 ? strength : 1;
        _data.intelligence = intelligence > 0 ? intelligence : 1;
        _data.agility = agility > 0 ? agility : 1;
        _data.stat_upgrade_points = statUpgradePoints;
        Save();
    }

    public void UpdateNames(string firstName, string lastName)
    {
        if (!string.IsNullOrEmpty(firstName)) _data.firstName = firstName;
        if (!string.IsNullOrEmpty(lastName)) _data.lastName = lastName;
        Save();
    }

    public void SetStats(int strength, int intelligence, int agility, int statUpgradePoints)
    {
        _data.strength = strength > 0 ? strength : 1;
        _data.intelligence = intelligence > 0 ? intelligence : 1;
        _data.agility = agility > 0 ? agility : 1;
        _data.stat_upgrade_points = statUpgradePoints;
        Save();
    }

    public void SetLevelAndExperience(int level, int experience)
    {
        _data.level      = level;
        _data.experience = experience;
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

    public void SetSights(int[] sights)
    {
        _data.sights = sights;
        Save();
    }

    public void AddSightToMarked(int sightId)
    {
        if (_data.sights == null)
        {
            _data.sights = new[] { sightId };
        }
        else
        {
            foreach (var id in _data.sights)
                if (id == sightId) return;
            var list = new List<int>(_data.sights) { sightId };
            _data.sights = list.ToArray();
        }
        Save();
    }
    
    public void UpdateCoins(int coins)
    {
        _data.coins = coins;
        Save();
    }

    public void SetPartnerStoreIds(int[] storeIds)
    {
        _data.partnerStoreIds = storeIds ?? Array.Empty<int>();
        Save();
    }

    public void AddPartnerStoreToMarked(int storeId)
    {
        if (_data.partnerStoreIds == null || _data.partnerStoreIds.Length == 0)
        {
            _data.partnerStoreIds = new[] { storeId };
            Save();
            return;
        }

        foreach (var id in _data.partnerStoreIds)
            if (id == storeId) return;

        var list = new List<int>(_data.partnerStoreIds) { storeId };
        _data.partnerStoreIds = list.ToArray();
        Save();
    }

    public bool IsPartnerStoreUnmarked(int storeId)
    {
        if (_data.partnerStoreIds == null) return true;
        foreach (var id in _data.partnerStoreIds)
            if (id == storeId) return false;
        return true;
    }

    public bool CheckSight(int sight)
    {
        if (Sights == null) return true;
        foreach (var id in Sights)
            if (id == sight) return false;
        return true;
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
