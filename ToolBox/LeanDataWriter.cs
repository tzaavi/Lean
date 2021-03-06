﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Data writer for saving an IEnumerable of BaseData into the LEAN data directory.
    /// </summary>
    public class LeanDataWriter
    {
        private readonly Symbol _symbol;
        private readonly string _market;
        private readonly string _dataDirectory;
        private readonly TickType _dataType;
        private readonly Resolution _resolution;
        private readonly SecurityType _securityType;
        
        /// <summary>
        /// Create a new lean data writer to this base data directory.
        /// </summary>
        /// <param name="symbol">Symbol string</param>
        /// <param name="dataDirectory">Base data directory</param>
        /// <param name="type">Security type</param>
        /// <param name="resolution">Resolution of the desired output data</param>
        /// <param name="market">Market for this security</param>
        /// <param name="dataType">Write the data to trade files</param>
        public LeanDataWriter(SecurityType type, Resolution resolution, Symbol symbol, string dataDirectory, string market, TickType dataType = TickType.Trade)
        {
            _securityType = type;
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _symbol = symbol.ToLower();
            _market = market.ToLower();
            _dataType = dataType;

            // All fx data is quote data.
            if (_securityType == SecurityType.Forex || _securityType == SecurityType.Cfd)
            {
                _dataType = TickType.Quote;
            }

            // Can only process Fx and equity for now
            if (_securityType != SecurityType.Equity && _securityType != SecurityType.Forex && _securityType != SecurityType.Cfd)
            {
                throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }
        }

        /// <summary>
        /// Given the constructor parameters, write out the data in LEAN format.
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        public void Write(IEnumerable<BaseData> source)
        {
            switch (_resolution)
            {
                case Resolution.Daily:
                case Resolution.Hour:
                    WriteDailyOrHour(source);
                    break;

                case Resolution.Minute:
                case Resolution.Second:
                case Resolution.Tick:
                    WriteMinuteOrSecondOrTick(source);
                    break;
            }
        }

        /// <summary>
        /// Write out the data in LEAN format (minute, second or tick resolutions)
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        /// <remarks>This function overwrites existing data files</remarks>
        private void WriteMinuteOrSecondOrTick(IEnumerable<BaseData> source)
        {
            var sb = new StringBuilder();
            var lastTime = new DateTime();

            // Determine file path
            var baseDirectory = Path.Combine(_dataDirectory, _securityType.ToString().ToLower(), _market);

            // Loop through all the data and write to file as we go
            foreach (var data in source)
            {
                // Ensure the data is sorted
                if (data.Time < lastTime) throw new Exception("The data must be pre-sorted from oldest to newest");

                // Based on the security type and resolution, write the data to the zip file
                if (lastTime != DateTime.MinValue && data.Time.Date > lastTime.Date)
                {
                    // Write and clear the file contents
                    var outputFile = GetZipOutputFileName(baseDirectory, lastTime);
                    WriteFile(outputFile, sb.ToString(), lastTime);
                    sb.Clear();
                }

                lastTime = data.Time;

                // Build the line and append it to the file
                sb.Append(GenerateFileLine(data) + Environment.NewLine);
            }

            // Write the last file
            if (sb.Length > 0)
            {
                var outputFile = GetZipOutputFileName(baseDirectory, lastTime);
                WriteFile(outputFile, sb.ToString(), lastTime);
            }
        }

        /// <summary>
        /// Write out the data in LEAN format (daily or hour resolutions)
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        /// <remarks>This function performs a merge (insert/append/overwrite) with the existing Lean zip file</remarks>
        private void WriteDailyOrHour(IEnumerable<BaseData> source)
        {
            var sb = new StringBuilder();
            var lastTime = new DateTime();

            // Determine file path
            var baseDirectory = Path.Combine(_dataDirectory, _securityType.ToString().ToLower(), _market);

            var outputFile = GetZipOutputFileName(baseDirectory, lastTime);

            // Load new data rows into a SortedDictionary for easy merge/update
            var newRows = new SortedDictionary<DateTime, string>(source.ToDictionary(x => x.Time, GenerateFileLine));
            SortedDictionary<DateTime, string> rows;

            if (File.Exists(outputFile))
            {
                // If file exists, we load existing data and perform merge
                rows = LoadHourlyOrDailyFile(outputFile);
                foreach (var kvp in newRows)
                {
                    rows[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // No existing file, just use the new data
                rows = newRows;
            }

            // Loop through the SortedDictionary and write to file contents
            foreach (var kvp in rows)
            {
                // Build the line and append it to the file
                sb.Append(kvp.Value + Environment.NewLine);
            }

            // Write the file contents
            if (sb.Length > 0)
            {
                WriteFile(outputFile, sb.ToString(), lastTime);
            }
        }

        /// <summary>
        /// Loads an existing hourly or daily Lean zip file into a SortedDictionary
        /// </summary>
        private static SortedDictionary<DateTime, string> LoadHourlyOrDailyFile(string fileName)
        {
            var rows = new SortedDictionary<DateTime, string>();

            using (var zip = ZipFile.Read(fileName))
            {
                using (var stream = new MemoryStream())
                {
                    zip[0].Extract(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var time = DateTime.ParseExact(line.Substring(0, DateFormat.TwelveCharacter.Length), DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                            rows.Add(time, line);
                        }
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// Write this file to disk
        /// </summary>
        private void WriteFile(string fileName, string data, DateTime time)
        {
            data = data.TrimEnd();
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                Log.Trace("LeanDataWriter.Write(): Existing deleted: " + fileName);
            }
            // Create the directory if it doesnt exist
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            // Write out this data string to a zip file
            Compression.Zip(data, fileName, Compression.CreateZipEntryName(_symbol, _securityType, time, _resolution, _dataType));
            Log.Trace("LeanDataWriter.Write(): Created: " + fileName);
        }

        /// <summary>
        /// Generate a single line of the data for this security type
        /// </summary>
        /// <param name="data">Data we're generating</param>
        /// <returns>String line for this basedata</returns>
        private string GenerateFileLine(IBaseData data)
        {
            var line = string.Empty;
            var format = "{0},{1},{2},{3},{4},{5}";
            var milliseconds = data.Time.TimeOfDay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            var longTime = data.Time.ToString(DateFormat.TwelveCharacter);

            switch (_securityType)
            {
                case SecurityType.Equity:
                    switch (_resolution)
                    {
                        case Resolution.Tick:
                            var tick = data as Tick;
                            if (tick != null)
                            {
                                line = string.Format(format, milliseconds, Scale(tick.LastPrice), tick.Quantity, tick.Exchange, tick.SaleCondition, tick.Suspicious);
                            }
                            break;

                        case Resolution.Minute:
                        case Resolution.Second:
                            var bar = data as TradeBar;
                            if (bar != null)
                            {
                                line = string.Format(format, milliseconds, Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close), bar.Volume);   
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = data as TradeBar;
                            if (bigBar != null)
                            {
                                line = string.Format(format, longTime, Scale(bigBar.Open), Scale(bigBar.High), Scale(bigBar.Low), Scale(bigBar.Close), bigBar.Volume);
                            }
                            break;
                    }
                    break;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    switch (_resolution)
                    {
                        case Resolution.Tick:
                            var fxTick = data as Tick;
                            if (fxTick != null)
                            {
                                line = string.Format("{0},{1},{2}", milliseconds, fxTick.BidPrice, fxTick.AskPrice);
                            }
                            break;

                        case Resolution.Second:
                        case Resolution.Minute:
                            var fxBar = data as TradeBar;
                            if (fxBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", milliseconds, fxBar.Open, fxBar.High, fxBar.Low, fxBar.Close);
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var dailyBar = data as TradeBar;
                            if (dailyBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", longTime, dailyBar.Open, dailyBar.High, dailyBar.Low, dailyBar.Close);
                            }
                            break;
                    }
                    break;
            }

            return line;
        }

        /// <summary>
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static int Scale(decimal value)
        {
            return Convert.ToInt32(value*10000);
        }

        /// <summary>
        /// Get the output zip file
        /// </summary>
        /// <param name="baseDirectory">Base output directory for the zip file</param>
        /// <param name="time">Date/time for the data we're writing</param>
        /// <returns></returns>
        private string GetZipOutputFileName(string baseDirectory, DateTime time)
        {
            string file;

            // Further determine path based on the remaining data: security type
            switch (_securityType)
            {
                case SecurityType.Equity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    // Base directory includes the market
                    if (_resolution == Resolution.Daily || _resolution == Resolution.Hour)
                    {
                        file = Path.Combine(baseDirectory, _resolution.ToString().ToLower(), Compression.CreateZipFileName(_symbol, _securityType, time, _resolution));
                    }
                    else
                    {
                        file = Path.Combine(baseDirectory, _resolution.ToString().ToLower(), _symbol.ToLower(), Compression.CreateZipFileName(_symbol, _securityType, time, _resolution));
                    }
                    break;

                default:
                    throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }

            return file;
        }

    }
}
