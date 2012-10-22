using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.Dialog
{
    internal static class ProjectUtilities
    {
        private static Lazy<ImageSource> _solutionImage = new Lazy<ImageSource>(GetSolutionImage);

        public static ImageSource SolutionImage 
        {
            get
            {
                return _solutionImage.Value;
            }
        }

        public static ImageSource GetSolutionImage()
        {
            IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IVsHierarchy solutionHierachy = solution as IVsHierarchy;
            if (solutionHierachy != null)
            {
                return GetImageFromHierarchy(
                    new HierarchyItemIdentity(solutionHierachy, VSConstants.VSITEMID_ROOT), 
                    (int)__VSHPROPID.VSHPROPID_IconIndex, 
                    (int)__VSHPROPID.VSHPROPID_IconHandle);
            }

            return null;
        }

        public static ImageSource GetImage(Project project, bool folderExpandedView = false)
        {
            ImageSource icon = null;
            IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IVsHierarchy hierarchy;
            if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy)))
            {
                if (folderExpandedView)
                {
                    icon = GetImageFromHierarchy(
                        new HierarchyItemIdentity(hierarchy, VSConstants.VSITEMID_ROOT),
                        (int)__VSHPROPID.VSHPROPID_OpenFolderIconIndex,
                        (int)__VSHPROPID.VSHPROPID_OpenFolderIconHandle);
                }
                
                if (icon == null)
                {
                    icon = GetImageFromHierarchy(
                        new HierarchyItemIdentity(hierarchy, VSConstants.VSITEMID_ROOT),
                        (int)__VSHPROPID.VSHPROPID_IconIndex,
                        (int)__VSHPROPID.VSHPROPID_IconHandle);
                }
            }
            return icon;
        }

        static uint UnboxAsUInt32(object var)
        {
            if (var is short)
                return (uint)(short)var;
            else if (var is int)
                return (uint)(int)var;
            else if (var is long)
                return (uint)(long)var;
            else if (var is ushort)
                return (ushort)var;
            else if (var is uint)
                return (uint)var;
            else if (var is ulong)
                return (uint)(ulong)var;
            else if (var is IntPtr)
                return (uint)((IntPtr)var).ToInt32();
            else if (var is Enum)
                return Convert.ToUInt32(var, CultureInfo.InvariantCulture);
            else
                return default(uint);
        }

        static IntPtr UnboxAsIntPtr(object potentialIntPtr)
        {
            if (potentialIntPtr is int)
            {
                return new IntPtr((int)potentialIntPtr);
            }
            else if (potentialIntPtr is long)
            {
                return new IntPtr((long)potentialIntPtr);
            }
            else if (potentialIntPtr is IntPtr)
            {
                return (IntPtr)potentialIntPtr;
            }

            return IntPtr.Zero;
        }

        private static BitmapSource GetImageFromHierarchy(HierarchyItemIdentity item, int iconIndexProperty, int iconHandleProperty)
        {
            int iconIndex;
            IntPtr iconHandle;
            IntPtr iconImageList;
            BitmapSource iconBitmapSource = null;

            IVsHierarchy iconSourceHierarchy = item.Hierarchy;
            uint iconSourceItemid = item.ItemID;
            if (item.IsNestedItem)
            {
                bool useNestedHierarchyIconList;
                if (TryGetHierarchyProperty(item.Hierarchy, item.ItemID, (int)__VSHPROPID2.VSHPROPID_UseInnerHierarchyIconList, out useNestedHierarchyIconList) && useNestedHierarchyIconList)
                {
                    iconSourceHierarchy = item.NestedHierarchy;
                    iconSourceItemid = item.NestedItemID;
                }
            }

            if (TryGetHierarchyProperty(iconSourceHierarchy, iconSourceItemid, iconIndexProperty, out iconIndex) &&
                TryGetHierarchyProperty(iconSourceHierarchy, VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_IconImgList, UnboxAsIntPtr, out iconImageList))
            {
                NativeImageList imageList = new NativeImageList(iconImageList);
                iconBitmapSource = imageList.GetImage(iconIndex);
            }
            else if (TryGetHierarchyProperty(item.Hierarchy, item.ItemID, iconHandleProperty, UnboxAsIntPtr, out iconHandle))
            {
                // Don't call DestroyIcon on iconHandle, as it's a shared resource owned by the hierarchy
                iconBitmapSource = Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                iconBitmapSource.Freeze();
            }

            if (iconBitmapSource == null)
            {
                iconBitmapSource = GetSystemIconImage(item);
            }

            return iconBitmapSource;
        }

        private static BitmapSource GetSystemIconImage(HierarchyItemIdentity item)
        {
            IVsProject project = item.NestedHierarchy as IVsProject;
            if (project != null)
            {
                string document;
                if (ErrorHandler.Succeeded(project.GetMkDocument(item.NestedItemID, out document)))
                {
                    SHFILEINFO shfi = new SHFILEINFO();
                    uint cbFileInfo = (uint)Marshal.SizeOf(shfi);
                    IntPtr systemImageList = NativeMethods.SHGetFileInfo(document, 0, ref shfi, cbFileInfo, SHGFI.SysIconIndex | SHGFI.SmallIcon);
                    if (systemImageList == IntPtr.Zero)
                    {
                        systemImageList = NativeMethods.SHGetFileInfo(document, 0, ref shfi, cbFileInfo, SHGFI.SysIconIndex | SHGFI.SmallIcon | SHGFI.UseFileAttributes);
                    }

                    if (systemImageList != IntPtr.Zero)
                    {
                        NativeImageList imageList = new NativeImageList(systemImageList);
                        return imageList.GetImage(shfi.iIcon);
                    }
                }
            }

            return null;
        }

        static bool TryGetHierarchyProperty<T>(IVsHierarchy hierarchy, uint itemid, int propid, out T value)
        {
            object obj;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, propid, out obj)) && obj is T)
            {
                value = (T)obj;
                return true;
            }

            value = default(T);
            return false;
        }

        static bool TryGetHierarchyProperty<T>(IVsHierarchy hierarchy, uint itemid, int propid, Func<object, T> converter, out T value)
        {
            object obj;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemid, propid, out obj)))
            {
                value = converter(obj);
                return true;
            }

            value = default(T);
            return false;
        }

        #region HierarchyItemIdentity
        class HierarchyItemIdentity
        {
            bool _isNestedInfoValid;
            bool _isNestedItem;
            HierarchyItemPair _hierarchyInfo;
            HierarchyItemPair _nestedInfo;

            public HierarchyItemIdentity(IVsHierarchy hierarchy, uint itemid)
            {
                // see if this hierarchy/itemid pair is equal to a node in a parent hierarchy
                IVsHierarchy parentHierarchy;
                uint parentItemid;

                if (itemid == (uint)VSConstants.VSITEMID.Root &&
                    TryGetHierarchyProperty(hierarchy, itemid, (int)__VSHPROPID.VSHPROPID_ParentHierarchy, out parentHierarchy) &&
                    TryGetHierarchyProperty(hierarchy, itemid, (int)__VSHPROPID.VSHPROPID_ParentHierarchyItemid, UnboxAsUInt32, out parentItemid))
                {
                    _isNestedInfoValid = true;
                    _isNestedItem = true;
                    _hierarchyInfo = new HierarchyItemPair(parentHierarchy, parentItemid);
                    _nestedInfo = new HierarchyItemPair(hierarchy, itemid);
                }
                else
                {
                    _hierarchyInfo = new HierarchyItemPair(hierarchy, itemid);
                }
            }

            public IVsHierarchy Hierarchy
            {
                get
                {
                    return _hierarchyInfo.Hierarchy;
                }
            }

            public uint ItemID
            {
                get
                {
                    return _hierarchyInfo.ItemID;
                }
            }

            public IVsHierarchy NestedHierarchy
            {
                get
                {
                    EnsureNestedInfo();

                    return _nestedInfo.Hierarchy;
                }
            }

            public uint NestedItemID
            {
                get
                {
                    EnsureNestedInfo();

                    return _nestedInfo.ItemID;
                }
            }

            public bool IsNestedItem
            {
                get
                {
                    EnsureNestedInfo();

                    return _isNestedItem;
                }
            }

            private void EnsureNestedInfo()
            {
                if (!_isNestedInfoValid)
                {

                    _isNestedItem = HierarchyItemPair.MaybeMapToNested(_hierarchyInfo, out _nestedInfo);
                    _isNestedInfoValid = true;
                }
            }
        }
        #endregion

        #region HierarchyItemPair
        public class HierarchyItemPair
        {
            public HierarchyItemPair(IVsHierarchy hierarchy, uint itemid)
            {
                Hierarchy = hierarchy;
                ItemID = itemid;
            }

            public IVsHierarchy Hierarchy { get; private set; }
            public uint ItemID { get; private set; }

            public static bool MaybeMapToNested(HierarchyItemPair outerInfo, out HierarchyItemPair nestedInfo)
            {
                Guid IID_IVsHierarchy = typeof(IVsHierarchy).GUID;
                IVsHierarchy nestedHierarchy;
                uint nestedItemId;
                IntPtr nestedHierarchyPtr;
                if (ErrorHandler.Succeeded(outerInfo.Hierarchy.GetNestedHierarchy(outerInfo.ItemID, ref IID_IVsHierarchy, out nestedHierarchyPtr, out nestedItemId)) &&
                    nestedHierarchyPtr != IntPtr.Zero) // For items in solution folders, it succeeds with a returned null ptr!
                {
                    try
                    {
                        nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyPtr) as IVsHierarchy;
                    }
                    finally
                    {
                        Marshal.Release(nestedHierarchyPtr);
                    }
                    nestedInfo = new HierarchyItemPair(nestedHierarchy, nestedItemId);
                    return true;
                }
                else
                {
                    nestedInfo = outerInfo;
                    return false;
                }
            }
        }
        #endregion

        #region NativeMethods
        [Flags]
        enum SHGFI : uint
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags);

            [DllImport("comctl32.dll")]
            internal static extern int ImageList_GetImageCount(IntPtr himl);

            [DllImport("comctl32.dll")]
            internal static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, uint flags);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            public const int ILD_TRANSPARENT = 0x0001;
        }
        #endregion

        #region NativeImageList
        /// <summary>
        /// Represents a weak cache of images from a native HIMAGELIST.
        /// </summary>
        class NativeImageList
        {
            public NativeImageList(IntPtr imageListHandle)
            {
                if (imageListHandle == IntPtr.Zero)
                {
                    throw new ArgumentNullException("imageListHandle");
                }

                ImageListHandle = imageListHandle;
            }

            private IntPtr ImageListHandle { get; set; }

            private int ImageListCount
            {
                get
                {
                    return NativeMethods.ImageList_GetImageCount(ImageListHandle);
                }
            }

            public BitmapSource GetImage(int index)
            {
                if (index >= ImageListCount || index < 0)
                {
                    return null;
                }

                // generate the ImageSource from the native HIMAGELIST handle
                IntPtr hIcon = NativeMethods.ImageList_GetIcon(ImageListHandle, index, NativeMethods.ILD_TRANSPARENT);
                if (hIcon == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    // Int32Rect.Empty means use the dimensions of the source image
                    BitmapSource image = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    image.Freeze();
                    return image;
                }
                finally
                {
                    NativeMethods.DestroyIcon(hIcon);
                }
            }
        }
        #endregion
    }
}
