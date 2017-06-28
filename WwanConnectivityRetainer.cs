using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using MbnApi;
using System.Runtime.InteropServices;

namespace MobileBroadbandPersistence
{
    public class WwanConnectivityRetainer : IDisposable
    {
        private HashSet<WwanConnectionRequest> PendingConnectionRequestSet = new HashSet<WwanConnectionRequest>();
        private object PendingConnectionRequestSetLock = new object();

        public bool OnWwanConnectionRequestQueue(WwanConnectionRequest req)
        {
            lock (PendingConnectionRequestSetLock)
            {
                return PendingConnectionRequestSet.Add(req);
            }
        }
        public void OnWwanConnectionRequestComplete(IMbnConnection newConnection, uint requestID)
        {
            lock (PendingConnectionRequestSetLock)
            {
                PendingConnectionRequestSet.RemoveWhere((WwanConnectionRequest item) => (item.Connection.ConnectionID == newConnection.ConnectionID && item.RequestId == requestID));
            }
        }
        public void OnWwanConnectionRequestComplete(WwanConnectionRequest req)
        {
            lock (PendingConnectionRequestSetLock)
            {
                PendingConnectionRequestSet.Remove(req);
            }
        }

        private ConnectionManagerEventsSink ConnectionManagerEventsSinkInstance;
        private uint ConnectionManagerEventsSinkCookie;
        private ConnectionEventsSink ConnectionEventsSinkInstance;
        private uint ConnectionEventsSinkCookie;
        private RegistrationEventsSink RegistrationEventsSinkInstance;
        private uint RegistrationEventsSinkCookie;

        public WwanConnectivityRetainer()
        {
            {
                var objConnectionManager = new MbnConnectionManager();
                var intfConnectionManager = (IMbnConnectionManager)objConnectionManager;
                var ppCPP_ConnectionManager = (IConnectionPointContainer)intfConnectionManager;
                {
                    Guid IID = typeof(IMbnConnectionManagerEvents).GUID;
                    ppCPP_ConnectionManager.FindConnectionPoint(ref IID, out IConnectionPoint ppCP);
                    ppCP.Advise(ConnectionManagerEventsSinkInstance = new ConnectionManagerEventsSink(this), out ConnectionManagerEventsSinkCookie);
                }
                {
                    Guid IID = typeof(IMbnConnectionEvents).GUID;
                    ppCPP_ConnectionManager.FindConnectionPoint(ref IID, out IConnectionPoint ppCP);
                    ppCP.Advise(ConnectionEventsSinkInstance = new ConnectionEventsSink(this), out ConnectionEventsSinkCookie);
                }
            }
            {
                var objInterfaceManager = new MbnInterfaceManager();
                var intfInterfaceManager = (IMbnInterfaceManager)objInterfaceManager;
                var ppCPP_InterfaceManager = (IConnectionPointContainer)intfInterfaceManager;
                {
                    Guid IID = typeof(IMbnRegistrationEvents).GUID;
                    ppCPP_InterfaceManager.FindConnectionPoint(ref IID, out IConnectionPoint ppCP);
                    ppCP.Advise(RegistrationEventsSinkInstance = new RegistrationEventsSink(this), out RegistrationEventsSinkCookie);
                }
            }
        }

        #region IDisposable Support
        private object disposeLock = new object();
        private bool disposeComplete = false;

        protected virtual void Dispose(bool disposing)
        {
            lock (disposeLock)
            {
                if (!disposeComplete)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects).
                    }
                    {
                        var objConnectionManager = new MbnConnectionManager();
                        var intfConnectionManager = (IMbnConnectionManager)objConnectionManager;
                        var ppCPP_ConnectionManager = (IConnectionPointContainer)intfConnectionManager;
                        if (ConnectionManagerEventsSinkInstance != null)
                        {
                            Guid IID = typeof(IMbnConnectionManagerEvents).GUID;
                            ppCPP_ConnectionManager.FindConnectionPoint(ref IID, out IConnectionPoint connPoint);
                            connPoint.Unadvise(ConnectionEventsSinkCookie);
                            ConnectionManagerEventsSinkInstance = null;
                        }
                        if (ConnectionEventsSinkInstance != null)
                        {
                            Guid IID = typeof(IMbnConnectionEvents).GUID;
                            ppCPP_ConnectionManager.FindConnectionPoint(ref IID, out IConnectionPoint connPoint);
                            connPoint.Unadvise(ConnectionEventsSinkCookie);
                            ConnectionEventsSinkInstance = null;
                        }
                    }
                    {
                        var objInterfaceManager = new MbnInterfaceManager();
                        var intfInterfaceManager = (IMbnInterfaceManager)objInterfaceManager;
                        var ppCPP_InterfaceManager = (IConnectionPointContainer)intfInterfaceManager;
                        if (RegistrationEventsSinkInstance != null)
                        {
                            Guid IID = typeof(IMbnRegistrationEvents).GUID;
                            ppCPP_InterfaceManager.FindConnectionPoint(ref IID, out IConnectionPoint connPoint);
                            connPoint.Unadvise(RegistrationEventsSinkCookie);
                            RegistrationEventsSinkInstance = null;
                        }
                    }

                    disposeComplete = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WwanConnectivityRetainer()
        {
            Dispose(false);
        }
        #endregion

        public void CheckStatus()
        {
            var objMbnConnectionProfileManager = new MbnConnectionProfileManager();
            var intfMbnConnectionProfileManager = (IMbnConnectionProfileManager)objMbnConnectionProfileManager;

            var mbnInfMgr = new MbnInterfaceManager();
            var infMgr = (IMbnInterfaceManager)mbnInfMgr;

            var mbnConnectionMgr = new MbnConnectionManager();
            var ImbnConnectionMgr = (IMbnConnectionManager)mbnConnectionMgr;

            IMbnConnection[] connections;
            try
            {
                connections = (IMbnConnection[])ImbnConnectionMgr.GetConnections();
            }
            catch (COMException)
            {
                connections = new IMbnConnection[] { };
            }

            foreach (var connection in connections)
            {
                Console.WriteLine("=== BEGIN CONNECTION ===");
                Console.WriteLine(string.Format("ConnectionID: {0}, InterfaceID: {1}", connection.ConnectionID, connection.InterfaceID));
                var mobileInterface = infMgr.GetInterface(connection.InterfaceID);

                var mobileHomeProvider = mobileInterface.GetHomeProvider();
                Console.WriteLine(string.Format("Home provider: {0}/{1}", mobileHomeProvider.providerID, mobileHomeProvider.providerName));
                var mobileRegistration = (IMbnRegistration)mobileInterface;
                Console.WriteLine(string.Format("Registration: {0}/{1}", mobileRegistration.GetProviderID(), mobileRegistration.GetProviderName()));

                bool allowRequestConnect = true;
                Console.WriteLine("=== BEGIN PROFILES ===");
                var mobileProfiles = intfMbnConnectionProfileManager.GetConnectionProfiles(mobileInterface);
                foreach (var objMobileProfile in mobileProfiles)
                {
                    var intfMobileProfile = (IMbnConnectionProfile)objMobileProfile;
                    Console.WriteLine("== BEGIN PROFILE ==");
                    Console.WriteLine(intfMobileProfile.GetProfileXmlData());
                    Console.WriteLine("== BEGIN PROFILE ==");
                    connection.GetConnectionState(out MBN_ACTIVATION_STATE mobileActivationState, out string mobileProfile);
                    if (allowRequestConnect && mobileInterface.GetReadyState() == MBN_READY_STATE.MBN_READY_STATE_INITIALIZED && mobileRegistration.GetRegisterState() == MBN_REGISTER_STATE.MBN_REGISTER_STATE_HOME && mobileActivationState == MBN_ACTIVATION_STATE.MBN_ACTIVATION_STATE_DEACTIVATED)
                    {
                        allowRequestConnect = false;
                        Console.WriteLine("Queuing to connect the first profile ...");
                        new WwanConnectionRequest(this, connection, intfMobileProfile.GetProfileXmlData());
                    }
                }
                Console.WriteLine("=== END PROFILES ===");

                var sig = (IMbnSignal)mobileInterface;
                uint signalStrength = sig.GetSignalStrength();
                if (signalStrength != (uint)MBN_SIGNAL_CONSTANTS.MBN_RSSI_UNKNOWN)
                {
                    int signalStrengthDB = -113 + ((int)signalStrength * 2);
                    Console.WriteLine(string.Format("Signal Strength: {0} dbm", signalStrengthDB.ToString()));
                    Console.WriteLine("");
                    Console.WriteLine("Note: -113 means -113 or less");
                    Console.WriteLine("Note:  -51 means  -51 or greater");
                }
                else
                {
                    Console.WriteLine("Signal Strength unknown");
                }
                Console.WriteLine("=== END CONNECTION ===");
            }
        }
    }

    public class WwanConnectionRequest : IEquatable<WwanConnectionRequest>
    {
        public IMbnConnection Connection
        {
            get
            {
                return _Connection;
            }
        }
        public uint RequestId
        {
            get
            {
                return _requestId;
            }
        }

        private WwanConnectivityRetainer owner;
        private IMbnConnection _Connection;
        private string ProfileXml = string.Empty;
        private uint _requestId = uint.MaxValue;

        public WwanConnectionRequest(WwanConnectivityRetainer owner, IMbnConnection Connection)
        {
            Debug.Assert(owner != null);
            this.owner = owner;
            Debug.Assert(Connection != null);
            this._Connection = Connection;
        }

        public WwanConnectionRequest(WwanConnectivityRetainer owner, IMbnConnection Connection, string ProfileXml)
        {
            Debug.Assert(owner != null);
            this.owner = owner;
            Debug.Assert(Connection != null);
            this._Connection = Connection;
            Debug.Assert(!string.IsNullOrEmpty(ProfileXml));
            this.ProfileXml = ProfileXml;
            if (owner.OnWwanConnectionRequestQueue(this))
            {
                Console.WriteLine("Connecting the first profile ...");
                Task.Run(() => {
                    System.Threading.Thread.Sleep(30000);
                    try
                    {
                        _Connection.Connect(MBN_CONNECTION_MODE.MBN_CONNECTION_MODE_TMP_PROFILE, ProfileXml, out _requestId);
                    }
                    catch (COMException)
                    {
                        Console.WriteLine("Oops, something went wrong");
                        owner.OnWwanConnectionRequestComplete(this);
                    }
                    catch
                    {
                        owner.OnWwanConnectionRequestComplete(this);
                        throw;
                    }
                });
            }
            // We don't care if the request was not queued. It will be disposed by GC
        }

        public override int GetHashCode()
        {
            Debug.Assert(owner != null);
            Debug.Assert(_Connection != null);
            return base.GetHashCode() ^ owner.GetHashCode() ^ _Connection.ConnectionID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var _obj = obj as WwanConnectionRequest;
            if (_obj == null) return false;
            return Equals(_obj);
        }

        public bool Equals(WwanConnectionRequest other)
        {
            if (other == null) return false;
            return this.owner == other.owner && this._Connection.ConnectionID == other._Connection.ConnectionID;
        }
    }

    class ConnectionManagerEventsSink : IMbnConnectionManagerEvents
    {
        private WwanConnectivityRetainer owner;
        public ConnectionManagerEventsSink(WwanConnectivityRetainer owner)
        {
            this.owner = owner;
        }

        public void OnConnectionArrival(IMbnConnection newConnection)
        {
            Console.WriteLine(">>> Connection added to the system <<<");
            owner.CheckStatus();
        }

        public void OnConnectionRemoval(IMbnConnection oldConnection)
        {
            Console.WriteLine(">>> Connection removed from the system <<<");
            owner.CheckStatus();
        }
    }

    class ConnectionEventsSink : IMbnConnectionEvents
    {
        private WwanConnectivityRetainer owner;
        public ConnectionEventsSink(WwanConnectivityRetainer owner)
        {
            this.owner = owner;
        }

        public void OnConnectComplete(IMbnConnection newConnection, uint requestID, int status)
        {
            Console.WriteLine(">>> Connection: connect complete <<<");
            owner.OnWwanConnectionRequestComplete(newConnection, requestID);
            owner.CheckStatus();
        }

        public void OnDisconnectComplete(IMbnConnection newConnection, uint requestID, int status)
        {
            Console.WriteLine(">>> Connection: disconnect complete <<<");
            owner.CheckStatus();
        }

        public void OnConnectStateChange(IMbnConnection newConnection)
        {
            Console.WriteLine(">>> Connection status changed <<<");
            owner.CheckStatus();
        }

        public void OnVoiceCallStateChange(IMbnConnection newConnection)
        {
        }
    }

    class RegistrationEventsSink : IMbnRegistrationEvents
    {
        private WwanConnectivityRetainer owner;
        public RegistrationEventsSink(WwanConnectivityRetainer owner)
        {
            this.owner = owner;
        }

        public void OnRegisterModeAvailable(IMbnRegistration newInterface)
        {
        }

        public void OnRegisterStateChange(IMbnRegistration newInterface)
        {
            Console.WriteLine(">>> Registration status changed <<<");
            owner.CheckStatus();
        }

        public void OnPacketServiceStateChange(IMbnRegistration newInterface)
        {
            Console.WriteLine(">>> Packet Service status changed <<<");
            owner.CheckStatus();
        }

        public void OnSetRegisterModeComplete(IMbnRegistration newInterface, uint requestID, int status)
        {
        }
    }
}
