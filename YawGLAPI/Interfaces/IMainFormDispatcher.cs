using System;
using System.Collections.Generic;

namespace YawGLAPI
{
    public interface IMainFormDispatcher
    {

        public IDeviceParameters GetDeviceParameters();
        public string GetLanguage();
        public void ShowNotification(NotificationType type, string text);
        public void DialogShow(string _string, DIALOG_TYPE type, Action<bool> _yes = null, Action<bool> _no = null, bool showChk = false, bool chkDefault = false,string chkText = "");

        public string GetInstallPath(string name);
        public void OpenPluginManager();
        public void ExitGame();

        public bool GetObjectFile<T>(string objectPath,out T obj);

        public void RestartApp(bool admin);
        public void ExtractToDirectory(string sourceArchiveFileName,
                                              string destinationDirectoryName,
                                              bool overwrite);

        public List<Profile_Component> JsonToComponents(string json);
        public LedEffect JsonToLED(string json);

        public T GetConfigObject<T>();

    }
}
