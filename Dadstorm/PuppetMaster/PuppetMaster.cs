﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Dadstorm
{
    class PuppetMaster
    {

        static void Main(string[] args)
        {
            //Start processes phase
            Parser p = new Parser();
            p.readFile(@"C:\Users\sigma\Dropbox\repos\dadstorm\Exemplos\configEnunciado.txt");
            Dictionary<string, ConfigInfo> result;
            result = p.processFile();
            foreach(string s in result.Keys)
            {
                Console.WriteLine(s);
            }
            Console.ReadLine();
            //Receive inputs and log phase
        }

        public void Start(string operator_id)
        {
            //TODO: implement
        }

        public void Interval(string operator_id, string x_ms)
        {
            //TODO: implement
        }

        public void Status()
        {
            //TODO: implement
        }

        public void Crash(string processname)
        {
            //TODO: implement
        }

        public void Freeze(string processname)
        {
            //TODO: implement
        }

        public void Unfreeze(string processname)
        {
            //TODO: implement
        }

        public void Wait(string x_ms)
        {
            //TODO: implement
        }
    }
}