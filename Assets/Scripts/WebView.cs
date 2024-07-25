using System.Collections;
using System.Collections.Generic;
using Gpm.WebView;
using UnityEngine;

public class WebView : MonoBehaviour
{
    public void Run(string address)
    {
        if (!string.IsNullOrEmpty(address))
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            GpmWebView.ShowUrl(address, new GpmWebViewRequest.Configuration
            {
                style = GpmWebViewStyle.FULLSCREEN,
                orientation = GpmOrientation.UNSPECIFIED,

                isClearCache = false,
                isClearCookie = false,

                backgroundColor = "#FFFFFF",
                navigationBarColor = "#FF0000",

                title = string.Empty,

                isNavigationBarVisible = false,
                isBackButtonVisible = false,
                isForwardButtonVisible = false,
                supportMultipleWindows = true,

                position = new GpmWebViewRequest.Position(),
                size = new GpmWebViewRequest.Size(),
                margins = new GpmWebViewRequest.Margins(),
            }, OnCallback, null);
        }
    }

    private static void OnCallback(GpmWebViewCallback.CallbackType callbackType, string data, GpmWebViewError error)
    {
        switch (callbackType)
        {
            case GpmWebViewCallback.CallbackType.Open:
            case GpmWebViewCallback.CallbackType.PageStarted:
            case GpmWebViewCallback.CallbackType.PageLoad:
                if (error != null)
                {
                    Fetcher.SkipToPlug();
                }
                break;
            case GpmWebViewCallback.CallbackType.Close:
                Application.Quit();
                break;
        }
    }
}