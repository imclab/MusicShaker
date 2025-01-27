﻿// Ximmerse Defines|SDK_Ximmerse|001
namespace VRTK
{
    /// <summary>
    /// Handles all the scripting define symbols for the Ximmerse SDK.
    /// </summary>
    public static class SDK_XimmerseDefines
    {
        /// <summary>
        /// The scripting define symbol for the Ximmerse SDK.
        /// </summary>
        public const string ScriptingDefineSymbol = SDK_ScriptingDefineSymbolPredicateAttribute.RemovableSymbolPrefix + "SDK_XIMMERSE";

        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, "Standalone")]
        [SDK_ScriptingDefineSymbolPredicate(ScriptingDefineSymbol, "Android")]
        private static bool IsXimmerseAvailable()
        {
            //@EDIT:禁用XimmerseSDK，防止SDK冲突
            //Original----------------------------------------
            //VRTK_SharedMethods.GetTypeUnknownAssembly("Ximmerse.InputSystem.XDevicePlugin") != null;
            //Original----------------------------------------
            //Now----------------------------------------
            return false;
            //Now----------------------------------------            
        }
    }
}