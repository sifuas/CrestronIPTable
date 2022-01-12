using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace CrestronIPTableLibrary
{

    // holds the IP table information
    public class CrestronIPTable
    {
        // the program slot this IP table is for
        public ushort Slot { get; set; }

        // list of all of the IP Table entries
        public List<CrestronIPTableEntry> IPTableEntries;

        // the raw text representing the table entries
        public List<String> RawIPTableEntries;

        public ushort Count
        {
            get
            {
                return (ushort)IPTableEntries.Count;
            }
        }

        // the raw header data
        internal String RawHeaderData;

        // the separator data
        internal String RawSeparatorData;

        // return the # of IP Table Entries
        public ushort EntryCount 
        { 
            get 
            { 
                return (ushort)IPTableEntries.Count; 
            } 
        }

        // initialize the IPTable
        public CrestronIPTable()
        {
            RawHeaderData = "";
            RawSeparatorData = "";
            IPTableEntries = new List<CrestronIPTableEntry>();
            RawIPTableEntries = new List<string>();
        }

        public CrestronIPTable(ushort Slot)
            : this()
        {
            this.Slot = Slot;
        }


        // return the IPTable entry at the given index into the IPTable list
        public ushort GetIPTableEntry(ushort entryIndex, ref CrestronIPTableEntry entryRow)
        {
            ushort returnValue = 0;

            if (entryIndex > 0 && entryIndex <= IPTableEntries.Count)
            {
                entryRow = IPTableEntries[entryIndex -1];
                returnValue = 1;
            }

            return returnValue;
        }

        /// <summary>
        /// Clear out any IP Table Entries
        /// </summary>
        public void Clear()
        {
            if (IPTableEntries != null )
                IPTableEntries.Clear();

            if (RawIPTableEntries != null )
                RawIPTableEntries.Clear();

            RawHeaderData = "";
            RawSeparatorData = "";

            Slot = 0;
        }

        /// <summary>
        /// Return all of the IP Table Entries
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            
            //str.AppendLine(RawHeaderData);
            //str.AppendLine(RawSeparatorData);
            str.AppendLine("IP Table for Slot " + Slot);

            foreach (CrestronIPTableEntry entry in IPTableEntries)
            {
                str.AppendLine(entry.ToString());                
            }

            return str.ToString();
        }
    }
}