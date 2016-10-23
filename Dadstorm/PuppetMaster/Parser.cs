﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Dadstorm
{
    class Parser
    {
        private string[] lines;
        private int line;
        private string loggingLvl;
        private ArrayList commands;

        public void readFile(string path)
        {
            if (!path.Equals(""))
            {
                lines = System.IO.File.ReadAllLines(path);
                line = 0;
                loggingLvl = "light";
                commands = new ArrayList();
            }
            else
            {
                //TODO Throw exception
            }
        }

        public Dictionary<string, ConfigInfo> processFile()
        {
            Dictionary < String, ConfigInfo > returnConfig = new Dictionary<String, ConfigInfo>();
            string currentLine = lines[line];
            for (; line < lines.Length; line++)
            {
                currentLine = lines[line];
                if (currentLine.StartsWith("OP", StringComparison.InvariantCulture))
                {
                    ConfigInfo ci = getOpConfig();
                    returnConfig.Add(ci.OperatorId, ci);
                    continue;
                }
                else if (currentLine.Equals("", StringComparison.Ordinal) || currentLine.StartsWith("%", StringComparison.Ordinal))
                {
                    continue;
                }
                else if (currentLine.StartsWith("LoggingLevel", StringComparison.Ordinal))
                {
                    string[] splitedLine = currentLine.Split(' ');
                    LoggingLvl = splitedLine[1];
                    continue;
                }
                else if (currentLine.StartsWith("Semantics", StringComparison.Ordinal))
                {
                    //TODO implement 
                    continue;
                }
                else if (currentLine.StartsWith("Start", StringComparison.Ordinal)  || currentLine.StartsWith("Interval", StringComparison.Ordinal) ||
                         currentLine.StartsWith("Status", StringComparison.Ordinal) || currentLine.StartsWith("Crash", StringComparison.Ordinal)    ||
                         currentLine.StartsWith("Freeze", StringComparison.Ordinal) || currentLine.StartsWith("Unfreeze", StringComparison.Ordinal) ||
                         currentLine.StartsWith("Wait", StringComparison.Ordinal))
                {
                    commands.Add(currentLine);
                    continue;
                }
                else
                {
                    //TODO throw exception
                    Console.WriteLine("In class Parser method processFile else was reached");
                    Console.Read();
                }
            }
           
            return returnConfig;
        }

        private ConfigInfo getOpConfig()
        {
            //TODO need to add a variable to define a proper order to the file
            ConfigInfo configInfo = new ConfigInfo();
            string[] splitedLine = lines[line].Split(' ');
           
            configInfo.OperatorId = splitedLine[0];
            int inline = 1;

            saveInput(configInfo, splitedLine, ref inline);
            saveRepFact(configInfo, splitedLine, ref inline);
            saveRouting(configInfo, splitedLine, ref inline);
            saveAddr(configInfo, splitedLine, ref inline);
            saveOperation(configInfo, splitedLine, ref inline);
            return configInfo;
        }

        private void saveInput(ConfigInfo configInfo, string[] splitedLine, ref int inline)
        {
            if (!splitedLine[inline].Equals("input_ops", StringComparison.CurrentCultureIgnoreCase))
            {
                //TODO throw proper exception
                throw new NotImplementedException();
            }

            inline += 1;
            while(!splitedLine[inline + 1].Equals("rep_fact", StringComparison.CurrentCultureIgnoreCase))
            {
                configInfo.AddSourceInput(splitedLine[inline].Remove(splitedLine[inline].Length - 1));
                inline += 1;
            }
            configInfo.AddSourceInput(splitedLine[inline]);
            inline += 1;
        }

        private void saveRepFact(ConfigInfo configInfo, string[] splitedLine, ref int inline)
        {
            if (!splitedLine[inline].Equals("rep_fact", StringComparison.CurrentCultureIgnoreCase))
            {
                //TODO throw proper exception
                throw new NotImplementedException();
            }
            inline += 1;
            configInfo.RepFactor = Int32.Parse(splitedLine[inline]);
            inline += 1;
        }

        private void saveRouting(ConfigInfo configInfo, string[] splitedLine, ref int inline)
        {
            if (!splitedLine[inline].Equals("routing", StringComparison.CurrentCultureIgnoreCase))
            {
                //TODO throw proper exception
                throw new NotImplementedException();
            }
            inline += 1;
            configInfo.Routing = splitedLine[inline];
            inline += 1;
        }

        private void saveAddr(ConfigInfo configInfo, string[] splitedLine, ref int inline)
        {
            if (!splitedLine[inline].Equals("address", StringComparison.CurrentCultureIgnoreCase))
            {
                //TODO throw proper exception
                throw new NotImplementedException();
            }
            inline += 1;
            while (!splitedLine[inline + 1].Equals("operator_spec", StringComparison.CurrentCultureIgnoreCase))
            {
                configInfo.AddUrls(splitedLine[inline].Remove(splitedLine[inline].Length - 1));
                inline += 1;
            }
            configInfo.AddUrls(splitedLine[inline]);
            inline += 1;
        }

        private void saveOperation(ConfigInfo configInfo, string[] splitedLine, ref int inline)
        {
            if (!splitedLine[inline].Equals("operator_spec", StringComparison.CurrentCultureIgnoreCase))
            {
                //TODO throw proper exception
                throw new NotImplementedException();
            }
            inline += 1;
            configInfo.Operation = splitedLine[inline];
            if (splitedLine.Length - 1 > inline)
            {
                inline += 1;
                string[] param = splitedLine[inline].Split(',');
                for(int i = 0; i < param.Length; i++)
                {
                    configInfo.AddOperationParam(param[i]);
                } 
            }
        }

        public string LoggingLvl
        {
            get { return loggingLvl;  }
            set { loggingLvl = value; }
        }

        public ArrayList Commands
        {
            get { return commands; }
            set { commands = value; }
        }

        public bool anyCommands()
        {
            return commands.Count > 0;
        }
    }
}
