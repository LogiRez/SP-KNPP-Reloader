// Loads and unloads the tool inside CarX.
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace SpKnpp
{
    public static class Entry
    {
        private static readonly object Gate = new object();
        private static bool queued, wantLoaded;
        private static GameObject host;
        public static void Load()
        {
            Request(true);
        }
        public static void Unload()
        {
            Request(false);
        }
        private static void Request(bool load)
        {
            lock (Gate)
            {
                wantLoaded = load;
                if (queued) return;
                queued = true;
                Application.onBeforeRender += StartOnMainThread;
            }
        }
        private static void StartOnMainThread()
        {
            bool load;
            lock (Gate)
            {
                Application.onBeforeRender -= StartOnMainThread;
                load = wantLoaded;
                queued = false;
            }
            try
            {
                if (load && host == null)
                {
                    host = new GameObject("SpKnpp.Standalone");
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    host.AddComponent<Exporter>();
                }
                if (!load && host != null)
                {
                    if(!host.GetComponent<Exporter>().CanUnload) throw new InvalidOperationException("Wait for the pack reload to finish before unloading.");
                    host.SetActive(false);
                    UnityEngine.Object.Destroy(host);
                    host = null;
                }

                string suffix = System.Diagnostics.Process.GetCurrentProcess().Id + ".txt";
                string opposite = RcliSettings.RuntimeFile((load ? "unloaded-" : "loaded-") + suffix);
                if (File.Exists(opposite)) File.Delete(opposite);
                File.WriteAllText(RcliSettings.RuntimeFile((load ? "loaded-" : "unloaded-") + suffix), (load ? "GUI active at " : "Exporter deactivated at ") + DateTime.UtcNow.ToString("O"));
            }
            catch (Exception e)
            {
                Debug.LogError("Livery exporter: " + e);
                try { File.WriteAllText(RcliSettings.LogFile("load-error.txt"), e.ToString()); } catch { }
            }
        }
    }
}

