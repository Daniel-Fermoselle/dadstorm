using System;
using System.Collections.Generic;
using System.Text;

namespace Dadstorm
{
    class Parser
    {
        private string[] lines;
        private int line;

        public Parser(string path)
        {
        }
        public void readFile(string path)
        {
            if (path != "")
            {
                lines = System.IO.File.ReadAllLines(path);
                line = 0;
            }
            else
            {
                //TODO Throw exception
            }
        }

        public ConfigInfo[] processFile()
        {

        }

        public ConfigInfo getOpConfig()
        {
            //TODO need to add a variable to define a proper order to the file
            ConfigInfo configInfo = new ConfigInfo();
            StringComparison comparison = StringComparison.InvariantCulture;

            string currentLine = lines[line];
            //Ignore white spaces in the beginning
            while (lines[line].StartsWith("OPERATOR_SPEC", comparison))
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
            processLine4(currentLine, currentLine);
            return configInfo;
        }

        public void processLine1(ConfigInfo configInfo, string currentLine)
        {
            throw new NotImplementedException();
        }

        private void processLine2(ConfigInfo configInfo, string currentLine)
        {
            throw new NotImplementedException();
        }

        private void processLine3(ConfigInfo configInfo, string currentLine)
        {
            throw new NotImplementedException();
        }
        private void processLine4(string currentLine1, string currentLine2)
        {
            throw new NotImplementedException();
        }


    }
}
