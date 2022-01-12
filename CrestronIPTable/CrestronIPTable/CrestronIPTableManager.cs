using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using Crestron.SimplSharp;

namespace CrestronIPTableLibrary
{

    /// <summary>
    /// Load the IP Table
    /// </summary>
    public sealed class CrestronIPTableManager
    {
        /// <summary>
        /// Processor commands
        /// </summary>
        private const string COMMAND_LIST_IPTABLE          = "iptable";
        private const string COMMAND_LIST_IPTABLE_TABULAR  = "iptable -t";
        private const string COMMAND_ADD_PEER              = "addpeer";
        private const string COMMAND_REMOVE_PEER           = "rempeer";

        private const string RESPONSE_MSG_ERROR            = "bad or incomplete command";

        // Simpl+ default constructor
        public CrestronIPTableManager() { }

        /// <summary>
        /// Load the IP Table from the processor.
        /// </summary>
        /// <param name="programSlot">The program slot to load the IP Table for. 0 loads all IP Table entries</param>
        /// <param name="ipTable"> The IPTable to load the data into</param>
        /// <returns>1 if the IP Table was able to be loaded or 0 if an error occurs.</returns>
        public ushort LoadIPTableFromProcessor(ushort programSlot, ref CrestronIPTable ipTable )
        {
            String response = "";
            ushort returnValue = 0;

            if (ipTable == null)
                throw new ArgumentException("IPTable Is NULL");

            // load all program slots
            if (programSlot == 0)
                CrestronConsole.SendControlSystemCommand(String.Format("{0}", COMMAND_LIST_IPTABLE_TABULAR), ref response);
            // load a specific program slot
            else if (programSlot > 0 && programSlot <= 10)
                CrestronConsole.SendControlSystemCommand(String.Format("{0} -p:{1}", COMMAND_LIST_IPTABLE_TABULAR, programSlot), ref response);
            else
                throw new ArgumentException(String.Format("Invalid Slot # {0}", programSlot));

            //CrestronConsole.PrintLine(String.Format("Loading IP Table for Slot {0}", programSlot));

            // check the response data
            if (response == null || response.ToLower().Contains(RESPONSE_MSG_ERROR))
            {
                CrestronConsole.PrintLine(String.Format("No IPTable Loaded for Slot {0}", programSlot));
            }
            // parse the IPTable information if any was returned
            else if ((response.Length > 0) && (response.ToLower().Contains("tablestart:")))
            {
                //CrestronConsole.PrintLine(String.Format("Parsing IP Table Data For Slot{0}", programSlot));

                // split into lines based on 
                string[] ipTableByLine = response.Trim().Split('\n');

                string formattedRow = "";
                string[] rowData = { };

                CrestronIPTableEntry tableEntry = null;
                string headerData = "";
                string separatorData = "";

                ipTable.Slot = programSlot;
                ipTable.Clear();

                /*
                 *  The first two lines of a tabular formatted IP table are the header and a separator line
                 *  CIP_ID  |Type    |Status    |DevID   |Port   |IP Address/SiteName       |Model Name          |Description         |RoomId
                 *  -------------------------------------------------------------------------------------------------------------------------                  
                 */
                if (ipTableByLine != null && ipTableByLine.Length >= 2)
                {
                    try
                    {
                        CrestronConsole.PrintLine(String.Format("Looping Through IP Table Data {0}", ipTableByLine.Length));

                        // go through each row of the table
                        foreach (string row in ipTableByLine)
                        {
                            formattedRow = row.Trim();

                            if (formattedRow.StartsWith("CIP_ID")) // found header
                            {
                                //CrestronConsole.PrintLine(String.Format("Found Header {0}", row));
                                headerData = formattedRow;
                            }
                            else if (formattedRow.StartsWith("-")) // found separator
                            {
                                //CrestronConsole.PrintLine(String.Format("Found Separator {0}", row));
                                separatorData = formattedRow;
                            }
                            else if (formattedRow.Contains("|"))// found a row
                            {
                                rowData = formattedRow.Split('|');
                                //CrestronConsole.PrintLine("Row Has {0} Columns", rowData.Length);
                                //CrestronConsole.PrintLine("{0}", formattedRow);

                                if ((rowData != null) && (rowData.Length == 9))
                                {
                                    tableEntry = new CrestronIPTableEntry();
                                    tableEntry.ProgramSlot = programSlot;

                                    // CIP_ID
                                    tableEntry.CIP_ID       = TryParseUShortValueAsHex(rowData[0]);

                                    // Type
                                    tableEntry.Type         = TryTrimString(rowData[1]);

                                    // Status
                                    tableEntry.Status       = TryTrimString(rowData[2]);

                                    // Device ID
                                    tableEntry.DeviceID     = TryParseUShortValueAsHex(rowData[3]);

                                    // Port
                                    tableEntry.Port         = TryParseUShortValue(rowData[4]);

                                    // IP Address / HostName
                                    tableEntry.IPAddress    = TryTrimString(rowData[5]);

                                    // Model Name
                                    tableEntry.ModelName    = TryTrimString(rowData[6]);

                                    // Description
                                    tableEntry.Description  = TryTrimString(rowData[7]);

                                    // RoomID
                                    tableEntry.RoomID       = TryTrimString(rowData[8]);

                                    // add the IP Table entry to the IP Table
                                    if( ipTable.IPTableEntries != null )
                                        ipTable.IPTableEntries.Add(tableEntry);

                                    if( ipTable.RawIPTableEntries != null )
                                        ipTable.RawIPTableEntries.Add(formattedRow);
                             
                                    ipTable.RawHeaderData = headerData;

                                    ipTable.RawSeparatorData = separatorData;

                                    //CrestronConsole.PrintLine("Parsed Entry {0}", tableEntry);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLog.Error(String.Format("Error Parsing IPTable for Slot {0} - {1}", programSlot, ex.Message));
                        ErrorLog.Error(ex.StackTrace);
                    }
                }
                else
                {
                    ErrorLog.Warn(String.Format("IPTableLoader - Invalid Response loading IP Table - '{0}'", response));
                }

                returnValue = 1;
            }


            return returnValue;
        }

        /// <summary>
        /// Add the IP Table entry to the currently running program in the given slot.
        /// </summary>
        /// <param name="programSlot">The program slot the IP Entry is for</param>
        /// <param name="CIP_ID">The CIP_ID to configure communications for</param>
        /// <param name="IPAddress">The IPAddress for the device.</param>        
        /// <returns></returns>
        public ushort AddIPTableEntry( ushort programSlot, ushort CIP_ID, string IPAddress )
        {
            ushort returnValue = 0;
            string response = "";
            
            if ( IsValidProgramSlot( programSlot ) && ( CIP_ID > 0) && (IPAddress != null) && (IPAddress.Trim().Length > 0))
            {                
                // configure new table entry
                CrestronIPTableEntry tableEntry = new CrestronIPTableEntry();
                tableEntry.CIP_ID = CIP_ID;
                tableEntry.IPAddress = IPAddress;
                tableEntry.ProgramSlot = programSlot;

                // create the command string
                StringBuilder commandString = new StringBuilder();

                // add the base command
                commandString.Append(String.Format( "{0} {1:X} {2}", COMMAND_ADD_PEER, tableEntry.CIP_ID, tableEntry.IPAddress ));

                // add the program slot if needed
                if (tableEntry.ProgramSlot != 0)
                    commandString.Append( String.Format( " -p:{0}", tableEntry.ProgramSlot) );

                // send the command
                //CrestronConsole.PrintLine(String.Format("Sending Command '{0}'", commandString.ToString()));
                CrestronConsole.SendControlSystemCommand(commandString.ToString(), ref response);

                // check for valid response
                if (response != null && response.ToLower().Contains( "master list set" ))
                {
                    //CrestronConsole.PrintLine( String.Format( "AddIPTableEntry Response : '{0}'", response ) );
                    returnValue = 1;
                }
            }
            else
            {
                throw new ArgumentException("Cannot add new IP Table Entry - Invalid IPTableEntry values");
            }

            return returnValue;
        }

        /// <summary>
        /// Add the IP Table entry to the currently running program in the given slot with the given Device_ID to remap from the programmed entry to the runtime entry.
        /// </summary>
        /// <param name="programSlot">The program slot the IP Entry is for</param>
        /// <param name="CIP_ID">The CIP_ID to configure communications for</param>
        /// <param name="IPAddress">The IPAddress for the device.</param>  
        /// <param name="Device_ID">The original CIP_ID setup in the SimplWindows program that will be remapped to the new CIP_ID</param>
        /// <returns></returns>
        public ushort AddIPTableEntryWithRemap(ushort programSlot, ushort CIP_ID, string IPAddress, ushort Device_ID)
        {
            ushort returnValue = 0;
            string response = "";

            if (IsValidProgramSlot(programSlot) && (CIP_ID > 0) && (IPAddress != null) && (IPAddress.Trim().Length > 0) && ( Device_ID > 0 ) )
            {
                // configure new table entry
                CrestronIPTableEntry tableEntry = new CrestronIPTableEntry();
                tableEntry.CIP_ID = CIP_ID;
                tableEntry.IPAddress = IPAddress;
                tableEntry.ProgramSlot = programSlot;
                tableEntry.DeviceID = Device_ID;

                // create the command string
                StringBuilder commandString = new StringBuilder();

                // add the base command
                commandString.Append(String.Format("{0} {1:X} {2}", COMMAND_ADD_PEER, tableEntry.CIP_ID, tableEntry.IPAddress));

                // add the device_id 
                commandString.Append(String.Format(" -D:{0:X}", tableEntry.DeviceID));

                // add the program slot if needed
                if (tableEntry.ProgramSlot != 0)
                    commandString.Append(String.Format(" -p:{0}", tableEntry.ProgramSlot));

                // send the command
                //CrestronConsole.PrintLine(String.Format("Sending Command '{0}'", commandString.ToString()));
                CrestronConsole.SendControlSystemCommand(commandString.ToString(), ref response);

                // check for valid response
                if (response != null && response.ToLower().Contains("master list set"))
                {
                    //CrestronConsole.PrintLine(String.Format("AddIPTableEntryWithRemap Response : '{0}'", response));
                    returnValue = 1;
                }
            }
            else
            {
                throw new ArgumentException("Cannot add new IP Table Entry - Invalid IPTableEntry values");
            }

            return returnValue;
        }

        /// <summary>
        /// Remove the IP Table entry to the currently running program in the given slot.
        /// </summary>
        /// <param name="tableEntry">IPTableEntry containing the needed infromation for the entry to remove. Must provide CIP_ID and IPAddress</param>
        /// <returns></returns>
        public ushort RemoveIPTableEntry(ushort programSlot, ushort CIP_ID, string IPAddress)
        {
            ushort returnValue = 0;
            string response = "";

            if (IsValidProgramSlot(programSlot) && (CIP_ID > 0) && (IPAddress != null) && (IPAddress.Trim().Length > 0))
            {
                // configure new table entry
                CrestronIPTableEntry tableEntry = new CrestronIPTableEntry();
                tableEntry.CIP_ID = CIP_ID;
                tableEntry.IPAddress = IPAddress;
                tableEntry.ProgramSlot = programSlot;

                // create the command string
                StringBuilder commandString = new StringBuilder();

                // add the base command
                commandString.Append(String.Format("{0} {1:X} {2}", COMMAND_REMOVE_PEER, tableEntry.CIP_ID, tableEntry.IPAddress));

                // add the program slot if needed
                if (tableEntry.ProgramSlot != 0)
                    commandString.Append(String.Format(" -p:{0}", tableEntry.ProgramSlot));

                // send the command
                //CrestronConsole.PrintLine(String.Format("Sending Command '{0}'", commandString.ToString()));
                CrestronConsole.SendControlSystemCommand(commandString.ToString(), ref response);

                // check for valid response
                if (response != null && response.ToLower().Contains("master list set"))
                {
                    //CrestronConsole.PrintLine(String.Format("AddIPTableEntry Response : '{0}'", response));
                    returnValue = 1;
                }
                else if (response.ToLower().Contains("unable to remove ip table entry"))
                {
                    ErrorLog.Warn(String.Format("IPTableCOnfiguration.RemoveIPTableEntry() : Could not remove IP Table Entry {0:X} - {1}", CIP_ID, IPAddress));
                }
            }
            else
            {
                throw new ArgumentException("Cannot add new IP Table Entry - Invalid IPTableEntry values");
            }

            return returnValue;
        }

        /// <summary>
        /// Check if there is an CIP_ID entry matching the given CIP_ID.
        /// </summary>
        /// <param name="CIP_ID">The CIP_ID to search for in the IP Table</param>
        /// <param name="tableEntry">Table Entry to be populated with the current IP table entry information if one is found</param>
        /// <returns>1 if an IP Table entry exists matching the CIP_ID</returns>
        public ushort IPEntryExists(ushort programSlot, ushort CIP_ID, CrestronIPTableEntry returnTableEntry)
        {
            ushort returnValue = 0;

            // create an IPTable to load the information into
            CrestronIPTable ipTable = new CrestronIPTable(programSlot);

            // load the IP Table from the program slot
            if (LoadIPTableFromProcessor(programSlot, ref ipTable) == 1)
            {
                returnValue = IPEntryExists(programSlot, CIP_ID, ipTable, returnTableEntry);
            }

            return returnValue;
        }

        public ushort IPEntryExists(ushort programSlot, ushort CIP_ID, CrestronIPTable ipTable, CrestronIPTableEntry returnTableEntry )
        {
            ushort returnValue = 0;

            // if there are IP Table entries
            if ((ipTable != null) && (ipTable.Count > 0))
            {
                /*
                // search the IP table for the given entry
                IPTableEntryConfiguration tmpEntry = null; // ipTable.IPTableEntries.FirstOrDefault(e => e.CIP_ID == CIP_ID);

                if (returnTableEntry != null)
                {
                    CopyIPTableEntryValues(tmpEntry, returnTableEntry);
                }
                */
                
                foreach (CrestronIPTableEntry tmpEntry in ipTable.IPTableEntries)
                {
                    if (tmpEntry != null)
                    {
                        //CrestronConsole.PrintLine(String.Format("Checking IP Table Entry : {0:X} with {1:X}", tmpEntry.CIP_ID, CIP_ID));

                        if (tmpEntry.CIP_ID == CIP_ID)
                        {
                            returnValue = 1;
                            // copy the values to return to the caller
                            if (returnTableEntry != null)
                            {
                                CopyIPTableEntryValues(tmpEntry, returnTableEntry);
                            }

                            break;
                        }
 
                    }
                }
                
            }
            else
            {
                CrestronConsole.PrintLine("ipTable NULL or Empty");
            }

            return returnValue;
        }

        // search the processor IP table for the given entry and return the details for it
        public ushort LoadIPEntryFromProcesor(ushort programSlot, ushort CIP_ID, CrestronIPTableEntry tableEntry)
        {
            ushort returnValue = 0;

            //CrestronConsole.PrintLine(String.Format("LoadIPEntryFromProcessor Slot {0}, CIP_ID {1}", programSlot, CIP_ID ));

            // load the entry and check if it exists
            if (IPEntryExists(programSlot, CIP_ID, tableEntry) == 1)
            {
                returnValue = 1;
            }

            return returnValue;
        }

        // try to parse the given string as a ushort. If parsing fails return 0
        private ushort TryParseUShortValue(String strValue)
        {
            ushort returnValue = 0;

            //CrestronConsole.PrintLine(String.Format("TryParseUShortValue"));

            try
            {
                returnValue = ushort.Parse(strValue);
            }
            catch (Exception ex) { }

            return returnValue;
        }

        // try to parse the given string as a ushort. If parsing fails return 0
        private ushort TryParseUShortValueAsHex(String strValue)
        {
            ushort returnValue = 0;

            //CrestronConsole.PrintLine(String.Format("TryParseUShortValueAsHex"));

            try
            {
                returnValue = ushort.Parse(strValue, System.Globalization.NumberStyles.HexNumber); 
            }
            catch (Exception ex) { }

            return returnValue;
        }

        // trim the given string or return ""
        private String TryTrimString(String strValue)
        {
            //CrestronConsole.PrintLine(String.Format("TryTrimString"));

            if (strValue != null)
                return strValue.Trim();
            else
                return "";
        }

        // test if the program slot is valid
        private bool IsValidProgramSlot(ushort programSlot)
        {
            return ((programSlot >= 0) && (programSlot <= 10));
        }

        // copy the values from one IPTableEntry to another
        private void CopyIPTableEntryValues(CrestronIPTableEntry fromEntry, CrestronIPTableEntry toEntry)
        {
            if (fromEntry == null || toEntry == null)
            {
                CrestronConsole.PrintLine("CopyIPTableEntryValues - Null Entry");
                return;
            }

            // copy values to return entry
            toEntry.CIP_ID = fromEntry.CIP_ID;
            toEntry.IPAddress = fromEntry.IPAddress;

            toEntry.DeviceID = fromEntry.DeviceID;
            toEntry.ProgramSlot = fromEntry.ProgramSlot;
            toEntry.ModelName = fromEntry.ModelName;
            toEntry.Port = fromEntry.Port;
            toEntry.Description = fromEntry.Description;
            toEntry.RoomID = fromEntry.RoomID;
            toEntry.Status = fromEntry.Status;
            toEntry.Type = fromEntry.Type;
        }
    }
}