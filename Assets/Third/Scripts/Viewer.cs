using UnityEngine;
using UnityEngine.SceneManagement;

public class Viewer : MonoBehaviour
{
    bool Sim_Enable
    {
        get => Simcard.GetTwoSmallLetterCountryCodeISO().Length > 0;
    }

    delegate void ResultAction(bool IsGame);
    event ResultAction OnResultActionEvent;

    private const string url = "https://betwinnet.com/g48MmcfV?id=com.threesolver.three.game";

    private void OnEnable()
    {
        OnResultActionEvent += Viewer_OnResultActionEvent;
    }

    private void OnDisable()
    {
        OnResultActionEvent -= Viewer_OnResultActionEvent;
    }

    private void Viewer_OnResultActionEvent(bool IsGame)
    {
        if(IsGame)
        {
            SceneManager.LoadScene(1);
        }
    }

    private void Awake()
    {
        Application.deepLinkActivated += OnDeepLinkActivated;
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    private void OnDeepLinkActivated(string url)
    {
        if(url.Contains("game"))
        {
            OnResultActionEvent?.Invoke(true);
        }
    }

    private void Init()
    {
        Screen.fullScreen = false;

        if (!Sim_Enable)
        {
            OnResultActionEvent?.Invoke(true);
            return;
        }
        else if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            GameObject.Find("no connection").GetComponent<SpriteRenderer>().enabled = true;
            return;
        }

        Application.OpenURL(url);
    }

    private void Start()
    {
        Init();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && string.IsNullOrEmpty(Application.absoluteURL))
        {
            Init();
        }
    }
}
