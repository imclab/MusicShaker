﻿//=============================================================================
//
// Copyright 2016 Ximmerse, LTD. All rights reserved.
//
//=============================================================================
//@EDIT:USE_MIRAGE_API Define
#define USE_MIRAGE_API
//#define UNITY_ANDROID
//#undef UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;
using Ximmerse.VR;
using AOT;

namespace Ximmerse.InputSystem
{

    /// <summary>
    /// C# wrapper for X-Device SDK.
    /// </summary>
    public partial class XDevicePlugin
    {
        //@EDITOR:Add IsClient
        /// <summary>
        /// IS Run As Client ,Default is False
        /// </summary>
        public static bool IsClient = false;

        public static int DeviceVersion = XIM_DK4_DIS01;//

        #region Const

        /// <summary>
        /// Dll name
        /// </summary>
        public const string LIB_XDEVICE =
#if (UNITY_IOS && !UNITY_EDITOR) //|| UNITY_XBOX360
			// On iOS and Xbox 360 plugins are statically linked into the executable,
			// so we have to use __Internal as the library name.
			"__Internal"
#else
            "xdevice"
#endif
        ;

        #endregion Const

        #region Nested Types

        public delegate void VoidDelegate(int which);
        public delegate void AxesDelegate(int which, int axis, float value);
        public delegate void KeyDelegate(int which, int code, int action);
        public delegate void Vector3Delegate(int which, float x, float y, float z);
        public delegate void Vector4Delegate(int which, float x, float y, float z, float w);
        public delegate int ControllerStateDelegate(int which, ref ControllerState state);
        public delegate int SendMessageDelegate(int which, int Msg, int wParam, int lParam);
        public delegate int LogDelegate(int level, System.IntPtr tag, System.IntPtr msg);
        public delegate void calibration_delegate(int which, int state, ref IMUCalibrationResult result);
        
        /// <summary>
        /// A struct contains Cobra hand controller info, including button mask, axes value, positions, rotations, etc
        /// </summary>
		[StructLayout(LayoutKind.Sequential)]
        public struct ControllerState
        {

            // Common
            public int handle;
            public int timestamp;
            public int frameCount;
            public int state_mask;

            // Gamepad
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] axes;
            public uint buttons;

            // Motion
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] position;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] rotation;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] accelerometer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] gyroscope;

            public static ControllerState Obtain()
            {
                return new ControllerState(-1);
            }

            public ControllerState(int myHandle)
            {

                // Common
                handle = myHandle;
                timestamp = 0;
                frameCount = 0;
                state_mask = 0;

                // Gamepad
                axes = new float[6];
                buttons = 0u;

                // Motion
                position = new float[3];
                rotation = new float[4];
                accelerometer = new float[3];
                gyroscope = new float[3];
            }
        }

        /// <summary>
        /// A struct that contains raw camera tracker data. 
        /// </summary>
		[StructLayout(LayoutKind.Sequential)]
        public struct TrackerState
        {

            public const int POINT_DATA_SIZE = 3;

            public int handle;
            public int timestamp;
            public int frameCount;
            public int capacity;
            public int count;
            public System.IntPtr id;
            public System.IntPtr data;

        }


        [StructLayout(LayoutKind.Sequential)]
        public struct IMUCalibrationResult
        {
            public int timestamp;
            public int frameCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] center;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public float[] transform;
            public float confidence;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] magRaw;
        }

        #endregion Nested Types

        #region Natives
        public static AndroidJavaClass s_XDeviceApi;

        internal static class NativeMethods
        {
            private const string pluginName = LIB_XDEVICE;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceInit();

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceExit();

            /** Dummy functions.*/
            public static void XDeviceOnPause() { }
            public static void XDeviceOnResume() { }
            public static void XDeviceGetInputDeviceHandle() { }

#elif UNITY_ANDROID

            public static int XDeviceInit()
            {
                Debug.Log("XDeviceInit");
                if (XDevicePlugin.s_XDeviceApi == null)
                {
#if USE_MIRAGE_API
                    XDevicePlugin.s_XDeviceApi = new AndroidJavaClass("com.naocy.mirageapi.MirageApi");
                    XDevicePlugin.s_XDeviceApi.CallStatic("setListener", new LenovoMirageARSDK.MirageInitListener());

#else
                    XDevicePlugin.s_XDeviceApi = new AndroidJavaClass("com.ximmerse.sdk.XDeviceApi");                    
#endif
                }
                int ret = -1;
                if ((DeviceVersion == XIM_DK4_DIS01) || (DeviceVersion == XIM_RD06))
                {
                    using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        using (AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity"))
                        {
                            ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("init", currentActivity, DeviceVersion, 4);
                            SetBool(ID_CONTEXT, kField_CtxCanProcessInputEventBool, false);
                        }
                    }
                }
                else if (PlayerPrefsEx.GetBool("xdevice.noServicesInAndroid", false))
                {
                    using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        using (AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity"))
                        {
                            ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("init", currentActivity, 0x0, 1);
                            SetBool(ID_CONTEXT, kField_CtxCanProcessInputEventBool, false);
                        }
                    }
                }
                else
                {
                    ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("initInUnity");
                }
                return ret;
            }

            public static int XDeviceExit()
            {
                int ret = 0;
                if (XDevicePlugin.s_XDeviceApi != null)
                {
                    ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("exit");
                    XDevicePlugin.s_XDeviceApi.Dispose();
                    XDevicePlugin.s_XDeviceApi = null;
                }
                return ret;
            }

            public static void XDeviceOnPause()
            {
                if (XDevicePlugin.s_XDeviceApi != null)
                {
                    XDevicePlugin.s_XDeviceApi.CallStatic("onPause");
                }
            }

            public static void XDeviceOnResume()
            {
                if (XDevicePlugin.s_XDeviceApi != null)
                {
                    XDevicePlugin.s_XDeviceApi.CallStatic("onResume");
                }
            }

#endif

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern System.IntPtr XDeviceGetContext(bool autoCreate);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern System.IntPtr XDeviceGetInputDevice(int which);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInputDeviceHandle(System.IntPtr name);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern System.IntPtr XDeviceGetInputDeviceName(int which);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInputDevices(int type, int[] whichBuffer, int whichBufferSize);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern System.IntPtr XDeviceRemoveInputDeviceAt(int which, bool dispose);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceAddExternalControllerDevice(System.IntPtr name, ControllerStateDelegate converter, SendMessageDelegate sender);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInputState(int which, System.IntPtr state);
#if ((UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS) && !UNITY_EDITOR_WIN)
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int ControllerStateFromPtr(ref ControllerState state, System.IntPtr ptr);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int TrackerStateFromPtr(ref TrackerState state, System.IntPtr ptr);
#else
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInputState(int which, ref ControllerState state);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInputState(int which, ref TrackerState state);
#endif
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceSendMessage(int which, int Msg, int wParam, int lParam);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceSendRecenterMessage(int which, float wParam, int lParam);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceUpdateInputState(int which);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool XDeviceGetBool(int which, int fieldID, bool defaultValue);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceSetBool(int which, int fieldID, bool value);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetInt(int which, int fieldID, int defaultValue);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceSetInt(int which, int fieldID, int value);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern float XDeviceGetFloat(int which, int fieldID, float defaultValue);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceSetFloat(int which, int fieldID, float value);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceGetObject(int which, int fieldID, System.IntPtr buffer, int offset, int count);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceSetObject(int which, int fieldID, System.IntPtr buffer, int offset, int count);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern System.IntPtr XDeviceGetString(int which, int fieldID, System.IntPtr defaultValue);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void XDeviceSetString(int which, int fieldID, System.IntPtr value);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetNodePosition(int which, int history, int node, float[] position);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceUpdateNodePose(int which, int node, float[] position, float[] rotation);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetTrackerPose(int which, out float height, out float depth, out float pitch);
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceSetTrackerPose(int which, float height, float depth, float pitch);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceGetTickCount();
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
            public static int XDeviceSetLogger(LogDelegate logger)
            {
                return -1;
            }
#else
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceSetLogger(LogDelegate logger);
#endif

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool SetHeadRotation(float[] quaternion, float pitch = 0, float yaw = 0, float roll = 0);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool XDeviceSetDecoderFeature(int feature);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XHawkGetSmoothPosition(int node, float[] position);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int XDeviceSetCalibrationCallback(calibration_delegate userCallback);

        }

        #endregion Natives

        #region Methods

        protected static bool s_IsInited = false;
        protected static LogDelegate s_Logger = (level, tag, msg) =>
        {
            return 0;
            switch ((int)level)
            {
                case (int)LOGLevel.LOG_VERBOSE: Log.v(Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(msg)); break;
                case (int)LOGLevel.LOG_DEBUG: Log.d(Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(msg)); break;
                case (int)LOGLevel.LOG_INFO: Log.i(Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(msg)); break;
                case (int)LOGLevel.LOG_WARN: Log.w(Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(msg)); break;
                case (int)LOGLevel.LOG_ERROR: Log.e(Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(msg)); break;
                default: break;
            };

        };


        /// <summary>
        /// Initialize the X-Device SDK library.
        /// </summary>
        public static int Init()
        {
            // Initialization Lock.            
            if (s_IsInited) return 0;
            s_IsInited = true;

            //@EDIT:to fix Editor play 3 times,Unity Editor will crash
            //--------------------------------------------------------
#if UNITY_EDITOR
            NativeMethods.XDeviceExit();
#endif
            //--------------------------------------------------------

            // Init the device context.
            NativeMethods.XDeviceGetContext(true);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            //
            string pluginPath = UnityEngine.Application.dataPath + "\\Plugins\\"
#if UNITY_EDITOR_WIN
                + (GetInt(XDevicePlugin.ID_CONTEXT,/*FieldID::*/kField_CtxPlatformVersionInt, 0) % 2 == 0 ? "x86" : "x86_64");
#elif UNITY_STANDALONE_WIN
				;
#endif
            string path = System.Environment.GetEnvironmentVariable("PATH");
            if (path.IndexOf(pluginPath) == -1)
            {
                System.Environment.SetEnvironmentVariable("PATH", path + ";" + pluginPath);
            }
#endif
            NativeMethods.XDeviceSetInt(ID_CONTEXT, kField_CtxVIDInt, 0x1F3B);
            NativeMethods.XDeviceSetInt(ID_CONTEXT, kField_CtxPIDInt, 0);

            NativeMethods.XDeviceSetInt(XDevicePlugin.ID_CONTEXT, kField_CtxDeviceVersionInt,
#if (UNITY_EDITOR || UNITY_STANDALONE) && XIM_SDK_PREVIEW
				-1
#else
                DeviceVersion
#endif
            );
            NativeMethods.XDeviceSetLogger(null);//s_Logger);
            NativeMethods.XDeviceSetInt(XDevicePlugin.ID_CONTEXT, kField_CtxLogLevelInt, (int)LOGLevel.LOG_OFF);

            // set smooth level
            NativeMethods.XDeviceSetInt(XDevicePlugin.ID_CONTEXT, kField_CtxSmoothLevelInt, 1);

            //
            int ret = NativeMethods.XDeviceInit();
            // Add Unity objects into X-Device plugin.
            AddExternalControllerDevice("VRDevice", VRContext.s_OnHmdUpdate, VRContext.s_OnHmdMessage);
            DeviceVersion = NativeMethods.XDeviceGetInt(XDevicePlugin.ID_CONTEXT, kField_CtxDeviceVersionInt, 0);            
            //
            Log.i("XDevicePlugin", "Init the plugin(" + ret + ")...");
            return ret;
        }

        /// <summary>
        /// Finalize the X-Device SDK library.
        /// </summary>
        public static int Exit()
        {
            // Initialization Lock.
            if (!s_IsInited) return 0;
            s_IsInited = false;
            //TODO 
            //#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            //			int ret = 0;
            //#else
            int ret = NativeMethods.XDeviceExit();
            //#endif
            //
            Log.i("XDevicePlugin", "Exit the plugin(" + ret + ")...");
            return ret;
        }

        public static void OnPause()
        {
            NativeMethods.XDeviceOnPause();
        }

        public static void OnResume()
        {
            NativeMethods.XDeviceOnResume();
        }

        /// <summary>
        /// Get the handle of input device identified by name.
        /// </summary>
        /// <param name="name">Available parameters: 
        ///     XCobra-0 : represents left controller;
        ///     XCobra-1 : represents right controller;
        ///     XHawk-0 : represents Hawk tracking camera;
        /// </param>
        /// <returns>A handle to the device, which is an int.</returns>
        /// <example> 
        /// This example shows how to use the <see cref="GetInputDeviceHandle"/> method.
        /// <code>
        /// using UnityEngine;
        /// using Ximmerse;
        /// using Ximmerse.InputSystem;
        /// 
        /// class TestClass : MonoBehaviour
        /// {
        ///     private int m_leftControllerHandle;
        ///     private int m_hawkHandle;
        ///     
        ///     private XDevicePlugin.ControllerState m_leftControllerState;
        ///     private XDevicePlugin.TrackingStateEx m_hawkState;
        ///     
        ///     void Awake() 
        ///     {
        ///         XDevicePlugin.Init();
        ///         m_leftControllerHandle = XDevicePlugin.GetInputDeviceHandle("XCobra-0");
        ///         m_hawkHandle = XDevicePlugin.GetInputDeviceHandle("XHawk-0");
        ///     }
        ///     void Update()
        ///     {
        ///         // if this is larger than 0, it means it is valid input device;
        ///         if (m_leftControllerHandle >= 0)
        ///         {
        ///             UpdateLeftController();
        ///         }
        ///         if(m_hawkHandle>=0)
        ///         {
        ///             UpdateHawk();
        ///         }
        ///     }
        ///     
        ///     private void UpdateLeftController()
        ///     {
        ///         // You have to update the state manually. 
        ///         XDevicePlugin.UpdateInputState(m_leftControllerHandle);
        ///         XDevicePlugin.GetInputState(m_leftControllerHandle, ref m_leftControllerState);
        ///         var trigger = m_leftControllerState.axes[(int)ControllerRawAxis.LeftTrigger];
        ///         var xAxis = m_leftControllerState.axes[(int)ControllerRawAxis.LeftThumbX];
        ///         var yAxis = m_leftControllerState.axes[(int)ControllerRawAxis.LeftThumbY];
        ///         var orientation = new Quaternion(
        ///                 -m_leftControllerState.rotation[0],
        ///                 -m_leftControllerState.rotation[1],
        ///                  m_leftControllerState.rotation[2],
        ///                  m_leftControllerState.rotation[3]
        ///             );
        ///     }
        ///
        ///     private void UpdateHawk()
        ///     {
        ///         // You have to update the state manually. 
        ///         XDevicePlugin.UpdateInputState(m_hawkHandle);
        ///         XDevicePlugin.GetInputState(m_hawkHandle, ref m_hawkState);
        ///         // 0 = left controller, 1 = right controller 
        ///         int floatOffset = m_hawkState.OffsetOf(0);
        ///         // if this is -1, the position can not be found
        ///         // return to 0 if point is valid
        ///         if (floatOffset >= 0)
        ///         {
        ///             float[] rawPostionData = m_hawkState.GetData();
        ///             // convert the position to 1:1 scale movement. 
        ///             float movementScale = 0.001f;
        ///             Vector3 position = new Vector3(
        ///                          m_hawkState.data[floatOffset++] * movementScale,
        ///                          m_hawkState.data[floatOffset++] * movementScale,
        ///                         -m_hawkState.data[floatOffset++] * movementScale
        ///                     );
        ///             // todo : this position needs to be transformed into different space depending on what user is trying to do.
        ///             // Check out TrackingInput.cs line 148-160 for reference.
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static int GetInputDeviceHandle(string name)
        {
            int ret = -1;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            System.IntPtr ptr = Marshal.StringToHGlobalAnsi(name);
            ret = NativeMethods.XDeviceGetInputDeviceHandle(ptr);
            Marshal.FreeHGlobal(ptr);
#elif UNITY_ANDROID
            ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("getInputDeviceHandle", name);
#endif
            return ret;
        }
        /// <summary>
        /// Get the name of input device identified by handle.
        /// </summary>
        public static string GetInputDeviceName(int which)
        {
            return Marshal.PtrToStringAnsi(NativeMethods.XDeviceGetInputDeviceName(which));
        }

        /// <summary>
        /// Get count of input devices in X-Device SDK.
        /// </summary>
        public static int GetInputDeviceCount()
        {
            return NativeMethods.XDeviceGetInputDevices(-1, null, 0);
        }

        //@EDIT:add method for check the app is client or not
        /// <summary>
        /// check the app is client or not,be sure use this after the XDevicePlugin.Init() method execute
        /// </summary>
        /// <returns></returns>
        public static bool CheckIsClient()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            IsClient= XDevicePlugin.s_XDeviceApi == null ? false : XDevicePlugin.s_XDeviceApi.CallStatic<bool>("isClient");
#endif
            return IsClient;
        }

        /// <summary>
        /// Get handles of all input devices identified by type.
        /// </summary>
        public static int GetInputDevices(int type, int[] whichBuffer, int whichBufferSize)
        {
            return NativeMethods.XDeviceGetInputDevices(type, whichBuffer, whichBufferSize);
        }
        /// <summary>
        /// Add an external controller device to X-Device SDK.
        /// </summary>
        public static int AddExternalControllerDevice(string name, ControllerStateDelegate converter, SendMessageDelegate sender)
        {
#if USE_MIRAGE_API
            return 0;
#else
            System.IntPtr ptr = Marshal.StringToHGlobalAnsi(name);
            int ret = NativeMethods.XDeviceAddExternalControllerDevice(ptr, converter, sender);
            Marshal.FreeHGlobal(ptr);
            return ret;
#endif
        }

        /// <summary>
        /// Remove an input device identified by handle from X-Device SDK.
        /// </summary>
        public static System.IntPtr RemoveInputDeviceAt(int which)
        {
            return NativeMethods.XDeviceRemoveInputDeviceAt(which, true);
        }

        // I/O

        /// <summary>
        /// Get the input state of input device identified by handle.
        /// </summary>
        /// <param name="which">Device handle, which is grabbed by using XDevicePlugin.GetInputDeviceHandle().
        /// </param>
        /// <param name="state">An empty controller state, which is filled out with data inside of this function.
        /// </param>
        /// <example> 
        /// This example shows how to use the <see cref="GetInputState"/> method.
        /// <code>
        /// using UnityEngine;
        /// using Ximmerse;
        /// using Ximmerse.InputSystem;
        /// 
        /// class TestClass : MonoBehaviour
        /// {
        ///     private int m_leftControllerHandle;
        ///     private int m_hawkHandle;
        ///     
        ///     private XDevicePlugin.ControllerState m_leftControllerState;
        ///     private XDevicePlugin.TrackingStateEx m_hawkState;
        ///     
        ///     void Awake() 
        ///     {
        ///         XDevicePlugin.Init();
        ///         m_leftControllerHandle = XDevicePlugin.GetInputDeviceHandle("XCobra-0");
        ///         m_hawkHandle = XDevicePlugin.GetInputDeviceHandle("XHawk-0");
        ///     }
        ///     void Update()
        ///     {
        ///         // if this is larger than 0, it means it is valid input device;
        ///         if (m_leftControllerHandle >= 0)
        ///         {
        ///             UpdateLeftController();
        ///         }
        ///         if(m_hawkHandle>=0)
        ///         {
        ///             UpdateHawk();
        ///         }
        ///     }
        ///     
        ///     private void UpdateLeftController()
        ///     {
        ///         // You have to update the state manually. 
        ///         XDevicePlugin.UpdateInputState(m_leftControllerHandle);
        ///         XDevicePlugin.GetInputState(m_leftControllerHandle, ref m_leftControllerState);
        ///         var trigger = m_leftControllerState.axes[(int)ControllerRawAxis.LeftTrigger];
        ///         var xAxis = m_leftControllerState.axes[(int)ControllerRawAxis.LeftThumbX];
        ///         var yAxis = m_leftControllerState.axes[(int)ControllerRawAxis.LeftThumbY];
        ///         var orientation = new Quaternion(
        ///                 -m_leftControllerState.rotation[0],
        ///                 -m_leftControllerState.rotation[1],
        ///                  m_leftControllerState.rotation[2],
        ///                  m_leftControllerState.rotation[3]
        ///             );
        ///     }
        ///
        ///     private void UpdateHawk()
        ///     {
        ///         // You have to update the state manually. 
        ///         XDevicePlugin.UpdateInputState(m_hawkHandle);
        ///         XDevicePlugin.GetInputState(m_hawkHandle, ref m_hawkState);
        ///         // 0 = left controller, 1 = right controller 
        ///         int floatOffset = m_hawkState.OffsetOf(0);
        ///         // if this is -1, the position can not be found
        ///         // return to 0 if point is valid
        ///         if (floatOffset >= 0)
        ///         {
        ///             float[] rawPostionData = m_hawkState.GetData();
        ///             // convert the position to 1:1 scale movement. 
        ///             float movementScale = 0.001f;
        ///             Vector3 position = new Vector3(
        ///                          m_hawkState.data[floatOffset++] * movementScale,
        ///                          m_hawkState.data[floatOffset++] * movementScale,
        ///                         -m_hawkState.data[floatOffset++] * movementScale
        ///                     );
        ///             // todo : this position needs to be transformed into different space depending on what user is trying to do.
        ///             // Check out TrackingInput.cs line 148-160 for reference.
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>



#if ((UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS) && !UNITY_EDITOR_WIN)
        public static int GetInputState(int which, ref ControllerState state)
        {
            System.IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ControllerState)));
            int ret = NativeMethods.XDeviceGetInputState(which, ptr);
            if (ret == 0) NativeMethods.ControllerStateFromPtr(ref state, ptr);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }

        /// <summary>
        /// Get the input state of input device identified by handle.
        /// </summary>
        public static int GetInputState(int which, ref TrackerState state)
        {
            System.IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrackerState)));
            int ret = NativeMethods.XDeviceGetInputState(which, ptr);
            if (ret == 0) NativeMethods.TrackerStateFromPtr(ref state, ptr);
            Marshal.FreeHGlobal(ptr);
            return ret;
        }
#else
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
        static AndroidJavaObject controllerState = new AndroidJavaObject("com.naocy.mirageapi.model.ControllerState");
        static AndroidJavaObject controllerNodePos = new AndroidJavaObject("com.naocy.mirageapi.model.Vector3");
        static AndroidJavaObject hmdNodePos = new AndroidJavaObject("com.naocy.mirageapi.model.Vector3");
        //The UpdateState Return Value，XYZ,X=getInputStateValue,Y=getHmdNodePositionValue,Z=getControllerNodePositionValue
        static int updateStateRet=0;

        /// <summary>
        /// Update The InputState,and TrackNodePos By Device Handle And Save,For Reduce MirageService Call Times
        /// </summary>
        public static void UpdateState(int which)
        {
            updateStateRet=XDevicePlugin.s_XDeviceApi.CallStatic<int>("updateState", which, controllerState, controllerNodePos, hmdNodePos);
        }
#endif

        /// <summary>
        /// Get ControllerState By which,which=0 hmd,which=1 controller
        /// </summary>
        /// <param name="which"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static int GetInputState(int which, ref ControllerState state)
        {

#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API

            UpdateState(which);
            
            state.handle = controllerState.Get<int>("handle");
            state.timestamp = (int)controllerState.Get<long>("timestamp");
            state.frameCount = controllerState.Get<int>("frameCount");
            state.state_mask = controllerState.Get<int>("state_mask");
            state.axes = controllerState.Get<float[]>("axes");
            state.buttons = (uint)controllerState.Get<int>("buttons");
            state.position = controllerState.Get<float[]>("position");
            state.rotation = controllerState.Get<float[]>("rotation");
            state.accelerometer = controllerState.Get<float[]>("accelerometer");
            state.gyroscope = controllerState.Get<float[]>("gyroscope");
            //Debug.Log("GetInputState ControllerState end timestamp: " + androidController1.Get<long>("timestamp")+
            //    " position "+state.position[0]+" rotation " + state.rotation[0] + " axes " + state.axes[0] + " frameCount " + state.frameCount+
            //    " accelerometer " + state.accelerometer);
            return updateStateRet / 100;
           
#else           
            return NativeMethods.XDeviceGetInputState(which, ref state);
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
		static AndroidJavaObject androidTrackerState = new AndroidJavaObject("com.naocy.mirageapi.model.TrackerState");
#endif
        /// <summary>
        /// Get the input state of input device identified by handle.
        /// </summary>
        public static int GetInputState(int which, ref TrackerState state)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
        int ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("getInputState",which,androidTrackerState);
        state.handle=androidTrackerState.Get<int>("handle");
        state.timestamp=androidTrackerState.Get<int>("timestamp");
        state.frameCount=androidTrackerState.Get<int>("frameCount");
        state.capacity=androidTrackerState.Get<int>("capacity");
        state.count=androidTrackerState.Get<int>("count");
        
        int[] id=androidTrackerState.Get<int[]>("id");
        Marshal.FreeHGlobal(state.id);
        state.id=Marshal.AllocHGlobal(id.Length);
        Marshal.Copy(id, 0, state.id, id.Length);

        float[] data=androidTrackerState.Get<float[]>("data");
        Marshal.FreeHGlobal(state.data);
        state.data=Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, state.data, data.Length);
        return ret;
#else
            return NativeMethods.XDeviceGetInputState(which, ref state);
#endif
        }
#endif

        /// <summary>
        /// Send a message to input device identified by handle.
        /// </summary>
        public static int SendMessage(int which, int Msg, int wParam, int lParam)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<int>("sendMessage", which, Msg, wParam, lParam);
#else
            return NativeMethods.XDeviceSendMessage(which, Msg, wParam, lParam);
#endif
        }

        // recenter controller
        public static int SendRecenterMessage(int which, float wParam, int lParam)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<int>("sendRecenterMessage", which, wParam, lParam);
#else
            return NativeMethods.XDeviceSendRecenterMessage(which, wParam, lParam);
#endif
        }

        /// <summary>
        /// Update input device identified by handle.
        /// </summary>
        public static int UpdateInputState(int which)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<int>("updateInputState", which);
#else
            return NativeMethods.XDeviceUpdateInputState(which);
#endif
        }

        public static bool GetBool(int which, int fieldID, bool defaultValue)
        {
            if (!CheckHandle(which)) { return defaultValue; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<bool>("getBool", which, fieldID, defaultValue);
#else
            return NativeMethods.XDeviceGetBool(which, fieldID, defaultValue);
#endif


        }
        public static void SetBool(int which, int fieldID, bool value)
        {
            if (!CheckHandle(which)) { return; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            XDevicePlugin.s_XDeviceApi.CallStatic("setBool", which, fieldID, value);
#else
            NativeMethods.XDeviceSetBool(which, fieldID, value);
#endif
        }

        public static int GetInt(int which, int fieldID, int defaultValue)
        {
            if (!CheckHandle(which)) { return defaultValue; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            int ret = XDevicePlugin.s_XDeviceApi.CallStatic<int>("getInt", which, fieldID, defaultValue);
            return ret;

#else
            return NativeMethods.XDeviceGetInt(which, fieldID, defaultValue);
#endif
        }
        public static void SetInt(int which, int fieldID, int value)
        {
            if (!CheckHandle(which)) { return; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            XDevicePlugin.s_XDeviceApi.CallStatic("setInt", which, fieldID, value);
#else
            NativeMethods.XDeviceSetInt(which, fieldID, value);
#endif
        }

        public static float GetFloat(int which, int fieldID, float defaultValue)
        {
            if (!CheckHandle(which)) { return defaultValue; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<float>("getFloat", which, fieldID, defaultValue);
#else
            return NativeMethods.XDeviceGetFloat(which, fieldID, defaultValue);
#endif
        }
        public static void SetFloat(int which, int fieldID, float value)
        {
            if (!CheckHandle(which)) { return; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            XDevicePlugin.s_XDeviceApi.CallStatic("setFloat", which, fieldID, value);
#else
            NativeMethods.XDeviceSetFloat(which, fieldID, value);
#endif
        }

        public static void GetObject(int which, int fieldID, float[] data, int offset)
        {
            if (!CheckHandle(which)) { return; }
            int size = (data.Length - offset) * 4;
            System.IntPtr ptr = Marshal.AllocHGlobal(size);
            NativeMethods.XDeviceGetObject(which, fieldID, ptr, 0, size);
            Marshal.Copy(ptr, data, offset, data.Length - offset);
            Marshal.FreeHGlobal(ptr);
        }

        public static string GetString(int which, int fieldID, string defaultValue)
        {
            if (!CheckHandle(which)) { return defaultValue; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            return XDevicePlugin.s_XDeviceApi.CallStatic<string>("getString", which, fieldID, defaultValue);
#else
            System.IntPtr defaultValuePtr = Marshal.StringToHGlobalAnsi(defaultValue);
            string ret = Marshal.PtrToStringAnsi(NativeMethods.XDeviceGetString(which, fieldID, defaultValuePtr));
            Marshal.FreeHGlobal(defaultValuePtr);
            return ret;
#endif
        }

        public static void SetString(int which, int fieldID, string value)
        {
            if (!CheckHandle(which)) { return; }
#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
            XDevicePlugin.s_XDeviceApi.CallStatic<string>("setString", which, fieldID, value);
#else
            System.IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);
            NativeMethods.XDeviceSetString(which, fieldID, valuePtr);
            Marshal.FreeHGlobal(valuePtr);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API
        static AndroidJavaObject androidVector3 = new AndroidJavaObject("com.naocy.mirageapi.model.Vector3");
#endif
        public static TrackingResult GetNodePosition(int which, int history, int node, float[] position)
        {

#if UNITY_ANDROID && !UNITY_EDITOR && USE_MIRAGE_API

            if (which == 0)
            {
                if (node == 3)
                {
                    position[0] = controllerNodePos.Get<float>("x");
                    position[1] = controllerNodePos.Get<float>("y");
                    position[2] = controllerNodePos.Get<float>("z");
                    return (TrackingResult)(updateStateRet / 10 % 10);

                }
                else if (node == 5)
                {
                    position[0] = hmdNodePos.Get<float>("x");
                    position[1] = hmdNodePos.Get<float>("y");
                    position[2] = hmdNodePos.Get<float>("z");
                    return (TrackingResult)(updateStateRet % 10);
                }
                else
                {
                    return (TrackingResult)NativeMethods.XDeviceGetNodePosition(which, history, node, position);
                }
            }
            return TrackingResult.NotTracked;
#else
            return (TrackingResult)NativeMethods.XDeviceGetNodePosition(which, history, node, position);
#endif
        }

        protected static float[] GetNodePosition_floats = new float[3];

        /// <summary>
        /// Get the node position of input device identified by handle.
        /// </summary>
        public static TrackingResult GetNodePosition(int which, int history, int node, ref UnityEngine.Vector3 position)
        {
            //Debug.Log("GetNodePosition start " + which + " " + node);
            lock (GetNodePosition_floats)
            {
                int ret = (int)GetNodePosition(which, history, node, GetNodePosition_floats);
                //Debug.Log("GetNodePosition end" + which + " " + node + " ret: " + ret + " " + GetNodePosition_floats[0] + " " + GetNodePosition_floats[1] + " " + GetNodePosition_floats[2]);
                if (ret > 0)
                {
                    position.Set(
                         GetNodePosition_floats[0],
                         GetNodePosition_floats[1],
                        -GetNodePosition_floats[2]
                    );
                }
                else
                {
                    position.Set(0, 0, 0);
                    return TrackingResult.NotTracked;
                }
                return (TrackingResult)ret;
            }
        }
        protected static float[] UpdateNodeRotation_floats = new float[4];

        /// <summary>
        /// Update the node rotation of input device identified by handle.
        /// </summary>
        public static int UpdateNodeRotation(int which, int node, UnityEngine.Quaternion rotation)
        {
            lock (UpdateNodeRotation_floats)
            {
                int i = 0;
                UpdateNodeRotation_floats[i] = -rotation[i]; ++i;
                UpdateNodeRotation_floats[i] = -rotation[i]; ++i;
                UpdateNodeRotation_floats[i] = rotation[i]; ++i;
                UpdateNodeRotation_floats[i] = rotation[i]; ++i;
                //
                int ret = NativeMethods.XDeviceUpdateNodePose(which, node, null, UpdateNodeRotation_floats);
                return ret;
            }
        }

        public static int GetTrackerPose(int which, out float height, out float depth, out float pitch)
        {
            int ret = NativeMethods.XDeviceGetTrackerPose(which, out height, out depth, out pitch);
            depth *= -1.0f;
            pitch *= -1.0f;
            return ret;
        }

        public static int SetTrackerPose(int which, float height, float depth, float pitch)
        {
            return NativeMethods.XDeviceSetTrackerPose(which, height, -depth, -pitch);
        }

        public static int GetTickCount()
        {
            return NativeMethods.XDeviceGetTickCount();
        }

        protected static float[] UpdateRot = new float[4];
        public static void setHMDRotation(Quaternion rot)
        {
            UpdateRot[0] = -rot.x;
            UpdateRot[1] = -rot.y;
            UpdateRot[2] = rot.z;
            UpdateRot[3] = rot.w;

            Quaternion qua = new Quaternion(UpdateRot[0], UpdateRot[1], UpdateRot[2], UpdateRot[3]);
            Vector3 euler = qua.eulerAngles;

            NativeMethods.SetHeadRotation(UpdateRot, euler.x, euler.y, euler.z);
        }

        public static TrackingResult GetSmoothPosition(int node, float[] position)
        {
            return (TrackingResult)NativeMethods.XHawkGetSmoothPosition(node, position);
        }

        protected static float[] GetSmoothPosition_floats = new float[3];

        /// <summary>
        /// Get the node position of input device identified by handle.
        /// </summary>
        public static TrackingResult GetSmoothPosition(int node, ref UnityEngine.Vector3 position)
        {
            lock (GetSmoothPosition_floats)
            {
                int ret = NativeMethods.XHawkGetSmoothPosition(node, GetSmoothPosition_floats);
                if (ret > 0)
                {
                    position.Set(
                         GetSmoothPosition_floats[0],
                         GetSmoothPosition_floats[1],
                        -GetSmoothPosition_floats[2]
                    );
                }
                else
                {
                    position.Set(0, 0, 0);
                    return TrackingResult.NotTracked;
                }
                return (TrackingResult)ret;
            }
        }

        public static bool CheckHandle(int which)
        {
            if (which >= 0)
            {
                return true;
            }
            else
            {
                //Log.e("XDevicePlugin","which<0");
                return false;
            }
        }

#endregion Methods      

    }
}