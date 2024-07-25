using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Startup : MonoBehaviour
{
    [SerializeField]
    private int _plugSceneBuildIndex;
    
    [SerializeField]
    private Button _startButton;
    
    [SerializeField]
    private WebView _webView;
    
    [SerializeField]
    private GameObject _loadingScreen;

    private int _clicksCount;
    private bool _isBot;

    private void Start()
    {
        StartCoroutine(FakeLoading());
    }

    private void Update()
    {
#if UNITY_ANDROID
        for (var i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase is not TouchPhase.Began)
            {
                continue;
            }
            
            _clicksCount++;
            Debug.Log("clicks:" + _clicksCount);
            if (_clicksCount > 2)
            {
                _isBot = true;
            }
        }
#endif
    }

    private IEnumerator FakeLoading()
    {
        yield return new WaitForSeconds(2);
        if (!_isBot)
        {
            _startButton.onClick.AddListener(() =>
            {
                Fetcher.DoAsync(_plugSceneBuildIndex, _webView);
            });
        }
        else
        {
            _startButton.onClick.AddListener(Fetcher.SkipToPlug);
        }
        _startButton.gameObject.SetActive(true);
        _loadingScreen.SetActive(false);
    }
}
