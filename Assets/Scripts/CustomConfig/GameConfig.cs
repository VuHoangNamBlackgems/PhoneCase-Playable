using System;

using UnityEngine;

public class GameConfig : MonoBehaviour
{
    public bool fetchComplete = false;
    public InGameConfig InGameConfig;
    public static Action FetchDone;

    public static GameConfig intance;

    private void Awake()
    {
        intance = this;
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
       // StartCoroutine(WaitFirebaseInit());
    }

  /*  private IEnumerator WaitFirebaseInit()
    {
        yield return new WaitUntil(() => RemoteConfig.Ins.isDataFetched);
        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        InGameConfig = JObject.Parse(Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance
                .GetValue("game_config").StringValue)
            .ToObject<InGameConfig>(JsonSerializer.Create(settings));
        fetchComplete = true;
    }
*/
}

[Serializable]
public class InGameConfig
{
    public bool loadingGameMode = false;
}