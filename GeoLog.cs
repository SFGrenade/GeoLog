using System;
using System.Reflection;
using Modding;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using HutongGames.PlayMaker.Actions;
using UnityEngine.SceneManagement;
using SFCore.Utils;

namespace GeoLog
{
    public class GeoLog : Mod
    {
        internal static GeoLog Instance;

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private List<string> _doneGos = new List<string>();

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
            On.HealthManager.Start += OnHealthManagerStart;
            On.SceneManager.Start += OnSceneManagerStart;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            foreach (var jes in Resources.FindObjectsOfTypeAll<JournalEntryStats>())
            {
                if (jes.playerDataName.Equals("Crawler") && jes.nameConvo.Equals("NAME_HOLLOW_SHADE")) continue;
                Log($"\"{jes.playerDataName}\": \"{jes.nameConvo}\"");
                _pdToNameMap.Add(jes.playerDataName, Language.Language.Get(jes.nameConvo, "Journal"));
            }

            Log($"\"Scene and GameObject Path\",\"Name\",\"Small Geo (geo per hit when geo rock)\",\"Medium Geo (hits until final payout when geo rock)\",\"Large Geo (final payout when geo rock)\",\"Total Geo\",\"Total Geo (Greed)\"");
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
                if (_doneGos.Exists(x => x == self.gameObject.GetGoPath())) return;
                int sg = self.GetAttr<HealthManager, int>("smallGeoDrops");
                int mg = self.GetAttr<HealthManager, int>("mediumGeoDrops");
                int lg = self.GetAttr<HealthManager, int>("largeGeoDrops");
                EnemyDeathEffects ede = self.GetAttr<HealthManager, EnemyDeathEffects>("enemyDeathEffects");
                string name = ede.GetAttr<EnemyDeathEffects, string>("playerDataName");
                Log($"\"{self.gameObject.GetGoPath()}\",\"{_pdToNameMap[name]}\",\"{sg}\",\"{mg}\",\"{lg}\",\"{sg + (mg * 5) + (lg * 25)}\",\"{Mathf.CeilToInt(sg * 1.2f) + (Mathf.CeilToInt(mg * 1.2f) * 5) + (Mathf.CeilToInt(lg * 1.2f) * 25)}\"");
                _doneGos.Add(self.gameObject.GetGoPath());
            }
            catch (Exception )
            {
            }
        }

        private void CheckFsm(PlayMakerFSM self)
        {
            try
            {
                if (self.FsmName.Equals("Geo Rock"))
                {
                    if (_doneGos.Exists(x => x == self.gameObject.GetGoPath())) return;
                    var tmpV = self.FsmVariables;
                    GeoControl fpGC = self.GetAction<FlingObjectsFromGlobalPool>("Destroy", 2).gameObject.Value.GetComponent<GeoControl>();
                    int fpValue = fpGC.sizes[fpGC.type].value;
                    int fp = tmpV.GetFsmInt("Final Payout").Value * fpValue;
                    GeoControl gphGC = self.GetAction<FlingObjectsFromGlobalPool>("Hit", 1).gameObject.Value.GetComponent<GeoControl>();
                    int gphValue = gphGC.sizes[gphGC.type].value;
                    int gph = tmpV.GetFsmInt("Geo Per Hit").Value * gphValue;
                    int h = tmpV.GetFsmInt("Hits").Value;
                    Log($"\"{self.gameObject.GetGoPath()}\",\"Geo Rock\",\"{gph}\",\"{h}\",\"{fp}\",\"{(gph * h) + fp}\",\"{(gph * h) + fp}\"");
                    _doneGos.Add(self.gameObject.GetGoPath());
                }
                else if (self.FsmName.Equals("Shiny Control"))
                {
                    if (_doneGos.Exists(x => x == self.gameObject.GetGoPath())) return;
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
                        Log($"\"{self.gameObject.GetGoPath()}\",\"{name}\",\"-\",\"-\",\"-\",\"{ret}\",\"{ret}\"");
                        _doneGos.Add(self.gameObject.GetGoPath());
                    }
                }
            }
            catch (Exception )
            {
            }
        }
    }
}
