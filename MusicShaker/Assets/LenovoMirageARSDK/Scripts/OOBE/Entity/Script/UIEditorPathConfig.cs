// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.1433
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace LenovoMirageARSDK.OOBE
{
    using System.IO;
    /// <summary>
    /// State相关的路径
    /// </summary>
    public class StateEditorPathConfig
    {
        private const string m_ScriptAIGeneratorPath = "Assets/LenovoMirageARSDK/Scripts/OOBE/Flow";

        /// <summary>
        /// 生成Entity脚本的路径
        /// </summary>
        public static string ScriptGeneratorPath
        {
            get
            {
                string path = m_ScriptAIGeneratorPath;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static string ScriptAbsolutePath
        {
            get
            {
                string path = UnityEngine.Application.dataPath;
                path = path.Replace("Assets", "");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path + ScriptGeneratorPath;
            }
        }

        public static string StateGeneratorPath
        {
            get
            {
                string path = ScriptGeneratorPath + "/State";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
    }
}
