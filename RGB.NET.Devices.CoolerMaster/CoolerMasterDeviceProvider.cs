﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using RGB.NET.Core;
using RGB.NET.Core.Exceptions;
using RGB.NET.Devices.CoolerMaster.Helper;
using RGB.NET.Devices.CoolerMaster.Native;

namespace RGB.NET.Devices.CoolerMaster
{
    /// <summary>
    /// Represents a device provider responsible for Cooler Master devices.
    /// </summary>
    public class CoolerMasterDeviceProvider : IRGBDeviceProvider
    {
        #region Properties & Fields

        /// <summary>
        /// Gets the singleton <see cref="CoolerMasterDeviceProvider"/> instance.
        /// </summary>
        public static CoolerMasterDeviceProvider Instance { get; } = new CoolerMasterDeviceProvider();

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x86 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX86NativePaths { get; } = new List<string> { "x86/CMSDK.dll" };

        /// <summary>
        /// Gets a modifiable list of paths used to find the native SDK-dlls for x64 applications.
        /// The first match will be used.
        /// </summary>
        public static List<string> PossibleX64NativePaths { get; } = new List<string> { "x64/CMSDK.dll" };

        /// <summary>
        /// Indicates if the SDK is initialized and ready to use.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the loaded architecture (x64/x86).
        /// </summary>
        public string LoadedArchitecture => _CoolerMasterSDK.LoadedArchitecture;

        /// <summary>
        /// Gets whether the application has exclusive access to the SDK or not.
        /// </summary>
        public bool HasExclusiveAccess { get; private set; }

        /// <inheritdoc />
        public IEnumerable<IRGBDevice> Devices { get; private set; }

        /// <summary>
        /// Gets or sets a function to get the culture for a specific device.
        /// </summary>
        public Func<CultureInfo> GetCulture { get; set; } = () => CultureHelper.GetCurrentCulture();

        #endregion

        #region Constructors

        private CoolerMasterDeviceProvider()
        { }

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool Initialize(bool exclusiveAccessIfPossible = false, bool throwExceptions = false)
        {
            IsInitialized = false;

            try
            {
                _CoolerMasterSDK.Reload();
                if (_CoolerMasterSDK.GetSDKVersion() <= 0) return false;

                IList<IRGBDevice> devices = new List<IRGBDevice>();

                foreach (CoolerMasterDevicesIndexes index in Enum.GetValues(typeof(CoolerMasterDevicesIndexes)))
                {
                    _CoolerMasterSDK.SetControlDevice(index);
                    if (_CoolerMasterSDK.IsDevicePlugged())
                    {
                        try
                        {
                            CoolerMasterRGBDevice device = null;
                            switch (index.GetDeviceType())
                            {
                                case RGBDeviceType.Keyboard:
                                    CoolerMasterPhysicalKeyboardLayout physicalLayout = _CoolerMasterSDK.GetDeviceLayout();
                                    device = new CoolerMasterKeyboardRGBDevice(new CoolerMasterKeyboardRGBDeviceInfo(index, physicalLayout, GetCulture()));
                                    break;
                                default:
                                    if (throwExceptions)
                                        throw new RGBDeviceException("Unknown Device-Type");
                                    else
                                        continue;
                            }

                            _CoolerMasterSDK.EnableLedControl(true);

                            device.Initialize();
                            devices.Add(device);
                        }
                        catch
                        {
                            if (throwExceptions)
                                throw;
                            else
                                continue;
                        }
                    }
                }

                Devices = new ReadOnlyCollection<IRGBDevice>(devices);
            }
            catch
            {
                if (throwExceptions)
                    throw;
                else
                    return false;
            }

            IsInitialized = true;

            return true;
        }

        /// <inheritdoc />
        public void ResetDevices()
        {
            if (IsInitialized)
                try
                {
                    foreach (IRGBDevice device in Devices)
                    {
                        CoolerMasterRGBDeviceInfo deviceInfo = (CoolerMasterRGBDeviceInfo)device.DeviceInfo;
                        _CoolerMasterSDK.SetControlDevice(deviceInfo.DeviceIndex);
                        _CoolerMasterSDK.EnableLedControl(false);
                        _CoolerMasterSDK.EnableLedControl(true);
                    }
                }
                catch
                {
                    // shit happens ...
                }
        }

        #endregion
    }
}
