using System;
using System.Reflection;
using Modding;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using SFCore.Utils;

namespace GeoLog
{
    public class GeoLog : Mod
    {
        internal static GeoLog Instance;

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private List<KeyValuePair<string, string>> doneGos = new List<KeyValuePair<string, string>>();

        private Dictionary<string, string> pdToNameMap = new Dictionary<string, string>();

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
                pdToNameMap.Add(jes.playerDataName, Language.Language.Get(jes.nameConvo, "Journal"));
            }

            Log($"\"Scene Path\", \"GameObject Name\", Small Geo, Medium Geo, Large Geo, Total Geo, Total Geo (Greed)");
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
                if (doneGos.Exists(x => x.Key == self.gameObject.scene.path && x.Value == self.gameObject.GetGoPath())) return;
                int sg = self.GetAttr<HealthManager, int>("smallGeoDrops");
                int mg = self.GetAttr<HealthManager, int>("mediumGeoDrops");
                int lg = self.GetAttr<HealthManager, int>("largeGeoDrops");
                EnemyDeathEffects ede = self.GetAttr<HealthManager, EnemyDeathEffects>("enemyDeathEffects");
                string name = ede.GetAttr<EnemyDeathEffects, string>("playerDataName");
                Log($"{self.gameObject.scene.path}, {pdToNameMap[name]}, {sg}, {mg}, {lg}, {sg + (mg * 5) + (lg * 25)}, {Mathf.CeilToInt(sg * 1.2f) + (Mathf.CeilToInt(mg * 1.2f) * 5) + (Mathf.CeilToInt(lg * 1.2f) * 25)}");
                doneGos.Add(new KeyValuePair<string, string>(self.gameObject.scene.path, self.gameObject.GetGoPath()));
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
                    if (doneGos.Exists(x => x.Key == self.gameObject.scene.path && x.Value == self.gameObject.GetGoPath())) return;
                    var tmpV = self.FsmVariables;
                    int fp = tmpV.GetFsmInt("Final Payout").Value;
                    int gph = tmpV.GetFsmInt("Geo Per Hit").Value;
                    int h = tmpV.GetFsmInt("Hits").Value;
                    Log($"{self.gameObject.scene.path}, Geo Rock, -, -, -, {(gph * h) + fp}, {(gph * h) + fp}");
                    doneGos.Add(new KeyValuePair<string, string>(self.gameObject.scene.path, self.gameObject.GetGoPath()));
                }
                else if (self.FsmName.Equals("Shiny Control"))
                {
                    if (doneGos.Exists(x => x.Key == self.gameObject.scene.path && x.Value == self.gameObject.GetGoPath())) return;
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
                        Log($"{self.gameObject.scene.path}, {name}, -, -, -, {ret}, {ret}");
                        doneGos.Add(new KeyValuePair<string, string>(self.gameObject.scene.path, self.gameObject.GetGoPath()));
                    }
                }
            }
            catch (Exception )
            {
            }
        }
    }
}
