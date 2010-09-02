using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio;

namespace NuPackConsole
{
    static class ExtensionMethods
    {
        public static SnapshotPoint GetStart(this ITextSnapshot snapshot)
        {
            return new SnapshotPoint(snapshot, 0);
        }

        public static SnapshotPoint GetEnd(this ITextSnapshot snapshot)
        {
            return new SnapshotPoint(snapshot, snapshot.Length);
        }

        public static NormalizedSnapshotSpanCollection TranslateTo(this NormalizedSnapshotSpanCollection coll,
            ITextSnapshot snapshot, SpanTrackingMode spanTrackingMode)
        {
            if (coll.Count > 0 && coll[0].Snapshot != snapshot)
            {
                return new NormalizedSnapshotSpanCollection(coll.Select(
                    span => span.TranslateTo(snapshot, spanTrackingMode)));
            }
            else
            {
                return coll;
            }
        }

        /// <summary>
        /// Removes a ReadOnlyRegion and clears the reference (set to null).
        /// </summary>
        public static void ClearReadOnlyRegion(this IReadOnlyRegionEdit readOnlyRegionEdit, ref IReadOnlyRegion readOnlyRegion)
        {
            if (readOnlyRegion != null)
            {
                readOnlyRegionEdit.RemoveReadOnlyRegion(readOnlyRegion);
                readOnlyRegion = null;
            }
        }

        public static void Raise<T>(this EventHandler<EventArgs<T>> ev, object sender, T arg)
        {
            if (ev != null)
            {
                ev(sender, new EventArgs<T>(arg));
            }
        }

        /// <summary>
        /// Execute a VS command on the wpfTextView CommandTarget.
        /// </summary>
        public static void Execute(this IOleCommandTarget target, Guid guidCommand, uint idCommand, object args = null)
        {
            IntPtr varIn = IntPtr.Zero;
            try
            {
                if (args != null)
                {
                    varIn = Marshal.AllocHGlobal(VariantSize);
                    Marshal.GetNativeVariantForObject(args, varIn);
                }

                int hr = target.Exec(ref guidCommand, idCommand, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, varIn, IntPtr.Zero);
                ErrorHandler.ThrowOnFailure(hr);
            }
            finally
            {
                if (varIn != IntPtr.Zero)
                {
                    VariantClear(varIn);
                    Marshal.FreeHGlobal(varIn);
                }
            }
        }

        /// <summary>
        /// Execute a default VSStd2K command.
        /// </summary>
        public static void Execute(this IOleCommandTarget target, VSConstants.VSStd2KCmdID idCommand, object args = null)
        {
            target.Execute(VSConstants.VSStd2K, (uint)idCommand, args);
        }

        // Size of VARIANTs in 32 bit systems
        const int VariantSize = 16;

        [DllImport("Oleaut32.dll", PreserveSig = false)]
        static extern void VariantClear(IntPtr var);
    }
}
