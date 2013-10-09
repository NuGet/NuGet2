using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace NuGet.WebMatrix
{
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    internal class SystemInformation
    {
        private static SystemInformation _current = null;
        private Version _osVersion;
        private Version _spVersion;
        private string _productType;
        private string _architecture;

        /// <summary>
        /// Construct SystemInformation from current system properties
        /// </summary>
        private SystemInformation()
        {
            OSVERSIONINFOEX osVersionInfo = GetOSVersionInfo();
            SetOSVersion(osVersionInfo);
            SetSPVersion(osVersionInfo);
            SetProductType(osVersionInfo);

            SYSTEM_INFO osSystemInfo = GetOSSystemInfo();
            SetArchitecture(osSystemInfo);
        }

        /// <summary>
        /// Construct SystemInformation manually (for testing)
        /// </summary>
        /// <param name="osVersion"></param>
        /// <param name="spVersion"></param>
        /// <param name="productType"></param>
        /// <param name="architecture"></param>
        internal SystemInformation(Version osVersion, Version spVersion, string productType, string architecture)
        {
            _osVersion = osVersion;
            _spVersion = spVersion;
            _productType = productType;
            _architecture = architecture;
        }

        /// <summary>
        /// Operating System Version
        /// </summary>
        public Version OSVersion
        {
            get
            {
                return _osVersion;
            }
        }

        /// <summary>
        /// Service Pack Version
        /// </summary>
        public Version SPVersion
        {
            get
            {
                return _spVersion;
            }
        }

        /// <summary>
        /// Windows Product Type: "Client", "Server", or null
        /// </summary>
        public string ProductType
        {
            get
            {
                return _productType;
            }
        }

        /// <summary>
        /// Processor architecture: "x86", "x64", or null
        /// </summary>
        public string Architecture
        {
            get
            {
                return _architecture;
            }
        }

        /// <summary>
        /// The current system information
        /// </summary>
        public static SystemInformation Current
        {
            get 
            {
                if (_current == null)
                {
                    _current = new SystemInformation();
                }

                return _current;
            }
        }

        /// <summary>
        /// Helper to set OSVersion from winapi result
        /// </summary>
        /// <param name="osVersionInfo"></param>
        private void SetOSVersion(OSVERSIONINFOEX osVersionInfo)
        {
            _osVersion = new Version(osVersionInfo.MajorVersion, osVersionInfo.MinorVersion, osVersionInfo.BuildNumber, 0);
        }

        /// <summary>
        /// Helper to set SPVersion from winapi result
        /// </summary>
        /// <param name="osVersionInfo"></param>
        private void SetSPVersion(OSVERSIONINFOEX osVersionInfo)
        {
            _spVersion = new Version(osVersionInfo.ServicePackMajor, osVersionInfo.ServicePackMinor, 0, 0);
        }

        /// <summary>
        /// Helper to set ProductType from winapi result
        /// </summary>
        /// <param name="osVersionInfo"></param>
        private void SetProductType(OSVERSIONINFOEX osVersionInfo)
        {
            switch (osVersionInfo.ProductType)
            {
                case VER_NT_WORKSTATION:
                    _productType = "Client";
                    break;

                case VER_NT_SERVER:
                case VER_NT_DOMAIN_CONTROLLER:
                    _productType = "Server";
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Helper to set Architecture from winapi result
        /// </summary>
        /// <param name="osVersionInfo"></param>
        private void SetArchitecture(SYSTEM_INFO osSystemInfo)
        {
            switch (osSystemInfo.uProcessorInfo.wProcessorArchitecture)
            {
                case PROCESSOR_ARCHITECTURE_INTEL:
                    _architecture = "x86";
                    break;

                case PROCESSOR_ARCHITECTURE_AMD64:
                    _architecture = "x64";
                    break;

                case PROCESSOR_ARCHITECTURE_IA64:
                case PROCESSOR_ARCHITECTURE_UNKNOWN:
                default:
                    break;
            }
        }

        /// <summary>
        /// Call winapi to get OSVERSIONINFOEX
        /// </summary>
        /// <returns></returns>
        private OSVERSIONINFOEX GetOSVersionInfo()
        {
            OSVERSIONINFOEX versionInfo = new OSVERSIONINFOEX();
            versionInfo.Size = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

            if (!GetVersionEx(ref versionInfo))
            {
                throw new Win32Exception();
            }

            return versionInfo;
        }

        /// <summary>
        /// Call winapi to get SYSTEM_INFO
        /// </summary>
        /// <returns></returns>
        private SYSTEM_INFO GetOSSystemInfo()
        {
            SYSTEM_INFO systemInfo = new SYSTEM_INFO();
            
            bool isWow64 = false;
            IsWow64Process(GetCurrentProcess(), ref isWow64);

            if (isWow64)
            {
                GetNativeSystemInfo(ref systemInfo);
            }
            else
            {
                GetSystemInfo(ref systemInfo);
            }

            return systemInfo;
        }

        /// <summary>
        /// Struct for winapi version
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int Size;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string ServicePack;
            public short ServicePackMajor;
            public short ServicePackMinor;
            public short SuiteMask;
            public byte ProductType;
            public byte Reserved;
        }

        /// <summary>
        /// Constants for determining winapi product type
        /// </summary>
        private const byte VER_NT_WORKSTATION = 1;
        private const byte VER_NT_DOMAIN_CONTROLLER = 2;
        private const byte VER_NT_SERVER = 3;

        /// <summary>
        /// Declaration of winapi to get version info
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "self contained.")]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetVersionEx([MarshalAs(UnmanagedType.Struct)] ref OSVERSIONINFOEX versionInfo);

        /// <summary>
        /// Struct for winapi processor information
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            internal _PROCESSOR_INFO_UNION uProcessorInfo;
            public uint dwPageSize;
            public uint lpMinimumApplicationAddress;
            public uint lpMaximumApplicationAddress;
            public uint dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public uint dwProcessorLevel;
            public uint dwProcessorRevision;
        }

        /// <summary>
        /// Struct for winapi detailed processor information
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct _PROCESSOR_INFO_UNION
        {
            [FieldOffset(0)]
            internal uint dwOemId;
            [FieldOffset(0)]
            internal ushort wProcessorArchitecture;
            [FieldOffset(2)]
            internal ushort wReserved;
        }

        /// <summary>
        /// Constants for determining winapi processor architectue
        /// </summary>
        private const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        private const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const ushort PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF;

        /// <summary>
        /// Declaration of winapi to determine if running in Wow64
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="isWow64"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "self contained.")]
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isWow64);

        /// <summary>
        /// Get the current process handle api declaration
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "self contained.")]
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Declaration of winapi to get the current processor information
        /// </summary>
        /// <param name="lpSystemInfo"></param>
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "self contained.")]
        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);

        /// <summary>
        /// Declaration of winapi to get the current processor information under Wow64
        /// </summary>
        /// <param name="lpSystemInfo"></param>
        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "self contained.")]
        [DllImport("kernel32.dll")]
        private static extern void GetNativeSystemInfo([MarshalAs(UnmanagedType.Struct)] ref SYSTEM_INFO lpSystemInfo);
    }
}
