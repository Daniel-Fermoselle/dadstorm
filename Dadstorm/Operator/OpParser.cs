using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Dadstorm
{
    class OpParser
    {
        private String path;
        private List<Tuple> tuples;
        private String[] lines;
        private int line;


        //FOR TESTING PURPOSE
        /*static void Main(string[] args)
        {
            OpParser op = new OpParser("..\\..\\..\\..\\..\\..\\ex.dat");
            List<Tuple> test = op.processFile();
            System.Console.WriteLine(test.ToString());
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }*/
        //FOR TESTING PURPOSE

        public OpParser(String p)
        {
            path = p;
            tuples = new List<Tuple>();
        }


        public void readFile()
        {
            if (!path.Equals(""))
            {
                lines = System.IO.File.ReadAllLines(path);
                line = 0;
            }
            else
            {
                //TODO exception needed (question mark)
            }
        }

        public IList<Tuple> processFile()
        {
            readFile();
            String currentLine = lines[line];
            String[] splits;
            for (; line < lines.Length; line++)
            {
                currentLine = lines[line];

                if (currentLine.Equals("", StringComparison.Ordinal) || currentLine.StartsWith("%", StringComparison.Ordinal))
                {
                    continue;
                }
                else
                {
                    //splits=currentLine.Split(", ");
                    splits = currentLine.Split(new string[] { ", " }, StringSplitOptions.None);
                    IList<string> temp = new List<string>();
                    foreach (string s in splits)
                    {
                        temp.Add(s);
                    }
                    Tuple t = new Tuple(temp);
                    getTuples().Add(t);
                }


            }

            return getTuples();
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public IList<Tuple> getTuples()
        {
            return tuples;
        }

    }
}
