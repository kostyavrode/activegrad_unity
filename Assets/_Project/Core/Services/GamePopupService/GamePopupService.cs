using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class GamePopupService
{
    private readonly OtherPlayerProfileView.Factory _otherPlayerProfileViewFactory;
    private readonly ISightService _sightService;
    private readonly SightItemFactory _factory;
    private readonly SightsUpdater _sightsUpdater;
    private readonly OtherPlayerSightsDetailsView.Factory _otherPlayerSightsDetailsViewFactory;

    private GameObject _currentPopup;

    public GamePopupService(OtherPlayerProfileView.Factory otherPlayerProfileViewFactory, ISightService sightService, SightItemFactory factory, SightsUpdater sightsUpdater, OtherPlayerSightsDetailsView.Factory otherPlayerSightsDetailsViewFactory)
    {
        _otherPlayerProfileViewFactory = otherPlayerProfileViewFactory;
        _sightService = sightService;
        _factory = factory;
        _sightsUpdater = sightsUpdater;
        _otherPlayerSightsDetailsViewFactory = otherPlayerSightsDetailsViewFactory;
    }

    public void CreateOtherPlayerProfilePopup(string info)
    {
        Debug.Log("Info:"+info);
        var popup = _otherPlayerProfileViewFactory.Create();
        popup.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);

        var (data, externalIds) = Parse(info);
        RootResponse response = DeserializePlayerResponse(info);
        
        List<string> infoForView = new List<string>();
        
        infoForView.Add(response.playerData.Username);
        infoForView.Add(response.playerData.Landmarks?.TotalCount.ToString() ?? "0");
        infoForView.Add(response.playerData.Level.ToString());
        infoForView.Add(response.playerData.FirstName);
        infoForView.Add(response.playerData.LastName);
        infoForView.Add(response.playerData.RegistrationDate.ToString());
        infoForView.Add(response.playerData.Id.ToString());
        
        string[] finalInfoForView = infoForView.ToArray();
        int[] t = response.playerData.Landmarks?.ExternalIds?.ToArray() ?? Array.Empty<int>();

        popup.SetInfo(finalInfoForView, t);



        popup.OnSightsButtonClicked += CreateSightsPopup;
        popup.OnBackClicked += HandlePopupBackClicked;

        _currentPopup = popup.gameObject;
    }

    public async void CreateSightsPopup(int[] ids)
    {
        SightFullInfo[] allSights = await LoadAllSightsAsync(ids);

        var popup = _otherPlayerSightsDetailsViewFactory.Create();
        popup.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        //_currentPopup = popup.gameObject;
        
        foreach (var VARIABLE in allSights)
        {
                var item = _factory.Create();
                item.transform.SetParent(popup.Content, false);

                item.Title.text = VARIABLE.Title;
                item.Distance.text = "";
                item.PageId = VARIABLE.PageId;
                item.OnClicked += HandleItemClicked;

                if (_sightsUpdater.TryGetImage(VARIABLE.PageId, out var sprite))
                    item.SetImage(sprite);

                //_sightItemViews.Add(item);
        }

    }
    
    private void HandleItemClicked(int pageId)
    {
        Debug.Log("Sight clicked: " + pageId);
        _sightsUpdater.CreateSightDetailsPopup(pageId);
    }

    private void HandlePopupBackClicked()
    {
        // Вью сам делает fade-out и Destroy — здесь только сбрасываем ссылку
        _currentPopup = null;
    }

    public RootResponse DeserializePlayerResponse(string json)
    {
        return JsonConvert.DeserializeObject<RootResponse>(json);
    }
    
    private async Task<SightFullInfo[]> LoadAllSightsAsync(int[] pageIds)
    {
        Task<SightFullInfo>[] tasks = pageIds
            .Select(id => _sightService.LoadSightDetailsAsync(id))
            .ToArray();
        
        SightFullInfo[] results = await Task.WhenAll(tasks);

        return results;
    }

    public (string[] data, int[] externalIds) Parse(string json)
    {
        JObject root;

        try
        {
            root = JObject.Parse(json);
        }
        catch
        {
            Debug.LogError("GamePopupService.Parse() — JSON повреждён");
            return (Array.Empty<string>(), Array.Empty<int>());
        }

        var player = root["player"];
        var landmarks = player?["landmarks"];

        string SafeStr(JToken token) => token?.ToString() ?? "";
        
        string SafeShort(string s, int length)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Length <= length ? s : s.Substring(0, length);
        }
        
        string[] data = new string[]
        {
            SafeStr(root["success"]),
            SafeStr(player?["id"]),
            SafeStr(player?["player_id"]),
            SafeStr(player?["username"]),
            
            SafeShort(SafeStr(player?["first_name"]), 2),
            SafeShort(SafeStr(player?["last_name"]), 2),

            SafeStr(player?["registration_date"]),
            SafeStr(player?["gender"]),
            SafeStr(landmarks?["total_count"])
        };

        int[] externalIds = landmarks?["external_ids"]?
            .Where(x => x != null)
            .Select(x => int.TryParse(x.ToString(), out var id) ? id : -1)
            .Where(id => id >= 0)
            .ToArray()
            ?? Array.Empty<int>();

        return (data, externalIds);
    }
}

[Serializable]
public class RootResponse
{
    [JsonProperty("success")] public bool Success;
    [FormerlySerializedAs("Player")] [JsonProperty("player")] public PlayerData playerData;
}

[Serializable]
public class PlayerData
{
    [JsonProperty("id")] public int Id;
    [JsonProperty("player_id")] public int PlayerId;
    [JsonProperty("username")] public string Username;
    [JsonProperty("first_name")] public string FirstName;
    [JsonProperty("last_name")] public string LastName;
    [JsonProperty("registration_date")] public string RegistrationDate; 
    [JsonProperty("gender")] public string Gender;
    [JsonProperty("level")] public int Level;
    [JsonProperty("strength")] public int Strength;
    [JsonProperty("intelligence")] public int Intelligence;
    [JsonProperty("agility")] public int Agility;
    [JsonProperty("landmarks")] public Landmarks Landmarks;
    [JsonProperty("clan")] public ClanData clan;
}

[Serializable]
public class Landmarks
{
    [JsonProperty("external_ids")] public List<int> ExternalIds;
    [JsonProperty("total_count")] public int TotalCount;
}
