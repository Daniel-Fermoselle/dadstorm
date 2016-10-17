using System;
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

        public void readFile(string path)
        {
            if (!path.Equals(""))
            {
                lines = System.IO.File.ReadAllLines(path);
                line = 0;
                loggingLvl = "light";
            }
            else
            {
                //TODO Throw exception
            }
        }

        public Dictionary<string, ConfigInfo> processFile()
        {
            Dictionary < String, ConfigInfo > returnConfig = new Dictionary<String, ConfigInfo>();
            while(line != lines.Length)
            {
                ConfigInfo ci = getOpConfig();
                returnConfig.Add(ci.OperatorId, ci);
            }
            return returnConfig;
        }

        private ConfigInfo getOpConfig()
        {
            //TODO need to add a variable to define a proper order to the file
            ConfigInfo configInfo = new ConfigInfo();
            StringComparison comparison = StringComparison.InvariantCulture;

            string currentLine = lines[line];
            //Ignore white spaces in the beginning
            while (!lines[line].StartsWith("OPERATOR_SPEC", comparison))
            {
                currentLine = lines[line];
                
                //Ignores empty lines
                if (currentLine.Equals("", StringComparison.Ordinal))
                { 
                    line += 1;
                    continue;
                }

                else if (currentLine.StartsWith("OP", comparison))
                {
                    processLine1(configInfo, currentLine);
                    line += 1;
                    continue;
                }

                else if (currentLine.StartsWith("REP_FACT", comparison))
                {
                    processLine2(configInfo, currentLine);
                    line += 1;
                    continue;
                }

                else if (currentLine.StartsWith("ADDRESS", comparison))
                {
                    processLine3(configInfo, currentLine);
                    line += 1;
                    continue;
                }
                else
                {
                    //TODO throw exception and remove this
                    Console.WriteLine("In class Parser method getOpConfig else was reached");
                    Console.Read();
                }
            }
            processLine4(configInfo, lines[line]);
            line += 1;
            return configInfo;
        }

        private void processLine1(ConfigInfo configInfo, string currentLine)
        {
            string[] splitedLine = currentLine.Split(' ');
            configInfo.OperatorId = splitedLine[0];
            if (splitedLine.Length > 3)
            {
                //Removes the ',' from the input and adds it to configInfo
                for (int i = 2; i < splitedLine.Length - 1; i++)
                {
                    configInfo.AddSourceInput(splitedLine[i].Remove(splitedLine[i].Length - 1));
                }
            }
            configInfo.AddSourceInput(splitedLine[splitedLine.Length - 1]);
        }

        private void processLine2(ConfigInfo configInfo, string currentLine)
        {
            string[] splitedLine = currentLine.Split(' ');
            configInfo.RepFactor = Int32.Parse(splitedLine[1]);
            configInfo.Routing = splitedLine[3];
        }

        private void processLine3(ConfigInfo configInfo, string currentLine)
        {
            string[] splitedLine = currentLine.Split(' ');
            if (splitedLine.Length > 2)
            {
                //Removes the ',' from the urls and adds them to configInfo
                for (int i = 1; i < splitedLine.Length - 1; i++)
                {
                    configInfo.AddUrls(splitedLine[i].Remove(splitedLine[i].Length - 1));
                }
            }
            configInfo.AddUrls(splitedLine[splitedLine.Length - 1]);
        }

        private void processLine4(ConfigInfo configInfo, string currentLine)
        {
            string[] splitedLine = currentLine.Split(' ');
            configInfo.Operation = splitedLine[1];
            if(splitedLine.Length > 3)
            {
                //Removes the ',' from the input and adds it to configInfo
                for (int i = 2; i < splitedLine.Length - 1; i++)
                {
                    configInfo.AddOperationParam(splitedLine[i].Remove(splitedLine[i].Length - 1));
                }
            }
            configInfo.AddOperationParam(splitedLine[splitedLine.Length - 1]);
        }

        public string LoggingLvl
        {
            get { return loggingLvl;  }
            set { loggingLvl = value; }
        }
    }
}
