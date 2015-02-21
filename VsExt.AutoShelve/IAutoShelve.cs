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
        bool IsRunning { get; }
        ushort MaximumShelvesets { get; set; }
        string ShelvesetName { get; set; }
        double TimerInterval { get; set; }

        void CreateShelveset(bool force = false);
        void Start();
        void Stop();

        event EventHandler<VsExt.AutoShelve.EventArgs.ShelvesetCreatedEventArgs> OnShelvesetCreated;
        event EventHandler OnTfsConnectionError;
        event EventHandler OnStart;
        event EventHandler OnStop;
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
