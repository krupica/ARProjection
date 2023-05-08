using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Base;

//This class was taken from AREditor and modified for the needs of this application. (https://github.com/robofit/arcor2_areditor)
public class LandingScreen : Base.Singleton<LandingScreen>
{
    public TMPro.TMP_InputField Domain, Port;
    public Button ConnectToServerBtn;

    private void Start() {
        Domain.text = PlayerPrefs.GetString("arserver_domain", "");
        Port.text = PlayerPrefs.GetInt("arserver_port", 6789).ToString();
        ConnectToServerBtn.onClick.AddListener(() => ConnectToServer(true));
    }

    public void ConnectToServer(bool force = true) {
        if (!force) {
            if (PlayerPrefs.GetInt("arserver_keep_connected", 0) == 0) {
                return;
            }
        }
        string domain = Domain.text;
        int port = int.Parse(Port.text);
        PlayerPrefs.SetString("arserver_domain", domain);
        PlayerPrefs.SetInt("arserver_port", port);
        PlayerPrefs.SetString("arserver_username", "test");
        PlayerPrefs.SetInt("arserver_keep_connected", 0);
        PlayerPrefs.Save();
        SceneManager.Instance.DestroyScene();
        Base.GameManager.Instance.ConnectToSever(domain, port);
    }

    internal string GetUsername() {
        return "ARProjection";
    }
}
