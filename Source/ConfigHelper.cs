using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using KSP.IO;

namespace HyperEditX {

  /* Combining all the savedata into one neat file.
     Use either ConfigNode or XML??

     Files to combine: windowpos, hyperedit cfg, landing coordinates.
  */
  
  public static class ConfigHelper {
    //private static readonly string Plugin_Dir = System.IO.Path.Combine(System.IO.Path.ChangeExtension(typeof(ConfigHelper).Assembly.Location, null), "..");
    //private static readonly string PluginDir = Path.ChangeExtension(typeof(ConfigHelper).Assembly.Location, null);
    private static readonly string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); //.Replace("\\", "/");
    private static readonly string PluginDataDir = System.IO.Path.Combine(PluginDir, "PluginData");

    //private PluginConfiguration config;
    

    static ConfigHelper() {
      if (!System.IO.Directory.Exists(PluginDataDir)) {
        System.IO.Directory.CreateDirectory(PluginDataDir);

        //Should probably use this to create default data?
      }

      Utils.Log("ConfigHelper");
      //Utils.Log("Using '" + PluginDataDir + "' as root config directory");
      Utils.Log("PluginDir     = " + PluginDir);
      Utils.Log("PluginDataDir = " + PluginDataDir);
      Utils.Log("------------");
    }

    public static string GetPath(string path) => path == null ? PluginDataDir : System.IO.Path.Combine(PluginDataDir, path);

    public static void Save(this ConfigNode config) => config.Save(GetPath(config.name + ".cfg"));

  }
}
