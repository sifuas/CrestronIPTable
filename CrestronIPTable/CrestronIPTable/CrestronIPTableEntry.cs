using System;
using System.Text;
using System.Collections.Generic;

// Crestron Libraries
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes

//3rd party
using Newtonsoft.Json;

namespace CrestronIPTableLibrary
{
    public class CrestronIPTableEntry
    {
        /*
         *  Configuration properties that can be set
         */

        // CIP_ID of IPTable Entry
        [JsonProperty("CIP_ID")]
        public ushort CIP_ID { get; set; }

        // IP Address of IPTable Entry
        [JsonProperty("IPAddress")]
        public String IPAddress { get; set; }

        // Port for the IPTable Entry - Must be > 256
        [JsonProperty("Port")]
        public ushort Port { get; set; }

        // Remapping ID to allow program to remap IP table entries at runtime
        [JsonProperty("DeviceID")]
        public ushort DeviceID { get; set; }
       
        // the ID of the program this IP Table entry is for
        [JsonProperty("ProgramSlot")]
        public ushort ProgramSlot { get; set; }

        // RoomID if connecting to VC-4 instance
        [JsonIgnore]
        public String RoomID { get; set; }

        /*
         * Information Properties that can only be read from the control system
         */

        // type of entry that this represents
        [JsonIgnore]
        public String Type { get; internal set; }

        // status of the entry. ONLINE, OFFLINE, NOT_REG, ??
        [JsonIgnore]
        public String Status { get; internal set; }

        // model name of the equipment the IP table entry is for
        [JsonIgnore]
        public String ModelName { get; internal set; }

        // descriptin of assigned to the entry. Comes from the SimplWindows comment on the ethernet device
        [JsonIgnore]
        public String Description { get; internal set; }
        
        /// <summary>
        /// SIMPL+ can only execute the default constructor. If you have variables that require initialization, please
        /// use an Initialize method
        /// </summary>
        public CrestronIPTableEntry()
        {
            // default
            CIP_ID = 0;
            IPAddress = "";
            Port = 0;
            RoomID = "";
            DeviceID = 0;
            ProgramSlot = 0;
            Type = "";
            Status = "";
            ModelName = "";
            Description = "";
        }

        /// <summary>
        /// Display IP Table Settings
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format( "ProgramID - {0}, CIP_ID - {1:X}, IPAddress - '{2}', Port - {3}, RoomID - '{4}', DeviceID - {5:X}, Type - '{6}', Status - '{7}', ModelName - '{8}', Description - '{9}'", ProgramSlot, CIP_ID, IPAddress,Port,RoomID,DeviceID, Type, Status, ModelName, Description );
        }

    }
}
