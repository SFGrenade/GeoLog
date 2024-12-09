using System.Text;
using UnityEngine;

namespace GeoLog;

public static class GameObjectUtil
{
    public static (string sceneName, string gameObjectPath) GetGoPath(this GameObject self)
    {
        StringBuilder ret = new StringBuilder();
        Transform p = self.transform;
        while (p != null)
        {
            ret.Insert(0, $"/{p.gameObject.name}");
            p = p.parent;
        }
        return (self.scene.name, ret.ToString());
    }
}