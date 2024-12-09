using System;
using System.Reflection;
using Modding;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HutongGames.PlayMaker.Actions;
using UnityEngine.SceneManagement;
using SFCore.Utils;

namespace GeoLog;

public class GeoLog : Mod
{
    public enum GeoSourceType
    {
        Enemy,
        GeoRock,
        Relic
    };

    internal static GeoLog Instance;
    private readonly string _dir;
    private FileStream _fileStream;

    private void StartCSV()
    {
        WriteLine(
            $"\"Scene\",\"Type\",\"GameObject Path\",\"Name\",\"Total Small Geo\",\"Total Medium Geo\",\"Total Large Geo\",\"Total Geo\",\"Greed Bonus\"");
    }

    private void LogCSV(GeoSourceType type, GameObject go, string name, string numSmallPieces, string numMediumPieces, string numLargePieces, string totalGeo,
        string greedBonus)
    {
        if (_doneGos.Exists(x => x == go.GetGoPath())) return;
        (string sceneName, string goPath) = go.GetGoPath();
        WriteLine(
            $"\"{sceneName}\",\"{type.ToString()}\",\"{goPath}\",\"{name}\",\"{numSmallPieces}\",\"{numMediumPieces}\",\"{numLargePieces}\",\"{totalGeo}\",\"{greedBonus}\"");
        _doneGos.Add(go.GetGoPath());
    }

    private void LogCSV(GeoSourceType type, GameObject go, string name, int numSmallPieces, int numMediumPieces, int numLargePieces, bool canGreed)
    {
        string numSmallPiecesStr = "-";
        if (numSmallPieces != -1)
            numSmallPiecesStr = $"{numSmallPieces}";
        string numMediumPiecesStr = "-";
        if (numMediumPieces != -1)
            numMediumPiecesStr = $"{numMediumPieces}";
        string numLargePiecesStr = "-";
        if (numLargePieces != -1)
            numLargePiecesStr = $"{numLargePieces}";

        string totalGeoStr = "-";
        int totalGeo = 0;
        if (numSmallPieces != -1)
            totalGeo += numSmallPieces;
        if (numMediumPieces != -1)
            totalGeo += numMediumPieces * 5;
        if (numLargePieces != -1)
            totalGeo += numLargePieces * 25;
        totalGeoStr = $"{totalGeo}";

        string greedBonusStr = "-";
        if (canGreed)
        {
            int greedBonus = 0;
            if (numSmallPieces != -1)
                greedBonus += Mathf.CeilToInt(numSmallPieces * 0.2f);
            if (numMediumPieces != -1)
                greedBonus += Mathf.CeilToInt(numMediumPieces * 0.2f) * 5;
            if (numLargePieces != -1)
                greedBonus += Mathf.CeilToInt(numLargePieces * 0.2f) * 25;
            greedBonusStr = $"{greedBonus}";
        }

        LogCSV(type, go, name, numSmallPiecesStr, numMediumPiecesStr, numLargePiecesStr, totalGeoStr, greedBonusStr);
    }

    private void LogCSVEnemy(GameObject go, string name, int numSmallPieces, int numMediumPieces, int numLargePieces)
    {
        LogCSV(GeoSourceType.Enemy, go, name, numSmallPieces, numMediumPieces, numLargePieces, true);
    }

    private void LogCSVGeoRock(GameObject go, string name, GeoControl.Size[] piecesPerHitSizes, int piecesPerHitSize, GeoControl.Size[] finalPayoutSizes,
        int finalPayoutSize, int geoPerHit, int amountHits, int finalPayout)
    {
        int piecesPerHitValue = piecesPerHitSizes[piecesPerHitSize].value;
        int finalPayoutValue = finalPayoutSizes[finalPayoutSize].value;
        int totalGeoAmount = ((geoPerHit * piecesPerHitValue) * amountHits) + (finalPayout * finalPayoutValue);

        if (piecesPerHitValue == 1)
        {
            if (finalPayoutValue == 1)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"({geoPerHit}*{amountHits})+{finalPayout}", $"-", $"-", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 5)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"({geoPerHit}*{amountHits})", $"{finalPayout}", $"-", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 25)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"({geoPerHit}*{amountHits})", $"-", $"{finalPayout}", $"{totalGeoAmount}", "-");
            }
        }
        else if (piecesPerHitValue == 5)
        {
            if (finalPayoutValue == 1)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"{finalPayout}", $"({geoPerHit}*{amountHits})", $"-", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 5)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"-", $"({geoPerHit}*{amountHits})+{finalPayout}", $"-", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 25)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"-", $"({geoPerHit}*{amountHits})", $"{finalPayout}", $"{totalGeoAmount}", "-");
            }
        }
        else if (piecesPerHitValue == 25)
        {
            if (finalPayoutValue == 1)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"{finalPayout}", $"-", $"({geoPerHit}*{amountHits})", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 5)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"-", $"{finalPayout}", $"({geoPerHit}*{amountHits})", $"{totalGeoAmount}", "-");
            }
            else if (finalPayoutValue == 25)
            {
                LogCSV(GeoSourceType.GeoRock, go, name, $"-", $"-", $"({geoPerHit}*{amountHits})+{finalPayout}", $"{totalGeoAmount}", "-");
            }
        }
    }

    private void LogCSVRelic(GameObject go, string name, int geoSellAmount)
    {
        LogCSV(GeoSourceType.Relic, go, name, "-", "-", "-", $"{geoSellAmount}", "-");
    }

    public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private List<(string sceneName, string goPath)> _doneGos = new();

    private Dictionary<string, string> _pdToNameMap = new Dictionary<string, string>();

    public override List<ValueTuple<string, string>> GetPreloadNames()
    {
        var dict = new List<ValueTuple<string, string>>();
        int max = 499;
        //max = 3;
        for (int i = 0; i < max; i++)
        {
            switch (i)
            {
                case 0:
                case 1:
                case 2:
                    continue;
            }

            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            dict.Add((Path.GetFileNameWithoutExtension(scenePath), "_SceneManager"));
        }

        return dict;
    }

    public GeoLog() : base("Geo Log")
    {
        _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
               throw new DirectoryNotFoundException("I have no idea how you did this, but good luck figuring it out.");
        _fileStream = new FileStream(Path.Combine(_dir, "Geo.csv"), FileMode.Create);

        On.HealthManager.Start += OnHealthManagerStart;
        On.SceneManager.Start += OnSceneManagerStart;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        foreach (var jes in Resources.FindObjectsOfTypeAll<JournalEntryStats>())
        {
            if (jes.playerDataName.Equals("Crawler") && jes.nameConvo.Equals("NAME_HOLLOW_SHADE")) continue;
            Log($"\"{jes.playerDataName}\": \"{jes.nameConvo}\"");
            _pdToNameMap.Add(jes.playerDataName, Language.Language.Get(jes.nameConvo, "Journal"));
        }

        StartCSV();
    }

    private void OnSceneManagerStart(On.SceneManager.orig_Start orig, SceneManager self)
    {
        orig(self);
        Check();
    }

    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode lsm)
    {
        Log($"Scene '{loadedScene.name}' loaded!");
        Check();
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        Instance = this;
    }

    private void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);
        Check();
    }

    private void Check()
    {
        foreach (var hm in Resources.FindObjectsOfTypeAll<HealthManager>())
        {
            CheckHealthManager(hm);
        }

        foreach (var fsm in Resources.FindObjectsOfTypeAll<PlayMakerFSM>())
        {
            CheckFsm(fsm);
        }
    }

    private void CheckHealthManager(HealthManager self)
    {
        try
        {
            int sg = self.GetAttr<HealthManager, int>("smallGeoDrops");
            int mg = self.GetAttr<HealthManager, int>("mediumGeoDrops");
            int lg = self.GetAttr<HealthManager, int>("largeGeoDrops");
            EnemyDeathEffects ede = self.GetAttr<HealthManager, EnemyDeathEffects>("enemyDeathEffects");
            string name = ede.GetAttr<EnemyDeathEffects, string>("playerDataName");
            LogCSVEnemy(self.gameObject, _pdToNameMap[name], sg, mg, lg);
        }
        catch (Exception)
        {
        }
    }

    private void CheckFsm(PlayMakerFSM self)
    {
        try
        {
            if (self.FsmName.Equals("Geo Rock"))
            {
                var tmpV = self.FsmVariables;
                GeoControl fpGC = self.GetAction<FlingObjectsFromGlobalPool>("Destroy", 2).gameObject.Value.GetComponent<GeoControl>();
                GeoControl gphGC = self.GetAction<FlingObjectsFromGlobalPool>("Hit", 1).gameObject.Value.GetComponent<GeoControl>();
                LogCSVGeoRock(self.gameObject, "Geo Rock", gphGC.sizes, gphGC.type, fpGC.sizes, fpGC.type, tmpV.GetFsmInt("Geo Per Hit").Value,
                    tmpV.GetFsmInt("Hits").Value, tmpV.GetFsmInt("Final Payout").Value);
            }
            else if (self.FsmName.Equals("Shiny Control"))
            {
                var tmpV = self.FsmVariables;
                int trinketNum = tmpV.GetFsmInt("Trinket Num").Value;
                if (trinketNum > 0 && trinketNum < 5)
                {
                    int ret = 0;
                    string name = "";
                    switch (trinketNum)
                    {
                        case 1:
                            ret = 200;
                            name = "Wanderer's Journal";
                            break;
                        case 2:
                            ret = 450;
                            name = "Hallownest Seal";
                            break;
                        case 3:
                            ret = 800;
                            name = "King's Idol";
                            break;
                        case 4:
                            ret = 1200;
                            name = "Arcane Egg";
                            break;
                    }

                    LogCSVRelic(self.gameObject, name, ret);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private void WriteLine(string line)
    {
        byte[] lineBytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        _fileStream.Write(lineBytes, 0, lineBytes.Length);
    }
}