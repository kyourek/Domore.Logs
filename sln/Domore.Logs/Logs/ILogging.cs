﻿using System;
using System.Runtime.InteropServices;

namespace Domore.Logs {
    [Guid("5D009A1B-0F1C-4C78-B741-54F10B3BAF06")]
    [ComVisible(true)]
#if NETCOREAPP
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
#else
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface ILogging {
        [DispId(1)]
        ILogConfiguration Configuration { get; set; }

        [DispId(2)]
        TimeSpan CompleteTimeout { get; set; }

        [DispId(3)]
        ILog For(string name);

        [DispId(4)]
        void Add(ILogHandler handler, object log);

        [DispId(5)]
        void Complete();

        [DispId(6)]
        void Reset();

        [ComVisible(false)]
        ILog For(Type type, string name = null);

        [ComVisible(false)]
        ILog For(object owner, string name = null);

        [ComVisible(false)]
        void Add(ILogHandler handler, params object[] logs);
    }
}
