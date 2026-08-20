#if UNITY_EDITOR
using UnityEngine;
using UnitySkills;

namespace RilsForUnity.Tools
{
    public static class CommandLine
    {
        public static void StartSkillsServer()
        {
            const int preferredPort = 8090;
            SkillsHttpServer.AutoStart = true;
            SkillsHttpServer.PreferredPort = preferredPort;
            SkillsHttpServer.Start(preferredPort, fallbackToAuto: true);
            Debug.Log($"[RilsForUnity.Tools] Unity Skills startup requested at {SkillsHttpServer.Url}");
        }
    }
}
#endif
