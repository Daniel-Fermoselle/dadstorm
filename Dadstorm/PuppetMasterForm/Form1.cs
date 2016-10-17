using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace Dadstorm
{
    public partial class Form1 : Form
    {

        private PuppetMaster puppetMaster;
        public Form1()
        {
            InitializeComponent();
            puppetMaster = new PuppetMaster(this, new DelPrint(PrintToOutputBox));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            puppetMaster.StartProcessesPhase();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public delegate void DelPrint(string m);

        public void PrintToOutputBox(string toPrint)
        {
            textBox1.Text += toPrint + "\r\n";
        }
    }
}
