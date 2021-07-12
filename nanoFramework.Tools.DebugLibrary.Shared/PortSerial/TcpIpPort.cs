using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace nanoFramework.Tools.Debugger.PortSerial
{
    public class TcpIpPort : PortMessageBase, IPort
    {
        private const string LocalHost = "127.0.0.1";
        private readonly PortSerialManager _portManager;
        private NetworkStream _stream;
        private int _portNumber = 0;

        public NanoDevice<NanoSerialDevice> NanoDevice { get; }

        public TcpClient Device => (TcpClient)NanoDevice.DeviceBase;

        public string InstanceId
        {
            get
            {
                return NanoDevice.DeviceId;
            }
        }

        public TcpIpPort(PortSerialManager portManager, NanoDevice<NanoSerialDevice> serialDevice)
        {
            _portManager = portManager ?? throw new ArgumentNullException(nameof(portManager));
            NanoDevice = serialDevice ?? throw new ArgumentNullException(nameof(serialDevice));
        }

        public int SendBuffer(byte[] buffer)
        {
            if (Device?.Connected == true)
            {
                try
                {
                    _stream.Write(buffer, 0, buffer.Length);

                    return buffer.Length;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SendRawBufferAsync-Serial-Exception occurred: {ex.Message}\r\n {ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                throw new DeviceNotConnectedException();
            }

        }

        public byte[] ReadBuffer(int bytesToRead)
        {
            // device must be connected
            if (Device?.Connected == true)
            {
                byte[] buffer = new byte[bytesToRead];

                try
                {
                    int readBytes = _stream.Read(buffer, 0, bytesToRead);

                    if (readBytes != bytesToRead)
                    {
                        Array.Resize(ref buffer, readBytes);
                    }

                    return buffer;
                }
                catch (TimeoutException)
                {
                    // this is expected to happen when the timeout occurs, no need to do anything with it
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ReadBufferAsync-Serial-Exception occurred: {ex.Message}\r\n {ex.StackTrace}");

                    throw;
                }
            }
            else
            {
                throw new DeviceNotConnectedException();
            }

            // return empty byte array
            return new byte[0];
        }
        public int AvailableBytes => Device?.Connected == true ? Device.Available : -1;

        public ConnectPortResult ConnectDevice()
        {
            if (Device != null &&
                Device.Connected)
            {
                return ConnectPortResult.Connected;
            }

            bool successfullyOpenedDevice = false;
            bool retry = false;

            try
            {
                _portNumber = int.Parse(InstanceId.Substring(PortSerialManager.TcpIpPortType.Length));
                NanoDevice.DeviceBase = new TcpClient(LocalHost, _portNumber);

                if (Device != null)
                {
                    _stream = Device.GetStream();

                    successfullyOpenedDevice = true;
                }
                else
                {
                    successfullyOpenedDevice = false;
                }
            }
            catch(UnauthorizedAccessException)
            {
                return ConnectPortResult.Unauthorized;
            }
#if DEBUG
            catch (Exception ex)
#else
            catch
#endif
            {
                // catch all because the device open might fail for a number of reasons
                return ConnectPortResult.ExceptionOccurred;
            }

            return successfullyOpenedDevice ? ConnectPortResult.Connected : ConnectPortResult.NotConnected;
        }

        public void DisconnectDevice(bool force = false)
        {
            // disconnecting the current device

            OnLogMessageAvailable(NanoDevicesEventSource.Log.CloseDevice(InstanceId));

            // close device
            CloseDevice();

            if (force)
            {
                _portManager.DisposeDevice(InstanceId);
            }
        }

        private void CloseDevice()
        {
            _stream?.Close();
            _stream?.Dispose();
            Device?.Close();
            Device?.Dispose();
            NanoDevice.DeviceBase = null;
        }
    }
}