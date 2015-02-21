using System;
using System.Runtime.InteropServices;

namespace VsExt.AutoShelve {
    /// <summary>
    /// This is the interface that will be implemented by the global service exposed
    /// by the package defined in vsExtAutoShelvePackage. It is defined as COM 
    /// visible so that it will be possible to query for it from the native version 
    /// of IServiceProvider.
    /// </summary>
    [Guid("6581CC5B-7771-4ACE-8B47-FAE72B687341")]
    [ComVisible(true)]
    interface IAutoShelveService {
        void CreateShelveset();
        void Dispose();
        ushort MaximumShelvesets { get; set; }
        event EventHandler<VsExt.AutoShelve.EventArgs.ShelvesetCreatedEventArgs> OnShelvesetCreated;
        event EventHandler OnTfsConnectionError;
        event EventHandler OnTimerStart;
        event EventHandler OnTimerStop;
        bool IsRunning { get; }
        string OutputPane { get; set; }
        void SaveShelveset();
        string ShelvesetName { get; set; }
        void StartTimer();
        void StopTimer();
        bool SuppressDialogs { get; set; }
        int TimerInterval { get; set; }
    }

    /// <summary>
    /// The goal of this interface is actually just to define a Type (or Guid from the native
    /// client's point of view) that will be used to identify the service.
    /// In theory, we could use the interface defined above, but it is a good practice to always
    /// define a new type as the service's identifier because a service can expose different interfaces.
    /// </summary>
    [Guid("ABEC5E88-9257-46C8-852F-57F42F5F4023")]
    interface SAutoShelveService {
    }
}
