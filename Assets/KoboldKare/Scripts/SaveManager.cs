using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;
using Object = UnityEngine.Object;

public static class SaveManager {
    public const string saveDataLocation = "saves/";
    public const string saveExtension = ".sav";
    public const string imageExtension = ".jpg";
    public const string saveHeader = "KKSAVE";
    //public const string version = "0";
    public const int textureSize = 256;
    public delegate void SaveCompleteAction();
    public class SaveData {
        public SaveData(string fileName, DateTime time) {
            this.fileName = fileName;
            imageLocation = fileName.Substring(0, fileName.Length-4)+imageExtension;
            this.time = time;
            image = new Texture2D(16,16);
            if (File.Exists(imageLocation)) {
                image.LoadImage(File.ReadAllBytes(imageLocation));
            }
        }
        public readonly string imageLocation;
        public readonly Texture2D image;
        public readonly string fileName;
        public readonly DateTime time;
    }
    private static List<SaveData> saveDatas = new List<SaveData>();
    public static void Init() {
        string saveDataPath = $"{Application.persistentDataPath}/{saveDataLocation}";
        if (!Directory.Exists(saveDataPath)) {
            Directory.CreateDirectory(saveDataPath);
        }
        saveDatas.Clear();
        foreach(string fileName in Directory.EnumerateFiles(saveDataPath)) {
            if (fileName.EndsWith(saveExtension)) {
                saveDatas.Add(new SaveData(fileName, File.GetCreationTime(fileName)));
            }
        }
    }
    // Give a copy, we don't want anyone manipulating it manually.
    public static List<SaveData> GetSaveDatas() {
        return new List<SaveData>(saveDatas);
    }
    private static string PrefabifyGameObjectName(GameObject obj) {
        string name = obj.name;

        if(name.Contains("(")){
            //Debug.Log(String.Format("[SaveManager] :: Convering {0} to {1}.",name,name.Split('(')[0].Trim()));
            return name.Split('(')[0].Trim();
        }
        
        return name;
    }

    public static bool IsLoadable(string filename, out string lastError) {
        using FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using StreamReader reader = new StreamReader(file);
        string fileContents = reader.ReadToEnd();
        JSONNode node;
        try {
            node = JSONNode.Parse(fileContents);
        } catch {
            lastError = $"Not a json file: {filename}";
            return false;
        }

        if (node["header"] != saveHeader) {
            lastError = $"Not a save file: {filename}";
            return false;
        }

        if (node["version"] != PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion) {
            Debug.LogWarning("Loading old version of KoboldKare... Might not work correctly!");
        }

        lastError = "";
        return true;
    }

    public static void Save(string filename, SaveCompleteAction action = null) {
        //Debug.Log("[SaveManager] :: <Init Stage> File attempting to be saved: "+filename);
        string saveDataPath = $"{Application.persistentDataPath}/{saveDataLocation}";
        string savePath = string.Format("{0}{1}{2}", saveDataPath, filename, saveExtension);
        if (!Directory.Exists(saveDataPath)) {
            Directory.CreateDirectory(saveDataPath);
        }
        JSONNode rootNode = JSONNode.Parse("{}");
        rootNode["header"] = saveHeader;
        rootNode["version"] = PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion;
        int viewCount = 0;
        foreach (PhotonView view in PhotonNetwork.PhotonViewCollection) {
            if (view.name.Contains("DontSave")) {
                continue;
            }
            viewCount++;
        }

        JSONArray savedObjects = new JSONArray();
        // We need to enable all our saved objects, they don't have proper viewids otherwise
        foreach(PhotonView view in Object.FindObjectsOfType<PhotonView>(true)) {
            if (view.gameObject.activeInHierarchy || ((DefaultPool)PhotonNetwork.PrefabPool).ResourceCache.ContainsKey(
                    PrefabifyGameObjectName(view.gameObject))) continue;
            var gameObject = view.gameObject;
            Debug.LogError( $"Found a disabled static viewID {view.ViewID} {gameObject.name}, this is not allowed as it prevents unique id assignments!", gameObject);
            return;
        }
        foreach(PhotonView view in PhotonNetwork.PhotonViewCollection) {
            if (view.name.Contains("DontSave")) {
                continue;
            }
            JSONNode objectNode = JSONNode.Parse("{}");
            objectNode["viewID"] = view.ViewID;
            objectNode["name"] = PrefabifyGameObjectName(view.gameObject);
            foreach(var observable in view.ObservedComponents) {
                if (observable is ISavable savable) {
                    savable.Save(objectNode);
                }
            }
            savedObjects.Add(objectNode);
        }
        rootNode["objects"] = savedObjects;
        using (FileStream file = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write)) {
            using StreamWriter writer = new StreamWriter(file);
            writer.Write(rootNode.ToString());
        }
        // Save a screenshot of what's going on.
        string imageSavePath = string.Format("{0}{1}{2}", saveDataPath, filename, imageExtension);
        Screenshotter.GetScreenshot((texture)=>{
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            using(FileStream file = new FileStream(imageSavePath, FileMode.CreateNew, FileAccess.Write)) {
                byte[] jpg = texture.EncodeToJPG();
                file.Write(jpg, 0, jpg.Length);
            }
            saveDatas.Add(new SaveData(savePath, File.GetCreationTime(savePath)));
            action?.Invoke();
        });
    }

    public static bool RemoveSave(string fileName){
        string saveDataPath = $"{Application.persistentDataPath}/{saveDataLocation}";
        string imageSavePath = string.Format("{0}{1}", fileName.Substring(0,fileName.Length-4), imageExtension);
        string savePath = fileName;
        if(!File.Exists(savePath)){
            Debug.LogWarning("Indicated save file doesn't exist! ("+savePath+") Should remove from UI rather than disk. TODO: Callback.");
            return false;
        }
        else{
            File.Delete(savePath);
            File.Delete(imageSavePath); //Make sure to remove associated .jpg file of the same name too.
            //Debug.Log("Deleted file from disk: "+savePath);
            return true;
        }
    }
    private static void CleanUpImmediate() {
        foreach(PhotonView view in Object.FindObjectsOfType<PhotonView>(true)) {
            if(((DefaultPool)PhotonNetwork.PrefabPool).ResourceCache.ContainsKey(PrefabifyGameObjectName(view.gameObject))){
                PhotonNetwork.Destroy(view.gameObject);
            }
        }
    }
    private static void LoadImmediate(string filename) {
        //Debug.Log("[SaveManager] :: <Init Stage> File attempting to be loaded: "+filename);
        // Don't load saves while online.
        if (NetworkManager.instance.online) {
            return;
        }
        CleanUpImmediate();
        JSONNode rootNode;
        using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
            using StreamReader reader = new StreamReader(file);
            rootNode = JSONNode.Parse(reader.ReadToEnd());
        }
        if (rootNode["header"] != saveHeader) {
            throw new UnityException($"Not a save file: {filename}");
        }
        string fileVersion = rootNode["version"];
            
        if (fileVersion != PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion) {
            Debug.Log("Load save file with a different version, it might not load correctly...");
        }

        JSONArray array = rootNode["objects"].AsArray;
        for(int i=0;i<array.Count;i++) {
            JSONNode objectNode = array[i];
            int viewID = objectNode["viewID"];
            string prefabName = objectNode["name"];
            PhotonView view = PhotonNetwork.GetPhotonView(viewID);
            if(((DefaultPool)PhotonNetwork.PrefabPool).ResourceCache.ContainsKey(prefabName)){
                GameObject obj = PhotonNetwork.Instantiate(prefabName, Vector3.zero, Quaternion.identity);
                view = obj.GetComponent<PhotonView>();
            }
            if (view == null) {
                foreach(PhotonView deepCheck in Object.FindObjectsOfType<PhotonView>(true)) {
                    if (deepCheck.ViewID != viewID) continue;
                    view = deepCheck;
                    break;
                }
            }
            if (view == null) {
                Debug.LogError($"Failed to find view id {viewID} with name {prefabName}...Skipping");
                continue;
            }
            try {
                foreach(Component observable in view.ObservedComponents) {
                    if (observable is ISavable savable) {
                        savable.Load(objectNode);
                    }
                }
            } catch (Exception e) {
                Debug.LogError($"Failed to load observable on photonView {viewID}, {prefabName}", view);
                Debug.LogException(e);
                // Try our best to load the save... anyway
                //throw;
            }
        }
    }
    private static IEnumerator MakeSureMapIsLoadedThenLoadSave(string filename) {
        if (!IsLoadable(filename, out string lastError)) {
            PopupHandler.instance.SpawnPopup("FailedLoad");
            throw new UnityException(lastError);
        }
        //Ensure we show the player that the game is loading while we load
        if(SceneManager.GetActiveScene().name != "MainMenu"){
            GameManager.instance.Pause(false);
            GameManager.instance.loadListener.Show();
        }

        if (SceneManager.GetActiveScene().name != "MainMap") {
            yield return NetworkManager.instance.SinglePlayerRoutine();
        }
        yield return new WaitForSecondsRealtime(1f);
        try {
            LoadImmediate(filename);
        } catch {
            GameManager.instance.loadListener.Hide();
            PopupHandler.instance.SpawnPopup("FailedLoad");
            throw;
        }

        //Once loading is finished, hide loading screen
        if(SceneManager.GetActiveScene().name != "MainMenu"){
            GameManager.instance.loadListener.Hide();
        }
    }
    public static void Load(string filename) {
        //Debug.Log("[SaveManager] :: Loading in process...");
        GameManager.instance.StartCoroutine(MakeSureMapIsLoadedThenLoadSave(filename));
    }
}
