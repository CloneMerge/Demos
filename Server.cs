// This code is for demonstration purposes only!
// Written by: Jason Drawdy

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace OPCDAServer
{
    public class Server : ClassicBaseNodeManager
    {
        const int PropertyIdCasingMaterial          = 5650;
        const int PropertyIdCasingHeight            = 5651;
        const int PropertyIdCasingManufacturer      = 5652;

        private static int itemHandle_ = 1;

        // Simulated Data Items
        private static MyItem myDynamicRampItem_;
        private static MyItem myDynamicSineItem_;
        private static MyItem myDynamicRandomItem_;

        static private Thread myThread_;
        static private ManualResetEvent stopThread_;

        static private Dictionary<IntPtr, MyItem> items_ = new Dictionary<IntPtr, MyItem>();

        public override int OnGetLogLevel()
        {
            return (int)LogLevel.Info;
        }

        public override string OnGetLogPath()
        {
            return "";
        }

        public override int OnCreateServerItems()
        {
            CreateServerAddressSpace();

            // create a thread for simulating signal changes
            // in real application this thread reads from the device
            myThread_ = new Thread(RefreshThread) { Name = "Device Simulation", Priority = ThreadPriority.AboveNormal };
            myThread_.Start();

            return StatusCodes.Good;
        }

        public override void OnShutdownSignal()
        {
            stopThread_ = new ManualResetEvent(false);
            stopThread_.WaitOne(5000, true);
            stopThread_.Close();
            stopThread_ = null;
        }

        public override int OnGetDaServerParameters(out int updatePeriod, out char branchDelimiter, out DaBrowseMode browseMode)
        {
            // Default Values
            updatePeriod = 100; // This can be whatever it wants.
            branchDelimiter = '.';
            browseMode = DaBrowseMode.Generic;
            return StatusCodes.Good;
        }

        public override ClassicServerDefinition OnGetDaServerDefinition()
        {
            DaServer = new ClassicServerDefinition
            {
                ClsIdApp = "{B5876868-F60B-4CC1-8FA5-36ED38B51FEB}",
                CompanyName = "Your-Company-Name-Here",
                ClsIdServer = "{212F55FA-7617-487E-B929-BC75279AC2AA}",
                PrgIdServer = "OpcNetDaAe.DaSimpleSample",
                PrgIdCurrServer = "OpcNetDaAe.DaSimpleSample.90",
                ServerName = "OPC DA Server",
                CurrServerName = "OPC Server v1.0.0"
            };

            return DaServer;
        }

        public override int OnQueryProperties(
            IntPtr deviceItemHandle,
            out int noProp,
            out int[] iDs)
        {
            MyItem item;
            if (items_.TryGetValue(deviceItemHandle, out item))
            {
                if (item.ItemProperties != null)
                {
                    // item has  custom properties
                    noProp = item.ItemProperties.Length;
                    iDs = new int[noProp];
                    for (int i = 0; i < noProp; ++i)
                    {
                        iDs[i] = item.ItemProperties[i].PropertyId;
                    }
                    return StatusCodes.Good;
                }
            }
            noProp = 0;
            iDs = null;
            return StatusCodes.Bad;
        }

        public override int OnGetPropertyValue(IntPtr deviceItemHandle, int propertyId, out object propertyValue)
        {
            MyItem item;
            if (items_.TryGetValue(deviceItemHandle, out item))
            {
                if (item.ItemProperties != null)
                {

                    int numProp = item.ItemProperties.Length;
                    for (int i = 0; i < numProp; ++i)
                    {
                        if (item.ItemProperties[i].PropertyId == propertyId)
                        {
                            propertyValue = item.ItemProperties[i].PropertyValue;
                            return StatusCodes.Good;
                        }
                    }
                }
            }
            // Item property is not available
            propertyValue = null;
            return StatusCodes.BadInvalidPropertyId;
        }

        public override int OnWriteItems(DaDeviceItemValue[] values, out int[] errors)
        {
            errors = new int[values.Length];                            // result array
            for (int i = 0; i < values.Length; ++i)                     // init to Good
                errors[i] = StatusCodes.Good;

            // TO-DO: write the new values to the device
            foreach (DaDeviceItemValue t in values)
            {
                MyItem item;
                if (items_.TryGetValue(t.DeviceItemHandle, out item))
                {
                    // Only if there is a Value specified write the value into buffer
                    if (t.Value != null)
                        item.Value = t.Value;
                    if (t.QualitySpecified)
                        item.Quality = new DaQuality(t.Quality);
                    if (t.TimestampSpecified)
                        item.Timestamp = t.Timestamp;
                }
            }
            return StatusCodes.Good;
        }

        internal void CreateSampleVariant(
            Type itemType,
            bool isArray,
            out object itemValue)
        {
            var rand = new Random();
            if (isArray)
            {
                string bstr = null;
                if (itemType == typeof(string[]))
                {
                    bstr = "This is string #";
                }

                var itemList = new ArrayList();

                for (int i = 0; i < 4; i++)
                {
                    if (itemType == typeof(bool))
                    {
                        itemList.Add(i % 2 == 1);
                    }
                    else if (itemType == typeof(sbyte))
                    {
                        var d = new byte[1];
                        rand.NextBytes(d);
                        itemList.Add((sbyte)d[0]);
                    }
                    else if (itemType == typeof(short))
                    {
                        var d = new byte[1];
                        rand.NextBytes(d);
                        itemList.Add((short)d[0]);
                    }
                    else if (itemType == typeof(int))
                    {
                        itemList.Add(rand.Next() * 100);
                    }
                    else if (itemType == typeof(Int64))
                    {
                        itemList.Add((Int64)rand.Next() * 100);
                    }
                    else if (itemType == typeof(UInt64))
                    {
                        itemList.Add((UInt64)rand.Next() * 100);
                    }
                    else if (itemType == typeof(byte))
                    {
                        var d = new byte[1];
                        rand.NextBytes(d);
                        itemList.Add(d[0]);
                    }
                    else if (itemType == typeof(ushort))
                    {
                        var d = new byte[1];
                        rand.NextBytes(d);
                        itemList.Add((ushort)d[0]);
                    }
                    else if (itemType == typeof(uint))
                    {
                        itemList.Add((uint)rand.NextDouble() * 100);
                    }
                    else if (itemType == typeof(float))
                    {
                        itemList.Add((float)rand.NextDouble() * 100);
                    }
                    else if (itemType == typeof(double))
                    {
                        itemList.Add(rand.NextDouble() * 100);
                    }
                    else if (itemType == typeof(DateTime))
                    {
                        itemList.Add(new DateTime(rand.Next()));
                    }
                    else if (itemType == typeof(string))
                    {
                        itemList.Add(bstr + i);
                    }
                    else
                        itemList.Add(null);    // not supported type
                }
                itemValue = itemList.ToArray(itemType);
            }
            else
            {
                if (itemType == typeof(sbyte)) itemValue = (sbyte)76;
                else if (itemType == typeof(byte)) itemValue = (byte)23;
                else if (itemType == typeof(short)) itemValue = (short)345;
                else if (itemType == typeof(ushort)) itemValue = (ushort)39874;
                else if (itemType == typeof(int)) itemValue = 20196;
                else if (itemType == typeof(Int64)) itemValue = Int64.MinValue;
                else if (itemType == typeof(UInt64)) itemValue = UInt64.MaxValue;
                else if (itemType == typeof(uint)) itemValue = (uint)4230498;
                else if (itemType == typeof(float)) itemValue = (float)8.123242;
                else if (itemType == typeof(double)) itemValue = 83289.48243;
                else if (itemType == typeof(DateTime)) itemValue = new DateTime(1900, 1, 1, 12, 0, 0);
                else if (itemType == typeof(bool)) itemValue = false;
                else if (itemType == typeof(string)) itemValue = "-- It's a nice day --";
                else itemValue = null;
            }
        }

        internal class StructItemIDs
        {
            public StructItemIDs(string itemId, Type type)
            { ItemId = itemId; ItemType = type; }

            public string ItemId { get; set; }

            public Type ItemType { get; set; }
        }

        internal class StructIoTypes
        {
            public StructIoTypes(string branch, DaAccessRights accessRights)
            { Branch = branch; AccessRights = accessRights; }

            public string Branch { get; set; }

            public DaAccessRights AccessRights { get; set; }
        }

        /// <summary>
        /// Create all items supported by this server
        /// </summary>
        /// <returns></returns>
        private void CreateServerAddressSpace()
        {
            var arrayItems =
                new[]{
									   new StructItemIDs( "Short",        typeof(short)  ),
									   new StructItemIDs( "Integer",      typeof(int)    ),
									   new StructItemIDs( "Int64",        typeof(Int64)  ),
									   new StructItemIDs( "UInt64",       typeof(UInt64)  ),
									   new StructItemIDs( "SingleFloat",  typeof(float)  ),
									   new StructItemIDs( "DoubleFloat",  typeof(double) ),
									   new StructItemIDs( "String",       typeof(string) ),
									   new StructItemIDs( "Byte",         typeof(byte)   ),
									   new StructItemIDs( "Character",    typeof(sbyte)  ),
									   new StructItemIDs( "Word",         typeof(ushort) ),
									   new StructItemIDs( "DoubleWord",   typeof(uint)   ),
									   new StructItemIDs( "Boolean",      typeof(bool)   ),
									   new StructItemIDs( "DateTime",     typeof(DateTime)   ),
									   new StructItemIDs( null,           null        ) };

            var ioTypes =
                new[] {
										new StructIoTypes( "In", DaAccessRights.Readable      ),
										new StructIoTypes( "Out",   DaAccessRights.Writable      ),
										new StructIoTypes( "InOut", DaAccessRights.ReadWritable  ),
										new StructIoTypes( null, 0                    ) };
            int z;
            MyItem myItem;

            // SimpleTypes In/Out/InOut
            int i = 0;
                while (ioTypes[i].Branch != null)
                {
                    z = 0;
                    while (arrayItems[z].ItemId != null)
                    {
                        object initialItemValue;
                        CreateSampleVariant(arrayItems[z].ItemType, false, out initialItemValue);

                    var itemId = "CTT.SimpleTypes.";
                    itemId += ioTypes[i].Branch;


                    myItem = new MyItem(itemId + "." + arrayItems[z].ItemId, initialItemValue);

                    AddItem(itemId + "." + arrayItems[z].ItemId,
                            ioTypes[i].AccessRights, initialItemValue, out myItem.DeviceItemHandle);
                    items_.Add(myItem.DeviceItemHandle, myItem);
                        z++;
                    }
                    i++;
                }

                // Arrays In/Out/InOut
                i = 0;
                while (ioTypes[i].Branch != null)
                {
                    z = 0;
                    while (arrayItems[z].ItemId != null)
                    {
                        object initialItemValue;
                        CreateSampleVariant(arrayItems[z].ItemType, true, out initialItemValue);

                    var itemId = "CTT.ArrayTypes.";
                    itemId += ioTypes[i].Branch;

                    myItem = new MyItem(itemId + "." + arrayItems[z].ItemId + "[]", initialItemValue);

                    AddItem(itemId + "." + arrayItems[z].ItemId + "[]",
                            ioTypes[i].AccessRights, initialItemValue, out myItem.DeviceItemHandle);
                    items_.Add(myItem.DeviceItemHandle, myItem);
                        z++;
                    }
                    i++;
                }


                // SimulatedData/Ramp
                {
                const int itemValue = 0; // canonical data type

                myItem = new MyItem("SimulatedData.Ramp", itemValue);

                myDynamicRampItem_ = myItem;

                AddItem("SimulatedData.Ramp",
                        DaAccessRights.Readable, itemValue, out myItem.DeviceItemHandle);
                items_.Add(myItem.DeviceItemHandle, myItem);
                }

                // SimulatedData/Sine
                {
                const double itemValue = 0.0; // canonical data type

                myItem = new MyItem("SimulatedData.Sine", itemValue);

                myDynamicSineItem_ = myItem;

                AddItem("SimulatedData.Sine",
                        DaAccessRights.Readable, itemValue, out myItem.DeviceItemHandle);
                items_.Add(myItem.DeviceItemHandle, myItem);
                }

                // SimulatedData/Random
                {
                const int itemValue = 0; // canonical data type

                myItem = new MyItem("SimulatedData.Random", itemValue);

                myDynamicRandomItem_ = myItem;

                AddItem(myItem.ItemName, DaAccessRights.Readable, itemValue, out myItem.DeviceItemHandle);
                items_.Add(myItem.DeviceItemHandle, myItem);
                }

                // SpecialItems/WithAnalogEUInfo
            var itemPropertiesAnalog = new MyItemProperty[2];
            itemPropertiesAnalog[0] = new MyItemProperty(DaProperty.LowEu.Code, 40.86);
            itemPropertiesAnalog[1] = new MyItemProperty(DaProperty.HighEu.Code, 92.67);

            myItem = new MyItem("SpecialItems.WithAnalogEUInfo", 20.56, itemPropertiesAnalog);

            AddAnalogItem(myItem.ItemName,
                          DaAccessRights.ReadWritable, myItem.Value, 40.86, 92.67, out myItem.DeviceItemHandle);
            items_.Add(myItem.DeviceItemHandle, myItem);

                // SpecialItems/WithAnalogEUInfo
            itemPropertiesAnalog[0] = new MyItemProperty(DaProperty.LowEu.Code, 12.50);
            itemPropertiesAnalog[1] = new MyItemProperty(DaProperty.HighEu.Code, 27.90);

            myItem = new MyItem("SpecialItems.WithAnalogEUInfo2", 21.00, itemPropertiesAnalog);

            AddAnalogItem(myItem.ItemName,
                          DaAccessRights.ReadWritable, myItem.Value, 12.50, 27.90, out myItem.DeviceItemHandle);
            items_.Add(myItem.DeviceItemHandle, myItem);

                // Add Custom Property Definitions to the generic server
            AddProperty(PropertyIdCasingHeight, "Casing Height", 25.34);
            AddProperty(PropertyIdCasingMaterial, "Casing Material", "Aluminum");
            AddProperty(PropertyIdCasingManufacturer, "Casing Manufacturer", "CBM");
            AddProperty(102, "High EU", 45.86);
            AddProperty(103, "Low EU", 35.86);

                // Create custom item properties for the item
            var itemProperties = new MyItemProperty[3];
            itemProperties[0] = new MyItemProperty(PropertyIdCasingHeight, 25.45);
            itemProperties[1] = new MyItemProperty(PropertyIdCasingMaterial, "Aluminum");
            itemProperties[2] = new MyItemProperty(PropertyIdCasingManufacturer, "CBM");

            myItem = new MyItem("SpecialItems.WithVendorSpecificProperties", 1111, itemProperties);

            AddItem(myItem.ItemName,
                    DaAccessRights.ReadWritable, myItem.Value, out myItem.DeviceItemHandle);
            items_.Add(myItem.DeviceItemHandle, myItem);
        }

        // This method simulates item value changes.
        void RefreshThread()
        {
            double count = 0;
            int ramp = 0;
            var rand = new Random();

            // Update all used items once
            foreach (MyItem item in items_.Values)
            {
                SetItemValue(item.DeviceItemHandle, item.Value, DaQuality.Good.Code, DateTime.Now);
            }

            for (; ; )   // forever thread loop
            {
                count++;
                ramp++;
                myDynamicRampItem_.Value = ramp;
                // update server cache for this item
                SetItemValue(myDynamicRampItem_.DeviceItemHandle, myDynamicRampItem_.Value,
                   DaQuality.Good.Code, DateTime.Now);

                myDynamicRandomItem_.Value = rand.Next();
                SetItemValue(myDynamicRandomItem_.DeviceItemHandle, myDynamicRandomItem_.Value,
                   DaQuality.Good.Code, DateTime.Now);

                myDynamicSineItem_.Value = Math.Sin((count % 40) * 0.1570796327);
                SetItemValue(myDynamicSineItem_.DeviceItemHandle, myDynamicSineItem_.Value,
                   DaQuality.Good.Code, DateTime.Now);

                Thread.Sleep(1000);    // ms

                if (stopThread_ != null)
                {
                    stopThread_.Set();
                    return;               // terminate the thread
                }
            }
        }
    }

    /// <summary>
    /// My Item Property Implementation
    /// </summary>
    public class MyItemProperty
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyId">ID of the property</param>
        /// <param name="propertyValue">Value of the property</param>
        public MyItemProperty(int propertyId, object propertyValue)
        {
            PropertyId = propertyId;
            PropertyValue = propertyValue;
        }

        /// <summary>
        /// ID of the property
        /// </summary>
        public int PropertyId { get; private set; }

        /// <summary>
        /// Value of the property
        /// </summary>
        public object PropertyValue { get; private set; }
    }

    class MyItem
    {
        public MyItem(
                        string itemName,
                        object initValue)
        {
            ItemName = itemName;
            Value = initValue;
            Quality = DaQuality.Good;
            Timestamp = DateTime.UtcNow;
        }

        public MyItem(
                string itemName,
                object initValue,
                MyItemProperty[] itemProperties)
        {
            ItemName = itemName;
            Value = initValue;
            Quality = DaQuality.Good;
            ItemProperties = itemProperties;
            Timestamp = DateTime.UtcNow;
        }

        // Can be used to identify the item, not used in this example. You can use also other information like device
        // specific information (e.g. serial line, datablock and data number for PLC, ...
        public IntPtr DeviceItemHandle;
        public string ItemName { get; private set; }
        public object Value { get; set; }
        public DaQuality Quality { get;  set; }
        public DateTime Timestamp { get; set; }
        public MyItemProperty[] ItemProperties { get; private set; }
    }
}
