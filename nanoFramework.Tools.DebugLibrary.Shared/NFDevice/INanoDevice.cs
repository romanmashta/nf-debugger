//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Tools.Debugger
{
    public interface INanoDevice
    {
        public string DeviceId { get; set; }

        ConnectPortResult Connect();

        void Disconnect(bool force = false);
    }
}
